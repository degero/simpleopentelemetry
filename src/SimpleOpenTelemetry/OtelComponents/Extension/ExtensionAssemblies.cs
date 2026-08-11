using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Extension;

internal static class ExtensionAssemblies
{
    public static readonly Dictionary<TraceExtensionsEnum, AssemblyDescriptor>
        KnownTraceExtensions = new()
        {
            [TraceExtensionsEnum.AWSXRayTraceId] = new(
                "OpenTelemetry.Extensions.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                ["AddXRayTraceId"])
        };

    public static readonly Dictionary<MetricExtensionsEnum, AssemblyDescriptor>
        KnownMetricExtensions = new();

    public static readonly Dictionary<LogExtensionsEnum, AssemblyDescriptor>
        KnownLogExtensions = new();

    public static readonly Dictionary<BuilderExtensionsEnum, AssemblyDescriptor>
        KnownBuilderExtensions = new()
        {
            [BuilderExtensionsEnum.AzureMonitorExporter] = new(
                "Azure.Monitor.OpenTelemetry.Exporter",
                "Azure.Monitor.OpenTelemetry.Exporter.OpenTelemetryBuilderExtensions",
                ["UseAzureMonitorExporter"],
                "AzureMonitorExporterOptions",
                false)
        };
}
