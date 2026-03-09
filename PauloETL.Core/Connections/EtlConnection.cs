using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
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
    private static bool _oracleConfigured;
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
        IsOracle = DetectOracle(config.ConnString, config.Id);
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
        var connString = BuildConnectionString();

        // Log the transformed connection string with password masked for diagnostics
        var maskedConnString = System.Text.RegularExpressions.Regex.Replace(
            connString,
            @"Password\s*=\s*[^;]+",
            "Password=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        _log.Debug("Transformed connection string for {ConnectionId}: {ConnString}", Id, maskedConnString);

        if (IsOracle)
        {
            EnsureOracleTnsAdmin();
            return new OracleConnection(connString);
        }

        return new SqlConnection(connString);
    }

    /// <summary>
    /// Configures ODP.NET Managed to find tnsnames.ora by setting OracleConfiguration.TnsAdmin.
    /// The original VB6 app used OraOLEDB.Oracle which relied on the Oracle client installation
    /// to resolve TNS aliases. ODP.NET Managed doesn't automatically find tnsnames.ora, so we
    /// check TNS_ADMIN, then ORACLE_HOME/network/admin, then scan the registry.
    /// </summary>
    private static void EnsureOracleTnsAdmin()
    {
        if (_oracleConfigured)
            return;
        _oracleConfigured = true;

        // If TnsAdmin is already set (e.g., by user code or config), leave it alone
        if (!string.IsNullOrEmpty(OracleConfiguration.TnsAdmin))
        {
            Log.Debug("OracleConfiguration.TnsAdmin already set: {TnsAdmin}", OracleConfiguration.TnsAdmin);
            return;
        }

        // 1. Check TNS_ADMIN environment variable
        var tnsAdmin = Environment.GetEnvironmentVariable("TNS_ADMIN");
        if (!string.IsNullOrEmpty(tnsAdmin) && Directory.Exists(tnsAdmin))
        {
            OracleConfiguration.TnsAdmin = tnsAdmin;
            Log.Information("Set OracleConfiguration.TnsAdmin from TNS_ADMIN env var: {TnsAdmin}", tnsAdmin);
            return;
        }

        // 2. Check ORACLE_HOME/network/admin
        var oracleHome = Environment.GetEnvironmentVariable("ORACLE_HOME");
        if (!string.IsNullOrEmpty(oracleHome))
        {
            var networkAdmin = Path.Combine(oracleHome, "network", "admin");
            if (File.Exists(Path.Combine(networkAdmin, "tnsnames.ora")))
            {
                OracleConfiguration.TnsAdmin = networkAdmin;
                Log.Information("Set OracleConfiguration.TnsAdmin from ORACLE_HOME: {TnsAdmin}", networkAdmin);
                return;
            }
        }

        // 3. On Windows, check the registry for Oracle home paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var tnsFromRegistry = FindTnsAdminFromRegistry();
            if (tnsFromRegistry != null)
            {
                OracleConfiguration.TnsAdmin = tnsFromRegistry;
                Log.Information("Set OracleConfiguration.TnsAdmin from registry: {TnsAdmin}", tnsFromRegistry);
                return;
            }
        }

        Log.Warning("Could not locate tnsnames.ora. Set the TNS_ADMIN environment variable " +
            "to the directory containing tnsnames.ora, or use a full TNS descriptor in the " +
            "connection string Data Source.");
    }

    /// <summary>
    /// Scans the Windows registry for Oracle home directories that contain tnsnames.ora.
    /// </summary>
    private static string? FindTnsAdminFromRegistry()
    {
        string[] registryKeys =
        [
            @"SOFTWARE\Oracle",
            @"SOFTWARE\WOW6432Node\Oracle"
        ];

        foreach (var keyPath in registryKeys)
        {
            using var oracleKey = Registry.LocalMachine.OpenSubKey(keyPath);
            if (oracleKey == null) continue;

            foreach (var subKeyName in oracleKey.GetSubKeyNames())
            {
                using var subKey = oracleKey.OpenSubKey(subKeyName);
                var home = subKey?.GetValue("ORACLE_HOME") as string;
                if (string.IsNullOrEmpty(home)) continue;

                var networkAdmin = Path.Combine(home, "network", "admin");
                if (File.Exists(Path.Combine(networkAdmin, "tnsnames.ora")))
                    return networkAdmin;
            }
        }

        return null;
    }

    /// <summary>
    /// Transforms legacy OLEDB connection strings to ADO.NET format.
    /// Strips 'Provider=xxx;' and adds User ID / Password if needed.
    ///
    /// SQL Server: Microsoft.Data.SqlClient v5+ defaults to Encrypt=Mandatory.
    /// Legacy internal servers typically don't have trusted TLS certificates,
    /// so we add Encrypt=false unless the connection string already specifies it.
    /// This matches the original OLEDB behavior (no encryption by default).
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

        // Remove Persist Security Info — it's a SqlClient property, not valid for Oracle
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned,
            @"Persist\s+Security\s+Info\s*=\s*[^;]+;?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Strip surrounding single quotes from values (e.g., 'sqloledb' -> sqloledb)
        cleaned = cleaned.Replace("'", "");

        // Ensure trailing semicolon
        cleaned = cleaned.TrimEnd();
        if (!cleaned.EndsWith(';'))
            cleaned += ";";

        // Add credentials only if not already present in the connection string
        var cleanedUpper = cleaned.ToUpperInvariant();
        if (!cleanedUpper.Contains("USER ID=") && !cleanedUpper.Contains("UID="))
            cleaned += $"User ID={uid};";
        if (!cleanedUpper.Contains("PASSWORD=") && !cleanedUpper.Contains("PWD="))
            cleaned += $"Password={pwd};";

        return cleaned;
    }

    /// <summary>
    /// Builds the final connection string with provider-specific defaults.
    /// SQL Server: adds Encrypt=false to match legacy OLEDB behavior (no TLS).
    /// </summary>
    private string BuildConnectionString()
    {
        var connString = TransformConnectionString(_config.ConnString, _config.Uid, _config.Pwd);

        if (!IsOracle)
        {
            // Microsoft.Data.SqlClient v5+ defaults to Encrypt=Mandatory.
            // Legacy internal SQL Servers don't have trusted certificates,
            // so we match the original OLEDB behavior (unencrypted) unless
            // the connection string already specifies encryption settings.
            var upper = connString.ToUpperInvariant();
            if (!upper.Contains("ENCRYPT=") && !upper.Contains("TRUSTSERVERCERTIFICATE="))
            {
                connString += "Encrypt=false;";
                _log.Debug("Added Encrypt=false for SQL Server connection {ConnectionId} " +
                    "(matching legacy OLEDB behavior)", Id);
            }
        }

        return connString;
    }

    private static bool DetectOracle(string connString, string connectionId)
    {
        var upper = connString.ToUpperInvariant();
        var upperId = connectionId.ToUpperInvariant();
        return upper.Contains("ORAOLEDB") || upper.Contains("ORACLE") || upperId.Contains("ORACLE");
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
