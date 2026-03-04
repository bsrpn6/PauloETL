namespace PauloETL.Models;

/// <summary>
/// Represents a &lt;param&gt; element from the ETL configuration XML.
/// </summary>
public sealed class ParamConfig
{
    public required string Name { get; init; }
    public required string Source { get; init; }
    public required string Type { get; init; }
    public required string Direction { get; init; }
    public int? Size { get; init; }
}
