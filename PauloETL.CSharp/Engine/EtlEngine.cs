using System.Diagnostics;
using PauloETL.Configuration;
using PauloETL.Connections;
using PauloETL.Models;
using Serilog;

namespace PauloETL.Engine;

/// <summary>
/// Main ETL orchestrator — loads configuration, manages connections, executes jobs.
/// Mirrors the VB6 ETLControl class (LoadXMLConfig, ExecuteJob, ExecuteJobStep).
///
/// Execution is fully sequential (async I/O, but one step at a time) matching
/// the original VB6 behavior. See plan.md for the async feasibility analysis.
/// </summary>
public sealed class EtlEngine : IAsyncDisposable
{
    private readonly Dictionary<string, EtlConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<JobConfig> _jobs = [];
    private bool _loaded;

    public bool DryRun { get; set; }

    /// <summary>
    /// Loads the XML configuration file and initializes connection wrappers.
    /// Mirrors ETLControl.LoadXMLConfig().
    /// </summary>
    public void LoadConfig(string xmlFilePath)
    {
        var (connectionConfigs, jobs) = XmlConfigParser.Load(xmlFilePath);

        foreach (var connConfig in connectionConfigs)
        {
            _connections[connConfig.Id] = new EtlConnection(connConfig);
        }

        _jobs = jobs;
        _loaded = true;

        Log.Information("Configuration loaded: {ConnectionCount} connections, {JobCount} jobs",
            _connections.Count, _jobs.Count);
    }

    /// <summary>
    /// Executes a job by ID. Finds the job in config, then runs each step sequentially.
    /// Mirrors ETLControl.ExecuteJob().
    /// </summary>
    public async Task<bool> ExecuteJobAsync(string jobId, CancellationToken ct = default)
    {
        if (!_loaded)
            throw new InvalidOperationException("Must call LoadConfig before executing a job");

        var job = _jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Job '{jobId}' not found in configuration");

        var sw = Stopwatch.StartNew();

        if (DryRun)
            Log.Warning("*** DRY RUN MODE — child/mutation commands will be logged but not executed ***");

        Log.Information("Starting job: {JobId} ({JobName}) — {StepCount} steps",
            job.Id, job.Name, job.Steps.Count);

        for (int i = 0; i < job.Steps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var step = job.Steps[i];

            if (!await ExecuteStepAsync(job.Id, step, i + 1, job.Steps.Count, ct))
            {
                Log.Error("Job {JobId} failed at step {StepNum}/{StepCount}: {StepName}",
                    job.Id, i + 1, job.Steps.Count, step.Name);
                return false;
            }
        }

        sw.Stop();
        Log.Information("Job {JobId} completed successfully in {Elapsed:F1}s",
            job.Id, sw.Elapsed.TotalSeconds);
        return true;
    }

    /// <summary>
    /// Executes a single job step by building and running its command tree.
    /// Mirrors ETLControl.ExecuteJobStep().
    /// </summary>
    private async Task<bool> ExecuteStepAsync(
        string jobId, StepConfig step, int stepNum, int stepCount, CancellationToken ct)
    {
        Log.Information("Step {StepNum}/{StepCount}: {StepName}",
            stepNum, stepCount, step.Name);

        var location = $"Job: {jobId}\nStep: {step.Name}";

        try
        {
            var command = await EtlCommand.BuildAsync(
                step.Command, _connections, location, parent: null, ct);

            return await command.ExecuteAsync(DryRun, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Step '{StepName}' failed with exception\n{Location}",
                step.Name, location);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Log.Debug("Disposing ETL engine, closing all connections");
        foreach (var conn in _connections.Values)
        {
            await conn.DisposeAsync();
        }
        _connections.Clear();
    }
}
