using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using PauloETL.Models;
using Polly;
using Polly.Retry;
using Serilog;

namespace PauloETL.Connections;

/// <summary>
/// Wraps a DbConnection (SqlConnection or OracleConnection) with provider auto-detection.
/// Mirrors the VB6 ETLConnection class.
///
/// The original VB6 used OLEDB providers (sqloledb, OraOLEDB.Oracle).
/// This class detects the provider from the legacy connection string and creates the
/// appropriate ADO.NET connection, stripping/transforming the connection string as needed.
/// </summary>
public sealed class EtlConnection : IAsyncDisposable
{
    private DbConnection? _connection;
    private readonly ConnectionConfig _config;
    private readonly ILogger _log;

    public string Id => _config.Id;
    public string Name => _config.Name;
    public bool IsOracle { get; }

    /// <summary>
    /// Retry policy for transient connection failures.
    /// Retries up to 3 times with exponential backoff (2s, 4s, 8s).
    /// </summary>
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<DbException>(),
            OnRetry = args =>
            {
                Log.Warning(args.Outcome.Exception,
                    "Database connection attempt {Attempt} failed, retrying in {Delay}...",
                    args.AttemptNumber + 1, args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public EtlConnection(ConnectionConfig config)
    {
        _config = config;
        _log = Log.ForContext("ConnectionId", config.Id);
        IsOracle = DetectOracle(config.ConnString);
    }

    public bool IsOpen => _connection?.State == ConnectionState.Open;

    /// <summary>
    /// Gets the underlying DbConnection. Throws if not open.
    /// </summary>
    public DbConnection Connection =>
        _connection ?? throw new InvalidOperationException($"Connection '{Id}' is not open");

    /// <summary>
    /// Opens the database connection with retry logic.
    /// Mirrors VB6 ETLConnection.OpenConnection().
    /// </summary>
    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (IsOpen)
            return;

        _connection = CreateConnection();
        _log.Information("Opening connection {ConnectionId} ({ConnectionName})", Id, Name);

        await RetryPipeline.ExecuteAsync(async token =>
        {
            if (_connection.State != ConnectionState.Closed)
            {
                await _connection.CloseAsync();
            }
            await _connection.OpenAsync(token);
        }, ct);

        _log.Information("Connection {ConnectionId} opened successfully", Id);
    }

    /// <summary>
    /// Creates a DbCommand on this connection.
    /// </summary>
    public DbCommand CreateCommand()
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandTimeout = 120;
        return cmd;
    }

    private DbConnection CreateConnection()
    {
        var connString = TransformConnectionString(_config.ConnString, _config.Uid, _config.Pwd);

        if (IsOracle)
        {
            _log.Debug("Creating OracleConnection for {ConnectionId}", Id);
            return new OracleConnection(connString);
        }

        _log.Debug("Creating SqlConnection for {ConnectionId}", Id);
        return new SqlConnection(connString);
    }

    /// <summary>
    /// Transforms legacy OLEDB connection strings to ADO.NET format.
    /// Strips 'Provider=xxx;' and adds User ID / Password if needed.
    /// </summary>
    private static string TransformConnectionString(string oleDbConnString, string uid, string pwd)
    {
        // Remove OLEDB provider specification
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            oleDbConnString,
            @"Provider\s*=\s*[^;]+;?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove OLEDB-specific properties not used in ADO.NET
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"DistribTX\s*=\s*[^;]+;?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"PLSQLRSet\s*=\s*[^;]+;?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Strip surrounding single quotes from values (e.g., 'sqloledb' -> sqloledb)
        cleaned = cleaned.Replace("'", "");

        // Ensure trailing semicolon
        cleaned = cleaned.TrimEnd();
        if (!cleaned.EndsWith(';'))
            cleaned += ";";

        // Add credentials
        cleaned += $"User ID={uid};Password={pwd};";

        return cleaned;
    }

    private static bool DetectOracle(string connString)
    {
        var upper = connString.ToUpperInvariant();
        return upper.Contains("ORAOLEDB") || upper.Contains("ORACLE");
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            if (_connection.State != ConnectionState.Closed)
            {
                _log.Debug("Closing connection {ConnectionId}", Id);
                await _connection.CloseAsync();
            }
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
