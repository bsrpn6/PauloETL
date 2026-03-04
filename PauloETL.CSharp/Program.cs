using PauloETL.Engine;
using Serilog;
using Serilog.Events;

namespace PauloETL;

/// <summary>
/// Entry point for PauloETLExecute — C# replacement for the VB6 PauloETLExecute.exe.
///
/// Usage:
///   PauloETLExecute -Job:Main -XML:C:\path\to\PauloETL.xml [--dry-run] [--log-dir:C:\logs] [--verbose]
///
/// Arguments (mirrors the original VB6 command-line interface):
///   -Job:&lt;JobID&gt;       Required. The job ID to execute (e.g., "Main").
///   -XML:&lt;FilePath&gt;    Required. Path to the ETL configuration XML file.
///   --dry-run           Optional. Logs all commands with parameters but skips mutation (child) commands.
///   --log-dir:&lt;Path&gt;   Optional. Directory for log files. Defaults to current directory.
///   --verbose           Optional. Enables debug-level logging.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var commandLine = string.Join(" ", args);

        var jobId = GetArgument(commandLine, "-Job:");
        var xmlFile = GetArgument(commandLine, "-XML:");
        var logDir = GetArgument(commandLine, "--log-dir:") ?? Directory.GetCurrentDirectory();
        var dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
        var verbose = args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase));

        ConfigureLogging(logDir, dryRun, verbose);

        try
        {
            if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(xmlFile))
            {
                Log.Fatal("Must pass -Job: and -XML: parameters.\n" +
                    "Usage: PauloETLExecute -Job:Main -XML:C:\\path\\to\\PauloETL.xml [--dry-run] [--log-dir:C:\\logs] [--verbose]");
                return 1;
            }

            Log.Information("PauloETL starting — Job: {JobId}, XML: {XmlFile}, DryRun: {DryRun}",
                jobId, xmlFile, dryRun);

            await using var engine = new EtlEngine { DryRun = dryRun };
            engine.LoadConfig(xmlFile);

            var success = await engine.ExecuteJobAsync(jobId);

            if (success)
            {
                Log.Information("PauloETL completed successfully");
                return 0;
            }
            else
            {
                Log.Error("PauloETL completed with errors");
                return 2;
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("PauloETL was cancelled");
            return 3;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "PauloETL terminated with unhandled exception");
            return 99;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Configures Serilog with console and rolling file sinks.
    /// Dry-run mode gets a separate log file name to keep it distinct.
    /// </summary>
    private static void ConfigureLogging(string logDir, bool dryRun, bool verbose)
    {
        var minimumLevel = verbose ? LogEventLevel.Debug : LogEventLevel.Information;

        var filePrefix = dryRun ? "PauloETL_DryRun" : "PauloETL";
        var logFilePath = Path.Combine(logDir, $"{filePrefix}_.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Extracts a named argument value from the command line string.
    /// Mirrors the VB6 GetArgument() subroutine from MainModule.bas.
    ///
    /// Given command "-Job:Main -XML:foo.xml" and argName "-Job:",
    /// returns "Main".
    /// </summary>
    private static string? GetArgument(string commandLine, string argName)
    {
        var startPos = commandLine.IndexOf(argName, StringComparison.OrdinalIgnoreCase);
        if (startPos < 0)
            return null;

        startPos += argName.Length;
        var endPos = commandLine.IndexOf(' ', startPos);
        if (endPos < 0)
            endPos = commandLine.Length;

        var value = commandLine[startPos..endPos];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
