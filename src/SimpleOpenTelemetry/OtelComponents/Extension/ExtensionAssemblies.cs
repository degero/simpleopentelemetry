namespace SimpleOpenTelemetry.OtelComponents.Extension;

internal record ExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName
);

internal static class ExtensionAssemblies
{
    public static readonly Dictionary<TraceExtensionsEnum, ExtensionDescriptor>
        KnownTraceExtensions = new()
        {
            [TraceExtensionsEnum.AWSXRayTraceId] = new(
                "OpenTelemetry.Extensions.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddXRayTraceId")
        };


    public static readonly Dictionary<MetricExtensionsEnum, ExtensionDescriptor>
        KnownMetricExtensions = new();

    public static readonly Dictionary<LogExtensionsEnum, ExtensionDescriptor>
        KnownLogExtensions = new();
}
