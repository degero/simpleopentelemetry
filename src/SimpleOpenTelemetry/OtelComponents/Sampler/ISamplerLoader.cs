using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.OtelComponents.Sampler;

internal interface ISamplerLoader
{
    void SetSampler(TracerProviderBuilder builder, SimpleOpenTelemetryOptions options);
}