using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using PauloETL.Configuration;
using PauloETL.Connections;
using PauloETL.Models;
using Serilog;

namespace PauloETL.Engine;

/// <summary>
/// Executes a single ETL command with optional foreach iteration over child commands.
/// This is the core execution unit — mirrors the VB6 ETLCommand class.
///
/// Execution flow:
///   1. Resolve parameters from parent command's current row (if any)
///   2. Execute the SQL command
///   3. If rowset=true and has foreach children:
///      - Load results into DataTable (disconnected, like VB6's adUseClient + adOpenStatic)
///      - For each row, execute all child commands sequentially
///   4. If rowset=false:
///      - Execute as non-query
/// </summary>
public sealed class EtlCommand
{
    private readonly CommandConfig _config;
    private readonly EtlConnection _connection;
    private EtlCommand? _parent;
    private readonly List<EtlCommand> _forEachChildren;
    private readonly string _locationContext;
    private readonly ILogger _log;

    // The current row from this command's result set (used by child commands to read parameters)
    private DataRow? _currentRow;

    // The DbCommand instance (holds parameters with output values after execution)
    private DbCommand? _dbCommand;

    public string Name => _config.Name;
    public EtlCommand? Parent => _parent;
    public DataRow? CurrentRow => _currentRow;
    public DbCommand? DbCommand => _dbCommand;

    private EtlCommand(
        CommandConfig config,
        EtlConnection connection,
        EtlCommand? parent,
        List<EtlCommand> children,
        string locationContext)
    {
        _config = config;
        _connection = connection;
        _parent = parent;
        _forEachChildren = children;
        _locationContext = locationContext;
        _log = Log.ForContext("Command", config.Name);
    }

    /// <summary>
    /// Builds the full command tree from configuration, opening connections as needed.
    /// Mirrors ETLCommand.LoadFromXML + ETLCommands.LoadFromXML recursive loading.
    /// </summary>
    public static async Task<EtlCommand> BuildAsync(
        CommandConfig config,
        Dictionary<string, EtlConnection> connections,
        string parentLocation,
        EtlCommand? parent = null,
        CancellationToken ct = default)
    {
        if (!config.Enabled)
        {
            Log.Debug("Command '{CommandName}' is disabled, creating no-op", config.Name);
        }

        var location = $"{parentLocation}\nCommand: {config.Name}";

        if (!connections.TryGetValue(config.ConnId, out var connection))
            throw new InvalidOperationException(
                $"Connection '{config.ConnId}' not found for command '{config.Name}'");

        if (!connection.IsOpen)
            await connection.OpenAsync(ct);

        // Recursively build child commands
        var children = new List<EtlCommand>(config.ForEachChildren.Count);
        foreach (var childConfig in config.ForEachChildren)
        {
            var child = await BuildAsync(childConfig, connections, location, parent: null, ct);
            children.Add(child);
        }

        var command = new EtlCommand(config, connection, parent, children, location);

        // Set parent reference on children (must be done after construction)
        foreach (var child in children)
        {
            child.SetParent(command);
        }

        return command;
    }

    private void SetParent(EtlCommand parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Executes the command. If it returns a rowset with foreach children,
    /// iterates each row and executes all children per row.
    /// Mirrors ETLCommand.Execute() from VB6.
    /// </summary>
    public async Task<bool> ExecuteAsync(bool dryRun, CancellationToken ct = default)
    {
        if (!_config.Enabled)
        {
            _log.Debug("Skipping disabled command: {CommandName}", Name);
            return true;
        }

        try
        {
            _dbCommand = CreateDbCommand();
            ResolveParameters();

            // Dry-run: if this command has a parent (it's a mutation child in a foreach),
            // log what would happen but don't execute
            if (dryRun && _parent != null)
            {
                LogDryRun();
                return true;
            }

            if (_config.Rowset)
            {
                return await ExecuteWithRowsetAsync(dryRun, ct);
            }
            else
            {
                return await ExecuteNonQueryAsync(ct);
            }
        }
        catch (DbException ex)
        {
            _log.Error(ex, "Database error executing command '{CommandName}' [{ConnectionId}]\n{Location}",
                Name, _config.ConnId, _locationContext);
            return false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unexpected error executing command '{CommandName}'\n{Location}",
                Name, _locationContext);
            return false;
        }
    }

    private async Task<bool> ExecuteWithRowsetAsync(bool dryRun, CancellationToken ct)
    {
        _log.Information("Executing: {CommandName} [{ConnectionId}] (rowset)",
            Name, _config.ConnId);

        // Load into DataTable — equivalent to VB6's adUseClient + adOpenStatic
        // This disconnects from the server, allowing the connection to be reused by children
        using var adapter = CreateDataAdapter(_dbCommand!);
        var table = new DataTable();
        adapter.Fill(table);

        var rowCount = table.Rows.Count;
        _log.Information("  {CommandName} returned {RowCount} rows", Name, rowCount);

        if (rowCount == 0 || _forEachChildren.Count == 0)
            return true;

        // Iterate rows sequentially — child params depend on current row position
        for (int i = 0; i < rowCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            _currentRow = table.Rows[i];

            _log.Debug("  Foreach row {RowNum}/{RowCount} of {CommandName}",
                i + 1, rowCount, Name);

            foreach (var child in _forEachChildren)
            {
                if (!await child.ExecuteAsync(dryRun, ct))
                {
                    _log.Error("Child command '{ChildName}' failed at row {RowNum}/{RowCount}",
                        child.Name, i + 1, rowCount);
                    return false;
                }
            }
        }

        _currentRow = null;
        return true;
    }

    private async Task<bool> ExecuteNonQueryAsync(CancellationToken ct)
    {
        _log.Information("Executing: {CommandName} [{ConnectionId}] (non-query)",
            Name, _config.ConnId);
        LogParameterValues();

        await _dbCommand!.ExecuteNonQueryAsync(ct);
        return true;
    }

    /// <summary>
    /// Resolves parameter values from parent command's current row or output parameters.
    /// Mirrors the parameter resolution logic in ETLCommand.Execute() lines 51-66:
    ///
    ///   - Plain "FieldName":  reads from parent's current DataRow[FieldName]
    ///   - ".FieldName":       dots walk up the parent chain (each dot = one level up)
    ///   - "@ParamName":       reads from parent's DbCommand output parameter value
    /// </summary>
    private void ResolveParameters()
    {
        if (_parent == null || _config.Parameters.Count == 0)
            return;

        for (int i = 0; i < _config.Parameters.Count; i++)
        {
            var paramConfig = _config.Parameters[i];
            var source = paramConfig.Source;
            var sourceCommand = _parent;

            // Walk up parent chain for each leading "." prefix
            while (source.StartsWith('.'))
            {
                sourceCommand = sourceCommand.Parent
                    ?? throw new InvalidOperationException(
                        $"Parameter '{paramConfig.Name}' source '{paramConfig.Source}' has more '.' prefixes than parent levels exist");
                source = source[1..];
            }

            object? value;
            if (source.StartsWith('@'))
            {
                // Read from parent command's output parameter
                var paramName = source[1..];
                var srcParam = sourceCommand.DbCommand?.Parameters.Cast<DbParameter>()
                    .FirstOrDefault(p => p.ParameterName == paramName)
                    ?? throw new InvalidOperationException(
                        $"Output parameter '{paramName}' not found on command '{sourceCommand.Name}'");
                value = srcParam.Value;
            }
            else
            {
                // Read from parent command's current DataRow
                var row = sourceCommand.CurrentRow
                    ?? throw new InvalidOperationException(
                        $"Command '{sourceCommand.Name}' has no current row for parameter '{paramConfig.Name}'");

                if (!row.Table.Columns.Contains(source))
                    throw new InvalidOperationException(
                        $"Column '{source}' not found in result set of command '{sourceCommand.Name}'. " +
                        $"Available columns: {string.Join(", ", row.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

                value = row[source];
            }

            _dbCommand!.Parameters[i].Value = value == DBNull.Value ? DBNull.Value : value;
        }
    }

    private DbCommand CreateDbCommand()
    {
        var cmd = _connection.CreateCommand();
        var sqlText = _config.SqlText;

        // Detect Oracle stored procedure call syntax: {call pkg.proc(?, ?, ...)}
        // ODP.NET doesn't support ODBC escape syntax — convert to CommandType.StoredProcedure
        if (_connection.IsOracle && IsOdbcCallSyntax(sqlText))
        {
            var (procName, paramCount) = ParseOdbcCall(sqlText);
            cmd.CommandText = procName;
            cmd.CommandType = CommandType.StoredProcedure;
        }
        else if (!_connection.IsOracle && sqlText.StartsWith("EXEC ", StringComparison.OrdinalIgnoreCase))
        {
            // SQL Server: convert "EXEC dbo.spName ?, ?" to stored procedure call
            var parts = sqlText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                cmd.CommandText = parts[1].TrimEnd(',');
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                cmd.CommandText = sqlText;
                cmd.CommandType = CommandType.Text;
            }
        }
        else
        {
            // Plain SQL (e.g., SET ROLE ...)
            cmd.CommandText = sqlText;
            cmd.CommandType = CommandType.Text;
        }

        // Create parameters
        foreach (var paramConfig in _config.Parameters)
        {
            var dbParam = cmd.CreateParameter();
            dbParam.ParameterName = paramConfig.Name;
            dbParam.DbType = AdoTypeMapper.MapDataType(paramConfig.Type);
            dbParam.Direction = AdoTypeMapper.MapDirection(paramConfig.Direction);
            if (paramConfig.Size.HasValue)
                dbParam.Size = paramConfig.Size.Value;
            cmd.Parameters.Add(dbParam);
        }

        return cmd;
    }

    private static bool IsOdbcCallSyntax(string sql)
    {
        return sql.TrimStart().StartsWith("{call", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses ODBC call escape syntax: "{call pkg.proc (?, ?, ?)}" into procedure name and param count.
    /// </summary>
    private static (string ProcName, int ParamCount) ParseOdbcCall(string sql)
    {
        var match = Regex.Match(sql, @"\{\s*call\s+([\w$.]+)\s*\(([^)]*)\)\s*\}",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            throw new InvalidOperationException($"Cannot parse ODBC call syntax: '{sql}'");

        var procName = match.Groups[1].Value;
        var paramList = match.Groups[2].Value.Trim();
        var paramCount = string.IsNullOrEmpty(paramList)
            ? 0
            : paramList.Split(',').Length;

        return (procName, paramCount);
    }

    private DbDataAdapter CreateDataAdapter(DbCommand cmd)
    {
        if (_connection.IsOracle)
            return new OracleDataAdapter((OracleCommand)cmd);

        return new SqlDataAdapter((SqlCommand)cmd);
    }

    private void LogDryRun()
    {
        var paramValues = new Dictionary<string, object?>();
        if (_dbCommand != null)
        {
            foreach (DbParameter p in _dbCommand.Parameters)
            {
                paramValues[p.ParameterName] = p.Value;
            }
        }

        _log.Warning("[DRY RUN] SKIPPED: {CommandName} [{ConnectionId}] — would execute with params: {@Params}",
            Name, _config.ConnId, paramValues);
    }

    private void LogParameterValues()
    {
        if (_dbCommand == null || _dbCommand.Parameters.Count == 0)
            return;

        foreach (DbParameter p in _dbCommand.Parameters)
        {
            _log.Debug("    Param {ParamName} = {ParamValue}", p.ParameterName, p.Value);
        }
    }
}
