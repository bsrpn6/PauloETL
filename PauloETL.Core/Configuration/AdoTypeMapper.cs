using System.Data;

namespace PauloETL.Configuration;

/// <summary>
/// Maps ADO type/direction strings from the XML configuration to .NET DbType and ParameterDirection values.
/// Matches the VB.NET production GetADODataType() and GetADOParamDir() functions from Globals.vb.
///
/// NOTE: The VB.NET production code maps both "adchar" and "advarchar" to DbType.String.
/// This differs from the original VB6 which used distinct ADO types (adChar vs adVarChar).
/// We match the VB.NET production behavior here.
/// </summary>
public static class AdoTypeMapper
{
    /// <summary>
    /// Indicates that this parameter is an Oracle RefCursor (not a standard DbType).
    /// Callers must check for this sentinel and use OracleDbType.RefCursor instead.
    /// </summary>
    public const string OracleRefCursorSentinel = "adcursor";

    public static bool IsRefCursor(string adoType) =>
        adoType.Equals(OracleRefCursorSentinel, StringComparison.OrdinalIgnoreCase);

    public static DbType MapDataType(string adoType) => adoType.ToLowerInvariant() switch
    {
        "adchar" or "advarchar" => DbType.String,
        "adboolean" => DbType.Boolean,
        "addate" => DbType.DateTime,
        "adinteger" => DbType.Int32,
        "adbigint" => DbType.Int64,
        "adsingle" => DbType.Single,
        "addouble" => DbType.Double,
        _ => throw new ArgumentException($"Unknown ADO data type: '{adoType}'")
    };

    public static ParameterDirection MapDirection(string adoDirection) => adoDirection.ToLowerInvariant() switch
    {
        "adparaminput" => ParameterDirection.Input,
        "adparaminputoutput" => ParameterDirection.InputOutput,
        "adparamoutput" => ParameterDirection.Output,
        "adparamreturnvalue" => ParameterDirection.ReturnValue,
        _ => throw new ArgumentException($"Unknown ADO parameter direction: '{adoDirection}'")
    };
}
