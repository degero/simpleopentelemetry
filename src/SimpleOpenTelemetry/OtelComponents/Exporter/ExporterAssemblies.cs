namespace SimpleOpenTelemetry.OtelComponents.Exporter;

internal record ExporterExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? OptionsClassName,
     bool optionsRequired = false
);

/// <summary>
/// Provides registry of known exporters and their configurations for reflection-based loading.
/// </summary>
internal static class ExporterAssemblies
{
    public static readonly Dictionary<TraceExporterEnum, ExporterExtensionDescriptor>
        KnownTraceExporters = new()
        {
            /* opentelemetry-dotnet-contrib */
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
                "AzureMonitorExporterOptions",
                true),
        };

    public static readonly Dictionary<MetricExporterEnum, ExporterExtensionDescriptor>
        KnownMetricExporters = new()
        {
            /* opentelemetry-dotnet-contrib */
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

            /* Vendor libraries */
            [MetricExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorMetricExporter",
                "AzureMonitorExporterOptions",
                true),

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

            /* Vendor libraries */
            [LogExporterEnum.Azure] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                "AddAzureMonitorLogExporter",
                "AzureMonitorExporterOptions",
                true),
        };
}

