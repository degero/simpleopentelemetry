namespace SimpleOpenTelemetry.OtelComponents.Exporter;

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
/// <param name="optionsRequired">Indicates if options required.</param>
internal record ExporterExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? OptionsClassName,
     bool optionsRequired = false
);

/// <summary>
/// Defines supported trace exporters available in SimpleOpenTelemetry.
/// </summary>
public enum TraceExporterEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for trace export.</summary>
    Otlp,

    /// <summary>Exports traces to console output (for debugging).</summary>
    Console,

    /// <summary>Exports traces to Azure Monitor Application Insights.</summary>
    /* vendor libraries */
    Azure
}

/// <summary>
/// Defines supported metrics exporters available in SimpleOpenTelemetry.
/// </summary>
public enum MetricExporterEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for metrics export.</summary>
    Otlp,

    /// <summary>Exports metrics to console output (for debugging).</summary>
    Console,

    /// <summary>Prometheus metrics exporter with HTTP listener.</summary>
    PrometheusHttpListener,

    /// <summary>Prometheus metrics exporter integrated with ASP.NET Core.</summary>
    PrometheusAspNetCore,

    /* vendor libraries */
    /// <summary>Exports metrics to Azure Monitor Application Insights.</summary>
    Azure
}

/// <summary>
/// Defines supported log exporters available in SimpleOpenTelemetry.
/// </summary>
public enum LogExporterEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>Uses OTLP (OpenTelemetry Protocol) for log export.</summary>
    Otlp,

    /// <summary>Exports logs to console output (for debugging).</summary>
    Console,

    /// <summary>Exports logs to Azure Monitor Application Insights.</summary>
    /* vendor libraries */
    Azure
}

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

// TODO rename to Metric
    public static readonly Dictionary<MetricExporterEnum, ExporterExtensionDescriptor>
        KnownMetricsExporters = new()
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

