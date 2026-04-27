using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.OtelComponents.Sampler;

internal interface ISamplerLoader
{
    void AddSampler(TracerProviderBuilder builder, OpenTelemetry.Resources.Resource resource, SimpleOpenTelemetryOptions options);
}