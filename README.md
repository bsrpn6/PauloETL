# PauloETL

A bi-directional ETL (Extract-Transform-Load) application that synchronizes production data between a SQL Server WIP (Work In Progress) database and an Oracle PICS/DAQ manufacturing execution system.

PauloETL is used in Paulo Products Company's heat treatment facilities to keep shop floor equipment controllers in sync with the Oracle production scheduling system.

## What It Does

PauloETL runs as a scheduled console application that performs two main operations:

### Upload (SQL Server -> Oracle)

1. **Equipment Events** — Reads equipment events (furnace start/stop, load/unload, recipe execution) from SQL Server and sends them to Oracle via `qdaq$PK_SCHEDULE_IQ_PLC.SendEquipmentEvent`. Marks each event as sent in SQL Server.

2. **Shop Order Weights** — Reads actual weights recorded by shop floor scales from SQL Server and sends them to Oracle via `qdaq$PK_SCHEDULE_IQ_PLC.SetActualWeight`. Marks each weight record as sent.

3. **Gross Weights** — Same pattern for gross weight data via `qdaq$PK_SCHEDULE_IQ_PLC.SetActualGrossWeight`.

4. **Segment Events** *(optional step)* — Sends heat treatment segment timing events to Oracle via `qdaq$PK_SCHEDULE_IQ_PLC.SendHHEvent`.

### Download (Oracle -> SQL Server)

5. **Production Schedule Download** — For each cost center:
   - Initializes the cost center for download (`oiBHTSchedCCDownloadStart`)
   - Retrieves the full production queue from Oracle (`qdaq$PK_SCHEDULE_IQ_PLC.GetQueue`)
   - Loads each schedule entry into SQL Server staging tables
   - Identifies new/changed entries and downloads their details:
     - **Sequence step detail** — Processing steps and recipe parameters
     - **Train detail** — Multi-furnace train routing
     - **Furnace recipes** — Full recipe segments with temperature, carbon, quench parameters
   - Builds the final schedule data (`oiBHTSchedBuildScheduleID`)
   - Marks the cost center download complete (`oiBHTSchedCCDownloadEnd`)

### Data Flow Diagram

```
SQL Server (PauloWIP)                        Oracle (PICS/DAQ)
┌─────────────────────┐                      ┌─────────────────────┐
│                     │  Equipment Events    │                     │
│  Equipment Events   │ ──────────────────>  │  PK_SCHEDULE_IQ_PLC │
│  Shop Order Weights │ ──────────────────>  │                     │
│  Gross Weights      │ ──────────────────>  │                     │
│                     │                      │                     │
│  Schedule Tables    │ <──────────────────  │  Production Queue   │
│  Sequence Steps     │ <──────────────────  │  Schedule Details   │
│  Train Details      │ <──────────────────  │  Train Routing      │
│  Furnace Recipes    │ <──────────────────  │  Recipe Segments    │
└─────────────────────┘                      └─────────────────────┘
```

## Architecture

PauloETL uses an **XML-driven nested command pattern**. All SQL statements, stored procedure calls, connection strings, and parameter mappings are defined in `PauloETL.xml` — no code changes are needed to modify queries or add steps.

### Nested Command Execution

The key architectural pattern is **parent-child command chaining**:

1. A parent command executes a query that returns rows
2. For each row in the result set, child commands execute using column values from the parent row as their parameter values
3. Child commands can themselves return rows and have their own children (up to any depth)

This naturally models the ETL flow: "Get all pending events" -> "For each event, send to Oracle" -> "For each send result, mark as sent in SQL".

### Parameter Resolution

Parameters bind child commands to parent data using the `source` attribute:

| Source Format | Meaning |
|---|---|
| `ColumnName` | Read from the parent command's current row |
| `.ColumnName` | Read from the grandparent command's current row (dot prefix walks up one level) |
| `@ParamName` | Read from the parent command's parameter value (for passing parameters down the chain) |

## XML Configuration

The configuration file (`PauloETL.xml`) defines connections, jobs, steps, and commands:

```xml
<root>
  <connections>
    <connection id="SQL" connstring="..." name="..." uid="..." pwd="..."/>
    <connection id="ORACLE" connstring="..." name="..." uid="..." pwd="..."/>
  </connections>
  <jobs>
    <job id="Main" name="...">
      <step name="Step Name">
        <command name="..." rowset="true|false" connid="SQL|ORACLE" enabled="true|false">
          <![CDATA[SQL or stored procedure call]]>
          <params>
            <param name="..." source="..." type="adinteger|advarchar|..." direction="adparaminput" size="..."/>
          </params>
          <foreach>
            <!-- child commands executed for each row -->
          </foreach>
        </command>
      </step>
    </job>
  </jobs>
</root>
```

### Supported Parameter Types

| XML Type | Description |
|---|---|
| `adinteger` | 32-bit integer |
| `adchar` | Fixed-length character string |
| `advarchar` | Variable-length character string |
| `adsingle` | Single-precision float |
| `addate` | Date/time |
| `adcursor` | Oracle REF CURSOR (output only) |

### Supported Parameter Directions

| XML Direction | Description |
|---|---|
| `adparaminput` | Input parameter |
| `adparamoutput` | Output parameter |
| `adparaminputoutput` | Input/output parameter |
| `adparamreturnvalue` | Return value |

## Usage

```
PauloETLExecute -Job:Main -XML:C:\path\to\PauloETL.xml [--dry-run] [--log-dir:C:\logs] [--verbose]
```

| Argument | Required | Description |
|---|---|---|
| `-Job:<JobID>` | Yes | Job ID to execute (e.g., `Main`) |
| `-XML:<FilePath>` | Yes | Path to the ETL configuration XML file |
| `--dry-run` | No | Log all commands with resolved parameters but skip mutation commands |
| `--log-dir:<Path>` | No | Directory for log files (defaults to current directory) |
| `--verbose` | No | Enable debug-level logging |

### Exit Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Invalid arguments |
| 2 | Completed with errors |
| 3 | Cancelled |
| 99 | Unhandled exception |

### Typical Deployment

PauloETL is run on a schedule via Windows Task Scheduler, typically every few minutes, to keep the SQL Server and Oracle databases synchronized.

## Project Structure

```
PauloETL/
├── PauloETL.xml              # Active ETL job configuration
├── ETLSchema.xsd             # XML schema for config validation
├── ETLSegmentEventsStep.xml  # Segment events step definition (supplemental)
│
├── src/                      # Original VB6 source code
│   ├── PauloETL_ETLControl.cls
│   ├── PauloETL_ETLCommand.cls
│   ├── PauloETL_ETLCommands.cls
│   ├── PauloETL_ETLConnection.cls
│   ├── PauloETL_ETLConnections.cls
│   ├── PauloETL_Globals.bas
│   ├── PauloETL_frmETLCommand.frm
│   ├── PauloETL_frmSingleStep.frm
│   ├── PauloETLGUI_frmETLMain.frm
│   ├── PauloETLGUI_frmJobView.frm
│   └── *.vbp / *.vbw / *.vbg   # VB6 project files
│
└── PauloETL.CSharp/          # Modern C# rewrite (.NET 8+)
    ├── Program.cs             # Entry point, argument parsing, logging setup
    ├── Configuration/
    │   ├── AdoTypeMapper.cs   # XML type strings -> .NET DbType/ParameterDirection
    │   └── XmlConfigParser.cs # Parses PauloETL.xml with XSD validation
    ├── Connections/
    │   └── EtlConnection.cs   # Wraps DbConnection (auto-detects SQL Server vs Oracle)
    ├── Engine/
    │   ├── EtlEngine.cs       # Main orchestrator — loads config, runs jobs
    │   └── EtlCommand.cs      # Executes commands with foreach/parameter resolution
    └── Models/
        ├── ConnectionConfig.cs
        ├── JobConfig.cs
        ├── StepConfig.cs
        ├── CommandConfig.cs
        └── ParamConfig.cs
```

## History

PauloETL was originally written in **VB6** circa 2010 as a COM DLL (`PauloETL.dll`) with a console runner (`PauloETLExecute.exe`) and a GUI for testing (`PauloETLGUI.exe`).

An automated port to **VB.NET** was attempted using the Visual Studio Upgrade Wizard and `UpgradeHelpers` libraries. That port introduced several behavioral changes (see `ASSESSMENT.md` for a detailed comparison), but is the version currently running in production.

The **C# rewrite** in `PauloETL.CSharp/` is a clean reimplementation targeting modern .NET. It preserves the original XML configuration format and the VB6 execution logic, while adding:

- **Structured logging** via Serilog (console + rolling file)
- **Retry policies** via Polly for transient database failures
- **Dry-run mode** for safe testing without database mutations
- **Managed Oracle driver** (ODP.NET Core) — no COM/OLEDB dependency
- **XSD validation** of the configuration file at load time
- **Proper resource management** with `IAsyncDisposable`

## Dependencies (C# Version)

| Package | Purpose |
|---|---|
| `Microsoft.Data.SqlClient` | SQL Server connectivity |
| `Oracle.ManagedDataAccess.Core` | Oracle connectivity (fully managed, no native client required) |
| `Serilog` | Structured logging |
| `Polly` | Retry/resilience policies |
