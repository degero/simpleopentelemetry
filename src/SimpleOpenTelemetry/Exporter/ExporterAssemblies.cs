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
    PrometheusHttpListener,
    PrometheusAspNetCore,
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
            /* Otel SDK exporters */
            [TraceExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Trace.ConsoleExporterHelperExtensions",
                "AddConsoleExporter",
                null),
            
            /* Vendor exporters */
            [TraceExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorTraceExporter",
                "AzureExporterOptions"),
        };


    public static readonly Dictionary<MetricExporterEnum, ExporterExtensionDescriptor>
        KnownMetricsExporters = new()
        {
            /* Otel SDK exporters */
            [MetricExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Metrics.ConsoleExporterMetricsExtensions",
                "AddConsoleExporter",
                null),

            [MetricExporterEnum.PrometheusHttpListener] = new(
                "OpenTelemetry.Exporter.Prometheus.HttpListener",
                "OpenTelemetry.Metrics.PrometheusHttpListenerMeterProviderBuilderExtensions",
                "AddPrometheusHttpListener",
                "PrometheusHttpListenerOptions"),

            [MetricExporterEnum.PrometheusAspNetCore] = new(
                "OpenTelemetry.Exporter.Prometheus.AspNetCore",
                "OpenTelemetry.Metrics.PrometheusExporterMeterProviderBuilderExtensions",
                "AddPrometheusExporter",
                "PrometheusAspNetCoreOptions"),

            /* Vendor exporters */
            [MetricExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorMetricExporter",
                "AzureExporterOptions"),

        };

    public static readonly Dictionary<LogExporterEnum, ExporterExtensionDescriptor>
        KnownLogExporters = new()
        {
            /* Otel SDK exporters */
            [LogExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Logs.ConsoleExporterLoggingExtensions",
                "AddConsoleExporter",
                null), 

            /* Vendor exporters */
            [LogExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorLogExporter",
                "AzureExporterOptions"),
        };
}

