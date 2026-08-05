using OpenTelemetry.Resources;

namespace SimpleOpenTelemetry.OtelComponents.Resource;

internal interface IResourceDetectorLoader
{
    void AddResourceDetectors(ResourceBuilder builder, SimpleOpenTelemetryOptions options);
}