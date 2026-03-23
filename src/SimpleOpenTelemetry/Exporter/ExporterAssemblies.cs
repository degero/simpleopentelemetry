using SimpleOpenTelemetry.Exporter;

namespace SimpleOpenTelemetry.Exporter;

public record ExporterExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string OptionsClassName
);

public enum TraceExporterEnum
{
    Otlp,
    Azure
}

public enum MetricExporterEnum
{
    Otlp,
    Azure
}

public enum LogExporterEnum
{
    Otlp,
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
            [TraceExporterEnum.Otlp] = new(
                "OpenTelemetry",
                "OpenTelemetry.Trace.OtlpMetricExporterExtensions",
                "AddOltExporter",
                "OtlpExporterOptions"),

            [TraceExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorTraceExporter",
                "AzureExporterOptions"),
        };


    public static readonly Dictionary<MetricExporterEnum, ExporterExtensionDescriptor>
        KnownMetricsExporters = new()
        {
            [MetricExporterEnum.Otlp] = new(
                "OpenTelemetry",
                "OpenTelemetry.Metrics.OtlpMetricExporterExtensions",
                "AddOltExporter",
                "OtlpExporterOptions"),

            [MetricExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorMetricExporter",
                "AzureExporterOptions"),
        };

    public static readonly Dictionary<LogExporterEnum, ExporterExtensionDescriptor>
        KnownLogExporters = new()
        {
            [LogExporterEnum.Otlp] = new(
                "OpenTelemetry",
                "OpenTelemetry.Logs.OtlpMetricExporterExtensions",
                "AddOltExporter",
                "OtlpExporterOptions"),

            [LogExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorLogExporter",
                "AzureExporterOptions"),
        };
}

