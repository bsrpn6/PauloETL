namespace PauloETL.Models;

/// <summary>
/// Represents a &lt;connection&gt; element from the ETL configuration XML.
/// </summary>
public sealed class ConnectionConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ConnString { get; init; }
    public required string Uid { get; init; }
    public required string Pwd { get; init; }
}
