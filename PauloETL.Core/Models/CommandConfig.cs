namespace PauloETL.Models;

/// <summary>
/// Represents a &lt;command&gt; element from the ETL configuration XML.
/// Commands contain SQL text, optional parameters, and optional foreach children.
/// </summary>
public sealed class CommandConfig
{
    public required string Name { get; init; }
    public required string ConnId { get; init; }
    public required bool Rowset { get; init; }
    public required bool BeginTran { get; init; }
    public required bool Enabled { get; init; }
    public required string SqlText { get; init; }
    public IReadOnlyList<ParamConfig> Parameters { get; init; } = [];
    public IReadOnlyList<CommandConfig> ForEachChildren { get; init; } = [];
}
