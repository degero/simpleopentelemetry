using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Instrumentation;

internal record ExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? ConfigurationSection
);

/// <summary>
/// 
/// </summary>
internal static class ExtensionAssemblies
{
    public static readonly Dictionary<TraceExtensionsEnum, ExtensionDescriptor>
        KnownTraceExtensions = new()
        {
            [TraceExtensionsEnum.AWSXRayTraceId] = new(
                "OpenTelemetry.Extensions.AWS",
                "OpenTelemetry.Extensions.AWS.Trace",
                "AddAWSXRayTraceId",
                null),
        };


    public static readonly Dictionary<MetricExtensionsEnum, ExtensionDescriptor>
        KnownMetricExtensions = new()
        {
        };

    public static readonly Dictionary<LogExtensionsEnum, ExtensionDescriptor>
        KnownLogExtensions = new()
        {
        };
}
