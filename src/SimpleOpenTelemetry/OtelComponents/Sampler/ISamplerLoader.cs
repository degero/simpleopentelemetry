using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.OtelComponents.Sampler;

internal interface ISamplerLoader
{
    void SetSampler(TracerProviderBuilder builder, SimpleOpenTelemetryOptions options);
}