namespace PauloETL.Models;

/// <summary>
/// Represents a &lt;step&gt; element from the ETL configuration XML.
/// Each step contains exactly one top-level command.
/// </summary>
public sealed class StepConfig
{
    public required string Name { get; init; }
    public required CommandConfig Command { get; init; }
}
