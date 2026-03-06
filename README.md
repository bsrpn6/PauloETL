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

## Command-Line Reference

```
PauloETLExecute -Job:<JobID> -XML:<FilePath> [--dry-run] [--log-dir:<Path>] [--verbose]
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
├── PauloETL.sln              # Visual Studio solution
├── PauloETL.xml              # Active ETL job configuration
├── ETLSchema.xsd             # XML schema for config validation
├── ETLSegmentEventsStep.xml  # Segment events step definition (supplemental)
│
├── src/                      # Original VB6 source code (reference)
│   ├── PauloETL_ETLControl.cls
│   ├── PauloETL_ETLCommand.cls
│   ├── PauloETL_ETLConnections.cls
│   ├── PauloETL_ETLConnection.cls
│   ├── PauloETL_Globals.bas
│   ├── PauloETLGUI_frmETLMain.frm
│   ├── PauloETLGUI_frmJobView.frm
│   └── *.vbp / *.vbw / *.vbg   # VB6 project files
│
├── PauloETL.Core/            # Shared class library (.NET 8)
│   ├── Configuration/
│   │   ├── AdoTypeMapper.cs   # XML type strings -> .NET DbType/ParameterDirection
│   │   └── XmlConfigParser.cs # Parses PauloETL.xml with XSD validation
│   ├── Connections/
│   │   └── EtlConnection.cs   # Wraps DbConnection (auto-detects SQL Server vs Oracle)
│   ├── Engine/
│   │   ├── EtlEngine.cs       # Main orchestrator — loads config, runs jobs
│   │   └── EtlCommand.cs      # Executes commands with foreach/parameter resolution
│   └── Models/
│       ├── ConnectionConfig.cs, JobConfig.cs, StepConfig.cs
│       ├── CommandConfig.cs, ParamConfig.cs
│
├── PauloETL.Console/         # Console runner (.NET 8)
│   └── Program.cs             # Entry point, argument parsing, Serilog setup
│
└── PauloETL.Gui/             # WinForms GUI (.NET 8)
    ├── Program.cs             # WinForms entry point
    ├── MainForm.cs            # Load XML, view connections/jobs, test & execute
    └── JobDetailForm.cs       # View steps, execute individual steps
```

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- **Visual Studio 2022** (17.8+) with the ".NET desktop development" workload, or **VS Code** with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension

### Building

**Visual Studio:** Open `PauloETL.sln`, select Debug or Release, and build the solution (Ctrl+Shift+B). Set the startup project to `PauloETL.Console` for the headless runner or `PauloETL.Gui` for the interactive GUI.

**Command line / VS Code:**

```
dotnet build PauloETL.sln
```

This produces two executables:

| Output | Project | Description |
|---|---|---|
| `PauloETLExecute` | PauloETL.Console | Headless console runner (scheduled via Task Scheduler) |
| `PauloETLGui` | PauloETL.Gui | Interactive WinForms GUI for testing and debugging |

> **Note:** The GUI project targets `net8.0-windows` (WinForms). On macOS/Linux the GUI is silently skipped and only the Core library and Console runner are built.

### Running

**Console runner:**

```
PauloETLExecute -Job:Main -XML:C:\VB\PauloETL\PauloETL.xml
```

**GUI:** Launch `PauloETLGui.exe` (or set `PauloETL.Gui` as the startup project in Visual Studio and press F5). The main window lets you load the XML configuration, browse connections and jobs, test connections, and execute jobs interactively.

### Testing with Dry Run

Before running against production databases, use the `--dry-run` flag to validate configuration and parameter resolution without executing any mutation (child) commands:

```
PauloETLExecute -Job:Main -XML:C:\VB\PauloETL\PauloETL.xml --dry-run --verbose
```

In dry-run mode:
- All parent/rowset commands execute normally (read-only queries)
- Child commands (inserts, updates, stored procedure calls) are **logged with their resolved parameter values but not executed**
- The log output shows exactly what would run, making it safe to verify against production data
- Combine with `--verbose` to see debug-level detail including individual parameter bindings

Dry-run logs are written to a separate file (`PauloETL_DryRun_<date>.log`) to keep them distinct from production runs. Use `--log-dir:C:\logs` to control where log files are written.

## History

PauloETL was originally written in **VB6** circa 2010 as a COM DLL (`PauloETL.dll`) with a console runner (`PauloETLExecute.exe`) and a GUI for testing (`PauloETLGUI.exe`).

An automated port to **VB.NET** was attempted using the Visual Studio Upgrade Wizard and `UpgradeHelpers` libraries. That port introduced several behavioral changes (see `ASSESSMENT.md` for a detailed comparison), but is the version currently running in production.

The **C# rewrite** is a clean reimplementation targeting .NET 8. It is structured as three projects sharing a common `PauloETL.Core` library. It preserves the original XML configuration format and the VB6 execution logic, while adding:

- **Structured logging** via Serilog (console + rolling file)
- **Retry policies** via Polly for transient database failures
- **Dry-run mode** for safe testing without database mutations
- **Managed Oracle driver** (ODP.NET Core) — no COM/OLEDB dependency
- **XSD validation** of the configuration file at load time
- **Proper resource management** with `IAsyncDisposable`
- **WinForms GUI** for interactive testing (mirrors the original VB6 GUI)

## Dependencies (C# Version)

| Package | Purpose |
|---|---|
| `Microsoft.Data.SqlClient` | SQL Server connectivity |
| `Oracle.ManagedDataAccess.Core` | Oracle connectivity (fully managed, no native client required) |
| `Serilog` | Structured logging |
| `Polly` | Retry/resilience policies |
