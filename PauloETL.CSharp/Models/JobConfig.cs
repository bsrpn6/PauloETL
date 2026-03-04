namespace PauloETL.Models;

/// <summary>
/// Represents a &lt;job&gt; element from the ETL configuration XML.
/// </summary>
public sealed class JobConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<StepConfig> Steps { get; init; } = [];
}
