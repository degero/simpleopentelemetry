using SimpleOpenTelemetry.Exporter;

namespace SimpleOpenTelemetry.Exporter;

/// <summary>
/// Describes an exporter extension method found in an external assembly.
/// </summary>
/// <remarks>
/// Used by the reflection-based exporter loader to discover and invoke exporter registration methods.
/// </remarks>
/// <param name="AssemblyName">The name of the assembly containing the exporter (without .dll extension).</param>
/// <param name="TypeName">The full type name of the extension class (e.g., "OpenTelemetry.Trace.ConsoleExporterHelperExtensions").</param>
/// <param name="MethodName">The name of the public static extension method (e.g., "AddConsoleExporter").</param>
/// <param name="OptionsClassName">The fully qualified options class name if the method has an Action&lt;TOptions&gt; overload, otherwise null.</param>
public record ExporterExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string OptionsClassName
);

/// <summary>
/// Defines supported trace exporters available in SimpleOpenTelemetry.
/// </summary>
public enum TraceExporterEnum
{
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for trace export.</summary>
    Otlp,

    /// <summary>Exports traces to console output (for debugging).</summary>
    Console,

    /// <summary>Exports traces to Azure Monitor Application Insights.</summary>
    Azure
}

/// <summary>
/// Defines supported metrics exporters available in SimpleOpenTelemetry.
/// </summary>
public enum MetricExporterEnum
{
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for metrics export.</summary>
    Otlp,

    /// <summary>Exports metrics to console output (for debugging).</summary>
    Console,

    /// <summary>Prometheus metrics exporter with HTTP listener.</summary>
    PrometheusHttpListener,

    /// <summary>Prometheus metrics exporter integrated with ASP.NET Core.</summary>
    PrometheusAspNetCore,

    /// <summary>Exports metrics to Azure Monitor Application Insights.</summary>
    Azure
}

/// <summary>
/// Defines supported log exporters available in SimpleOpenTelemetry.
/// </summary>
public enum LogExporterEnum
{
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for log export.</summary>
    Otlp,

    /// <summary>Exports logs to console output (for debugging).</summary>
    Console,

    /// <summary>Exports logs to Azure Monitor Application Insights.</summary>
    Azure
}

/// <summary>
/// Provides registry of known exporters and their configurations for reflection-based loading.
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

