# PauloETL C# Rewrite — Implementation Plan

## Async Analysis Conclusion

**Use async I/O throughout, but keep sequential execution order.**

Findings from investigation:
- **Step-level parallelism**: Upload steps (Equipment Events, Weights, Gross Weights) are logically independent. However, they all share the same two database connections. Parallelizing would require connection pooling and adds complexity for minimal gain — these are simple row-by-row operations.
- **Command-level parallelism**: NOT feasible. Child command parameters are resolved from the parent recordset's *current row*. The VB6 code does `moRs.MoveFirst` / `moRs.MoveNext` and children read from the current position. Parallelizing would require pre-fetching all rows and passing data by value — a significant architectural change.
- **Oracle session state**: `SET ROLE equipment_user` is session-scoped. Any new connection needs it re-applied. This complicates connection pooling.
- **Database-side ordering**: Download phase has strict ordering — Start → Load → Build → Complete per cost center.

**Decision**: async/await for all DB calls (non-blocking I/O, clean resource usage) but sequential execution flow matching the VB6 behavior exactly. This is the safe, correct choice.

## Dry-Run Mode

When `--dry-run` is passed:
- **Read commands** (`rowset="true"` with no write side-effects like initial SELECTs) execute normally to drive foreach logic
- **Write/mutation commands** are logged with fully-resolved parameters but NOT executed
- **Commands that both write AND return data** (e.g., Oracle `SendEquipmentEvent` returns `RETURN_CODE`): logged but not executed; child commands that depend on their output are also skipped and logged
- A detailed structured log shows every command that WOULD execute with parameter names, values, and types
- Separate log file: `PauloETL_DryRun_{timestamp}.log`

Heuristic for identifying mutations: Commands calling Oracle package procedures and SQL `EXEC` stored procedures that are children within a `foreach` (i.e., they process individual rows) are treated as mutations. The top-level read commands that drive the loops still execute.

Additionally, the `--dry-run` flag will set a `DryRun` property on the engine. Each `EtlCommand.ExecuteAsync()` checks: if `DryRun` is true AND this command has a parent (it's a child in a foreach), log and skip. Top-level commands (no parent, rowset=true) still execute to populate the data that shows what WOULD be processed.

## Project Structure

```
PauloETL.CSharp/
├── PauloETL.CSharp.csproj          # .NET 8 console app
├── Program.cs                       # Entry point, argument parsing
├── appsettings.json                 # Default settings (log paths, etc.)
│
├── Configuration/
│   ├── AdoTypeMapper.cs             # Maps XML type/direction strings → DbType/ParameterDirection
│   └── XmlConfigParser.cs           # Parses PauloETL.xml, validates against XSD
│
├── Connections/
│   └── EtlConnection.cs            # Wraps DbConnection (SqlConnection or OracleConnection)
│
├── Engine/
│   ├── EtlEngine.cs                # Main orchestrator (replaces ETLControl)
│   ├── EtlCommand.cs               # Command executor with foreach support
│   └── ParameterResolver.cs        # Resolves ".", "@" parameter source prefixes
│
└── Models/
    ├── ConnectionConfig.cs          # POCO for <connection> element
    ├── JobConfig.cs                 # POCO for <job> element
    ├── StepConfig.cs                # POCO for <step> element
    ├── CommandConfig.cs             # POCO for <command> element with nested foreach
    └── ParamConfig.cs               # POCO for <param> element
```

## NuGet Dependencies

- `Microsoft.Data.SqlClient` — SQL Server (modern, maintained)
- `Oracle.ManagedDataAccess.Core` — Oracle (fully managed, no COM/OLEDB)
- `Serilog` + `Serilog.Sinks.Console` + `Serilog.Sinks.File` — Structured logging
- `Polly` — Retry policies for transient DB failures
- `Microsoft.Extensions.Configuration.Json` — appsettings.json support

## Class-by-Class Mapping

| VB6 | C# | Notes |
|-----|----|-------|
| `ETLControl` | `EtlEngine` | Loads XML, manages connections, executes jobs |
| `ETLCommand` | `EtlCommand` | Executes a single command with foreach children |
| `ETLCommands` | `List<EtlCommand>` | No custom collection class needed |
| `ETLConnection` | `EtlConnection` | Wraps `DbConnection`, detects provider from connstring |
| `ETLConnections` | `Dictionary<string, EtlConnection>` | Keyed by connection ID |
| `Globals.bas` | `AdoTypeMapper` + `ParameterResolver` | Split into focused classes |
| `MainModule.bas` | `Program.cs` | Argument parsing + Serilog setup |

## Implementation Order

### Phase 1: Project scaffolding
1. Create .NET 8 console project with NuGet references
2. Create `appsettings.json` with log configuration
3. Create `Program.cs` with argument parsing (`-Job:`, `-XML:`, `--dry-run`)
4. Set up Serilog (console + rolling file sinks)

### Phase 2: Configuration models & XML parsing
5. Create POCO models (`ConnectionConfig`, `JobConfig`, `StepConfig`, `CommandConfig`, `ParamConfig`)
6. Create `AdoTypeMapper` (string → `DbType`, string → `ParameterDirection`)
7. Create `XmlConfigParser` — parse PauloETL.xml with XSD validation, return strongly-typed config

### Phase 3: Connection management
8. Create `EtlConnection` — wraps `SqlConnection` or `OracleConnection`, auto-detects provider from connection string

### Phase 4: Command execution engine
9. Create `ParameterResolver` — resolves ".", "@" prefix parameter sources from parent chain
10. Create `EtlCommand` — the core: loads from `CommandConfig`, executes with ADO.NET, handles foreach recursion, dry-run logging
11. Create `EtlEngine` — orchestrator: loads config, opens connections, iterates job steps, delegates to `EtlCommand`

### Phase 5: Resilience & polish
12. Add Polly retry policies for transient DB failures (connection opens, command executes)
13. Ensure proper `IAsyncDisposable` cleanup of connections and readers
14. Error reporting: structured Serilog context (job, step, command name, row number)

## Key Design Decisions

1. **DbConnection abstraction**: `EtlConnection` holds a `DbConnection` and creates `DbCommand`/`DbParameter` generically. Provider detection from connection string keywords (`sqloledb`/`Server=` → SqlConnection, `OraOLEDB`/`Oracle` → OracleConnection).

2. **DataTable for foreach**: Parent command results loaded into `DataTable` via `DbDataAdapter.Fill()` (equivalent to VB6's `adUseClient` + `adOpenStatic`). Iterate rows with `foreach (DataRow row in table.Rows)`. This disconnects from the connection, matching VB6 behavior.

3. **Parameter source resolution**: `ParameterResolver.Resolve(paramConfig, currentCommand)` walks the parent chain for `.` prefixes, reads `@` for output params, reads plain field names from parent's current `DataRow`.

4. **Oracle stored proc syntax**: VB6 uses ODBC escape `{call pkg.proc(?, ?)}`. ODP.NET needs `CommandType.StoredProcedure` with `CommandText = "pkg.proc"` OR can use anonymous PL/SQL blocks. We'll parse the ODBC escape syntax and convert it.

5. **Dry-run detection**: `EtlCommand` checks `engine.DryRun`. If true and command has a parent (is a child/mutation), log parameters and skip execution. Top-level rowset commands still execute to populate foreach data.

6. **Connection string migration**: The existing XML uses `provider=sqloledb` and `provider=OraOLEDB.Oracle`. We'll strip the provider prefix and map to the correct ADO.NET provider. Users can also update their XML directly.

## CLI Usage

```
PauloETLExecute -Job:Main -XML:C:\path\to\PauloETL.xml [--dry-run] [--log-dir:C:\logs]
```

## Logging Output

Normal mode:
```
[INF] Starting job: Main
[INF] Step 1/5: Set Oracle Role
[INF]   Executing: Set Oracle Role [ORACLE] → SET ROLE equipment_user...
[INF] Step 2/5: Upload Events To Oracle
[INF]   Executing: Get Equipment Events To Send [SQL] → rows: 15
[INF]   Foreach row 1/15: Send Equipment Event To Oracle [ORACLE] → params: {PICSID=42, ScheduleID=100, ...}
[INF]   Foreach row 1/15: Update Event Row In SQL [SQL] → params: {OI_EventID=7, OracleReturnCode=0}
...
[INF] Job Main completed successfully in 12.4s
```

Dry-run mode:
```
[INF] *** DRY RUN MODE — no write operations will be executed ***
[INF] Starting job: Main
[INF] Step 2/5: Upload Events To Oracle
[INF]   Executing: Get Equipment Events To Send [SQL] → rows: 15
[DRY] SKIPPED: Send Equipment Event To Oracle [ORACLE] → would execute with params: {PICSID=42, ScheduleID=100, ...}
[DRY] SKIPPED: Update Event Row In SQL [SQL] → would execute with params: {OI_EventID=7, OracleReturnCode=?}
...
[INF] Dry run complete. 15 events would have been uploaded, 0 schedules would have been downloaded.
```
