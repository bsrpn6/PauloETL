using System.Data;

namespace PauloETL.Configuration;

/// <summary>
/// Maps ADO type/direction strings from the XML configuration to .NET DbType and ParameterDirection values.
/// Mirrors the VB6 GetADODataType() and GetADOParamDir() functions from Globals.bas.
/// </summary>
public static class AdoTypeMapper
{
    public static DbType MapDataType(string adoType) => adoType.ToLowerInvariant() switch
    {
        "adchar" => DbType.AnsiStringFixedLength,
        "adboolean" => DbType.Boolean,
        "addate" => DbType.DateTime,
        "adinteger" => DbType.Int32,
        "advarchar" => DbType.AnsiString,
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
