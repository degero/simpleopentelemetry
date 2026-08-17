using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Exporter;

/// <summary>
/// Provides registry of known exporters and their configurations for reflection-based loading.
/// </summary>
internal static class ExporterAssemblies
{
    public static readonly Dictionary<TraceExporterEnum, AssemblyDescriptor>
        KnownTraceExporters = new()
        {
            /* opentelemetry-dotnet-contrib */
            [TraceExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Trace.ConsoleExporterHelperExtensions",
                ["AddConsoleExporter"]),

            /* Vendor exporters */
            [TraceExporterEnum.AzureMonitor] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                ["AddAzureMonitorTraceExporter"],
                "AzureMonitorExporterOptions"),
        };

    public static readonly Dictionary<MetricExporterEnum, AssemblyDescriptor>
        KnownMetricExporters = new()
        {
            /* opentelemetry-dotnet-contrib */
            [MetricExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Metrics.ConsoleExporterMetricsExtensions",
                ["AddConsoleExporter"]),

            [MetricExporterEnum.PrometheusHttpListener] = new(
                "OpenTelemetry.Exporter.Prometheus.HttpListener",
                "OpenTelemetry.Metrics.PrometheusHttpListenerMeterProviderBuilderExtensions",
                ["AddPrometheusHttpListener"],
                "PrometheusHttpListenerOptions"),

            [MetricExporterEnum.PrometheusAspNetCore] = new(
                "OpenTelemetry.Exporter.Prometheus.AspNetCore",
                "OpenTelemetry.Metrics.PrometheusExporterMeterProviderBuilderExtensions",
                ["AddPrometheusExporter"],
                "PrometheusAspNetCoreOptions"),

            /* Vendor libraries */
            [MetricExporterEnum.AzureMonitor] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                ["AddAzureMonitorMetricExporter"],
                "AzureMonitorExporterOptions"),

        };

    public static readonly Dictionary<LogExporterEnum, AssemblyDescriptor>
        KnownLogExporters = new()
        {
            /* Otel SDK exporters */
            [LogExporterEnum.Console] = new(
                "OpenTelemetry.Exporter.Console",
                "OpenTelemetry.Logs.ConsoleExporterLoggingExtensions",
                ["AddConsoleExporter"]),

            /* Vendor libraries */
            [LogExporterEnum.AzureMonitor] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorExporterExtensions",
                ["AddAzureMonitorLogExporter"],
                "AzureMonitorExporterOptions"),
        };
}

