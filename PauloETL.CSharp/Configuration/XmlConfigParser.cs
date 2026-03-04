using System.Xml;
using System.Xml.Schema;
using PauloETL.Models;
using Serilog;

namespace PauloETL.Configuration;

/// <summary>
/// Parses PauloETL.xml configuration files with optional XSD validation.
/// Mirrors the XML loading logic from ETLControl.LoadXMLConfig, ETLConnections.LoadFromXML,
/// ETLCommand.LoadFromXML, and ETLCommands.LoadFromXML.
/// </summary>
public static class XmlConfigParser
{
    /// <summary>
    /// Loads and validates the ETL configuration XML, returning all connections and jobs.
    /// </summary>
    public static (IReadOnlyList<ConnectionConfig> Connections, IReadOnlyList<JobConfig> Jobs) Load(string xmlFilePath)
    {
        Log.Debug("Loading XML configuration from {XmlFile}", xmlFilePath);

        if (!File.Exists(xmlFilePath))
            throw new FileNotFoundException($"XML configuration file not found: {xmlFilePath}");

        var doc = new XmlDocument();
        doc.Load(xmlFilePath);

        // Attempt XSD validation if schema file is co-located
        ValidateSchema(doc, xmlFilePath);

        var root = doc.DocumentElement
            ?? throw new InvalidOperationException("XML document has no root element");

        var connections = ParseConnections(root);
        var jobs = ParseJobs(root);

        Log.Information("Loaded {ConnectionCount} connections and {JobCount} jobs from XML",
            connections.Count, jobs.Count);

        return (connections, jobs);
    }

    private static void ValidateSchema(XmlDocument doc, string xmlFilePath)
    {
        var xmlDir = Path.GetDirectoryName(xmlFilePath) ?? ".";
        var xsdPath = Path.Combine(xmlDir, "ETLSchema.xsd");

        if (!File.Exists(xsdPath))
        {
            Log.Warning("XSD schema file not found at {XsdPath}, skipping validation", xsdPath);
            return;
        }

        try
        {
            var schemas = new XmlSchemaSet();
            schemas.Add("", xsdPath);
            doc.Schemas = schemas;

            var validationErrors = new List<string>();
            doc.Validate((_, e) => validationErrors.Add(e.Message));

            if (validationErrors.Count > 0)
            {
                foreach (var error in validationErrors)
                    Log.Warning("XML validation warning: {ValidationError}", error);
            }
            else
            {
                Log.Debug("XML validated successfully against schema");
            }
        }
        catch (XmlSchemaException ex)
        {
            Log.Warning(ex, "Failed to load XSD schema, skipping validation");
        }
    }

    private static IReadOnlyList<ConnectionConfig> ParseConnections(XmlElement root)
    {
        var nodes = root.SelectNodes("connections/connection");
        if (nodes == null || nodes.Count < 2)
            throw new InvalidOperationException("Configuration must have at least 2 connections defined");

        var connections = new List<ConnectionConfig>(nodes.Count);
        foreach (XmlElement node in nodes)
        {
            connections.Add(new ConnectionConfig
            {
                Id = GetRequiredAttribute(node, "id"),
                Name = GetRequiredAttribute(node, "name"),
                ConnString = GetRequiredAttribute(node, "connstring"),
                Uid = GetRequiredAttribute(node, "uid"),
                Pwd = GetRequiredAttribute(node, "pwd")
            });
            Log.Debug("Parsed connection: {ConnectionId} ({ConnectionName})",
                connections[^1].Id, connections[^1].Name);
        }
        return connections;
    }

    private static IReadOnlyList<JobConfig> ParseJobs(XmlElement root)
    {
        var jobNodes = root.SelectNodes("jobs/job");
        if (jobNodes == null || jobNodes.Count == 0)
            throw new InvalidOperationException("Configuration must have at least 1 job defined");

        var jobs = new List<JobConfig>(jobNodes.Count);
        foreach (XmlElement jobNode in jobNodes)
        {
            var steps = ParseSteps(jobNode);
            jobs.Add(new JobConfig
            {
                Id = GetRequiredAttribute(jobNode, "id"),
                Name = GetRequiredAttribute(jobNode, "name"),
                Steps = steps
            });
        }
        return jobs;
    }

    private static IReadOnlyList<StepConfig> ParseSteps(XmlElement jobNode)
    {
        var stepNodes = jobNode.SelectNodes("step");
        if (stepNodes == null || stepNodes.Count == 0)
            throw new InvalidOperationException(
                $"Job '{jobNode.GetAttribute("id")}' must have at least 1 step defined");

        var steps = new List<StepConfig>(stepNodes.Count);
        foreach (XmlElement stepNode in stepNodes)
        {
            var commandNode = stepNode.SelectSingleNode("command") as XmlElement
                ?? throw new InvalidOperationException(
                    $"Step '{stepNode.GetAttribute("name")}' has no command element");

            steps.Add(new StepConfig
            {
                Name = GetRequiredAttribute(stepNode, "name"),
                Command = ParseCommand(commandNode)
            });
        }
        return steps;
    }

    private static CommandConfig ParseCommand(XmlElement commandNode)
    {
        var parameters = ParseParameters(commandNode);
        var forEachChildren = ParseForEachChildren(commandNode);
        var sqlText = GetCDataText(commandNode);

        if (string.IsNullOrWhiteSpace(sqlText))
            throw new InvalidOperationException(
                $"Command '{commandNode.GetAttribute("name")}' has no SQL statement in CDATA section");

        return new CommandConfig
        {
            Name = GetRequiredAttribute(commandNode, "name"),
            ConnId = GetRequiredAttribute(commandNode, "connid"),
            Rowset = bool.Parse(GetRequiredAttribute(commandNode, "rowset")),
            BeginTran = bool.Parse(GetRequiredAttribute(commandNode, "begintran")),
            Enabled = bool.Parse(GetRequiredAttribute(commandNode, "enabled")),
            SqlText = sqlText.Trim(),
            Parameters = parameters,
            ForEachChildren = forEachChildren
        };
    }

    private static IReadOnlyList<ParamConfig> ParseParameters(XmlElement commandNode)
    {
        var paramNodes = commandNode.SelectNodes("params/param");
        if (paramNodes == null || paramNodes.Count == 0)
            return [];

        var parameters = new List<ParamConfig>(paramNodes.Count);
        foreach (XmlElement paramNode in paramNodes)
        {
            var sizeAttr = paramNode.GetAttribute("size");
            parameters.Add(new ParamConfig
            {
                Name = GetRequiredAttribute(paramNode, "name"),
                Source = GetRequiredAttribute(paramNode, "source"),
                Type = GetRequiredAttribute(paramNode, "type"),
                Direction = GetRequiredAttribute(paramNode, "direction"),
                Size = string.IsNullOrEmpty(sizeAttr) ? null : int.Parse(sizeAttr)
            });
        }
        return parameters;
    }

    private static IReadOnlyList<CommandConfig> ParseForEachChildren(XmlElement commandNode)
    {
        var forEachNode = commandNode.SelectSingleNode("foreach") as XmlElement;
        if (forEachNode == null)
            return [];

        var childNodes = forEachNode.SelectNodes("command");
        if (childNodes == null || childNodes.Count == 0)
            return [];

        var children = new List<CommandConfig>(childNodes.Count);
        foreach (XmlElement childNode in childNodes)
        {
            children.Add(ParseCommand(childNode));
        }
        return children;
    }

    /// <summary>
    /// Extracts CDATA text content from an XML element.
    /// Mirrors the VB6 GetXmlCData() function from Globals.bas.
    /// </summary>
    private static string GetCDataText(XmlElement element)
    {
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlCDataSection cdata)
                return cdata.Value ?? "";
        }
        return "";
    }

    private static string GetRequiredAttribute(XmlElement element, string name)
    {
        var value = element.GetAttribute(name);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException(
                $"Required attribute '{name}' missing on <{element.LocalName}> element");
        return value;
    }
}
