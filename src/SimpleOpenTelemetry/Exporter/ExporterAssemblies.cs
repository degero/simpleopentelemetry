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
    Console,
    Azure
}

public enum MetricExporterEnum
{
    Otlp,
    Console,
    Azure
}

public enum LogExporterEnum
{
    Otlp,
    Console,
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
            [TraceExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Trace.ConsoleExporterHelperExtensions",
                "AddConsoleExporter",
                "ConsoleExporterOptions"),
            
            [TraceExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorTraceExporter",
                "AzureExporterOptions"),
        };


    public static readonly Dictionary<MetricExporterEnum, ExporterExtensionDescriptor>
        KnownMetricsExporters = new()
        {
            [MetricExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Metrics.ConsoleExporterMetricsExtensions",
                "AddConsoleExporter",
                null),

            [MetricExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorMetricExporter",
                "AzureExporterOptions"),
        };

    public static readonly Dictionary<LogExporterEnum, ExporterExtensionDescriptor>
        KnownLogExporters = new()
        {
            [LogExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Logs.ConsoleExporterLoggingExtensions",
                "AddConsoleExporter",
                null), 

            [LogExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorLogExporter",
                "AzureExporterOptions"),
        };
}

