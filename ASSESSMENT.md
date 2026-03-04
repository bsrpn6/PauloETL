# PauloETL Assessment: Windows Server 2022 Compatibility & Modernization

**Date:** March 2026
**Project:** PauloETL — Bi-directional ETL between SQL Server (PauloWIP) and Oracle (PICS/DAQ)

---

## Executive Summary

PauloETL is a **VB6 (Visual Basic 6.0)** application — not VB.NET — consisting of a COM DLL, a console runner, and a GUI. It synchronizes equipment events, shop order weights, and production schedules between SQL Server and Oracle databases. While the architecture is well-designed with proper OOP patterns and XML-driven configuration, the underlying technology stack is fully deprecated and presents real risks on Windows Server 2022.

---

## A) Will This Run on Windows Server 2022?

### Short Answer: **Likely yes, but with caveats and risk.**

### What Works

Windows Server 2022 still includes:
- **VB6 Runtime (msvbvm60.dll)** — Microsoft has committed to shipping this with Windows for backward compatibility through at least Windows 11 / Server 2022.
- **MSXML 6.0** — Still included in Windows Server 2022.
- **ADO 2.8 (msado15.dll)** — Still present as part of Windows MDAC/WDAC components.
- **32-bit subsystem (WOW64)** — Server 2022 supports 32-bit applications.

### What May Break

| Component | Risk | Details |
|-----------|------|---------|
| **MSCOMCTL.OCX** | Medium | The Microsoft Common Controls OCX has a history of registration issues on newer Windows. The GUI may fail to load. |
| **MSDATGRD.OCX** | Medium | The DataGrid control is a legacy OCX that may not be registered by default on Server 2022. |
| **sqloledb** | Medium-High | Microsoft deprecated the SQL Server OLEDB provider. It ships with Windows but receives no updates. If connecting to SQL Server 2019+, there may be TLS/protocol negotiation issues. |
| **OraOLEDB.Oracle** | High | This Oracle OLEDB provider must be installed separately. Oracle may not provide a version tested against Server 2022 for the version you are running. If Oracle client libraries are upgraded, COM registration may break. |
| **COM Registration** | Medium | PauloETL.dll must be registered via `regsvr32`. Security policy changes in Server 2022 (Credential Guard, HVCI) may interfere with COM registration or execution. |
| **Hardcoded Paths** | Low | The GUI has a hardcoded fallback path (`C:\VB\PauloETL\PauloETL.xml`). This is cosmetic but indicates fragility. |
| **DEP/ASLR** | Low-Medium | Modern Windows enforces stricter memory protections. VB6 binaries were not compiled with these in mind and may trigger application compatibility warnings. |

### Recommendations for Running As-Is on Server 2022

1. **Test thoroughly** in a staging Server 2022 environment before migrating production.
2. Install the **Oracle Client** (matching architecture — 32-bit) and verify OraOLEDB registration.
3. Consider switching from `sqloledb` to **Microsoft OLE DB Driver for SQL Server (MSOLEDBSQL)** in PauloETL.xml — it's a drop-in connection string change.
4. Register all OCX controls manually if the GUI is needed.
5. Run PauloETLExecute.exe in **Windows Compatibility Mode** (Windows 7 or Windows Server 2008 R2) if issues arise.
6. If only the console runner (`PauloETLExecute.exe`) is used in production (likely scheduled via Task Scheduler), the OCX/GUI risks are irrelevant.

---

## B) Is There a Better Approach to This ETL Process?

### What the Current Process Does Well

- **XML-driven configuration** — Jobs, steps, commands, and parameters are all externalized. No code changes needed to modify SQL or add steps.
- **Nested command pattern** — Parent result sets drive child commands, which is a natural fit for the hierarchical schedule download.
- **Separation of concerns** — The DLL/EXE/GUI split is clean.
- **Parameterized queries** — Uses ADODB.Parameter objects rather than string concatenation (good for security and correctness).

### What Could Be Better

| Issue | Impact | Modern Alternative |
|-------|--------|--------------------|
| **No error recovery / retry** | A single network blip kills the entire job | Retry policies (Polly library in .NET), dead-letter queues |
| **No transaction management** | Partial uploads/downloads can leave data inconsistent | Database transactions wrapping logical units of work |
| **Single-threaded / synchronous** | The entire ETL runs serially; long Oracle queries block everything | Async I/O, parallel step execution, producer-consumer patterns |
| **No logging framework** | Only `Debug.Print`; no log files in production | Structured logging (Serilog, NLog) with file/event log sinks |
| **No monitoring / alerting** | Failures are silent unless someone checks | Health checks, email/Slack alerts on failure, Windows Event Log |
| **Tightly coupled to ADO/COM** | Can't swap database providers without code changes | Repository pattern, dependency injection, ADO.NET abstractions |
| **Credentials in XML** | `process/process` and `givemedata` are in plain text | Windows Authentication, Azure Key Vault, DPAPI encrypted config |

### Modern ETL Alternatives to Consider

1. **SSIS (SQL Server Integration Services)** — Microsoft's enterprise ETL tool. Excellent for SQL Server ↔ Oracle. Visual designer, built-in logging, restart on failure. Already licensed if you have SQL Server.

2. **Azure Data Factory** — Cloud-based ETL/ELT. Good if moving toward Azure. Supports both SQL Server and Oracle connectors.

3. **Custom .NET Console App** — A modern C# rewrite (see Section C). Best option if you want to preserve the current XML-driven architecture and keep full control.

4. **Apache Airflow / Prefect** — Python-based orchestration. Overkill for this use case but worth mentioning if the team has Python skills.

**Recommendation:** If the current logic works and the team is .NET-focused, a **C# console application** is the most natural migration path. It preserves your existing investment in the XML configuration and stored procedures while modernizing the runtime. SSIS is a good alternative if you want out of custom code entirely.

---

## C) What Will It Take to Convert to C#?

### Scope Assessment

The VB6 codebase is **moderate in size but well-structured**, which makes conversion feasible:

| Component | VB6 Files | Estimated C# Effort |
|-----------|-----------|---------------------|
| Globals module | 1 file (~150 lines) | Static helper class |
| ETLControl | 1 class (~300 lines) | Main orchestrator class |
| ETLConnection/ETLConnections | 2 classes (~200 lines) | Connection manager with ADO.NET |
| ETLCommand/ETLCommands | 2 classes (~500 lines) | Command executor with ADO.NET |
| Console runner | 1 module (~50 lines) | Program.cs with argument parsing |
| GUI forms | 3 forms | Optional — WinForms or skip entirely |
| XML Schema | 1 XSD | Reusable as-is or convert to strongly-typed config |
| XML Config | 1 XML | Reusable as-is with minor adjustments |

### Key Conversion Tasks

#### 1. Project Setup
- Create a .NET 8 (LTS) Console Application
- Add NuGet packages:
  - `System.Data.SqlClient` or `Microsoft.Data.SqlClient`
  - `Oracle.ManagedDataAccess.Core` (Oracle's managed ODP.NET — no COM dependency!)
  - `System.Xml` (built-in)
  - `Serilog` + sinks for logging
  - `Microsoft.Extensions.Configuration` for config management
  - `Polly` for retry/resilience policies

#### 2. Database Layer (Biggest Change)
- Replace `ADODB.Connection` → `SqlConnection` / `OracleConnection`
- Replace `ADODB.Command` → `SqlCommand` / `OracleCommand`
- Replace `ADODB.Recordset` → `SqlDataReader` / `OracleDataReader` (or `DataTable`)
- Replace `ADODB.Parameter` → `SqlParameter` / `OracleParameter`
- ADO data type constants (`adVarChar`, `adInteger`) → .NET `DbType` or provider-specific types

#### 3. XML Processing
- Replace `MSXML2.DOMDocument60` → `System.Xml.XmlDocument` or `System.Xml.Linq.XDocument`
- XPath queries work identically in .NET
- Schema validation via `XmlSchemaSet`

#### 4. Core Logic Translation
- VB6 `Collection` → `Dictionary<string, T>` or `List<T>`
- VB6 `For Each` over COM collections → standard `foreach`
- `On Error GoTo` → `try/catch/finally`
- `Property Get/Let/Set` → C# properties
- `Optional` parameters → C# optional parameters with defaults
- `Variant` → `object` (but prefer strong typing)
- `Debug.Print` → `ILogger.LogDebug()`

#### 5. Configuration Modernization (Optional but Recommended)
- Keep `PauloETL.xml` as the job definition format (it's well-designed)
- Move connection strings to `appsettings.json` with encryption
- Use `Microsoft.Extensions.Configuration` for environment-specific overrides

#### 6. GUI (Optional)
- If the GUI is needed: Convert to WinForms (.NET 8 supports WinForms)
- If the GUI is only used for testing/debugging: Skip it, use logging instead
- Alternative: Build a simple web dashboard with Blazor

### Estimated Level of Effort

| Phase | Tasks | Estimate |
|-------|-------|----------|
| **Phase 1: Console App** | Project setup, XML parsing, connection management, command execution, argument parsing | Small-Medium |
| **Phase 2: Hardening** | Logging, retry policies, transaction support, error handling, credential management | Small |
| **Phase 3: Testing** | Integration testing against SQL Server and Oracle, regression testing against current VB6 output | Medium |
| **Phase 4: GUI (if needed)** | WinForms port of job viewer and debug forms | Small-Medium |

### Risks & Considerations

1. **Oracle Stored Procedure Calls** — The ODBC escape syntax `{call package.procedure(?, ?)}` works differently in ODP.NET. You'll use `OracleCommand.CommandType = CommandType.StoredProcedure` instead.
2. **Parameter Direction Mapping** — VB6 uses `adParamInputOutput`; ODP.NET uses `OracleDbType` with `Direction` property. Test thoroughly with Oracle output parameters.
3. **Recordset vs DataReader** — ADO Recordsets are scrollable and disconnected; ADO.NET DataReaders are forward-only. The nested foreach pattern may need `DataTable` instead of `DataReader` to allow multiple active result sets.
4. **MARS (Multiple Active Result Sets)** — SQL Server supports this but Oracle does not. The nested command pattern must close parent readers before opening child readers on the same Oracle connection (or use separate connections).
5. **Existing Stored Procedures** — All SQL Server and Oracle stored procedures remain unchanged. The C# app calls the same procs with the same parameters.

### Migration Strategy

The safest approach is a **parallel run**:
1. Build the C# version
2. Run both VB6 and C# side-by-side on the same schedule
3. Compare outputs/database state after each run
4. Once validated, retire the VB6 version

---

## Recommendation

**For the immediate Server 2022 migration:** The VB6 application will *probably* run, but test it thoroughly in a staging environment first. The highest risk is the Oracle OLEDB provider.

**For the medium term:** Convert to a C# .NET 8 console application. The codebase is well-structured and moderate in size, making this a tractable project. This eliminates all COM/VB6 runtime dependencies and gives you a supported, maintainable platform for years to come.

**Skip SSIS** unless you want to hand off ETL ownership to a DBA team. The XML-driven architecture you already have is essentially a lightweight, custom ETL framework — rewriting it in C# preserves that flexibility.
