using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.OtelComponents.Resource;

internal interface IResourceDetectorLoader
{
    void AddResourceDetectors(ResourceBuilder builder, SimpleOpenTelemetryOptions options);
}