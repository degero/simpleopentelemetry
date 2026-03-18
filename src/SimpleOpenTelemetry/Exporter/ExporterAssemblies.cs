using SimpleOpenTelemetry.Exporter;

namespace SimpleOpenTelemetry.Exporter;

public record ExporterExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? ConfigurationSection
);

public enum TraceExporterEnum
{
    Azure
}

public enum MetricExporterEnum
{
    Azure
}

public enum LogExporterEnum
{
    Azure
}

/// <summary>
///
/// </summary>
public static class ExporterAssemblies
{
    public static readonly Dictionary<TraceExporterEnum, ExporterExtensionDescriptor>
        KnownTraceExporters = new()
        {
            [TraceExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorTraceExporter",
                null),
        };


    public static readonly Dictionary<MetricExporterEnum, ExporterExtensionDescriptor>
        KnownMetricsExporters = new()
        {
            [MetricExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorMetricExporter",
                null),
        };

    public static readonly Dictionary<LogExporterEnum, ExporterExtensionDescriptor>
        KnownLogExporters = new()
        {
            [LogExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorLogExporter",
                null),
        };
}

