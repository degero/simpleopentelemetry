using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

internal interface IInstrumentationLoader
{
    void AddMetricsInstrumentations(MeterProviderBuilder builder, SimpleOpenTelemetryOptions options);
    void AddTracingInstrumentations(TracerProviderBuilder builder, SimpleOpenTelemetryOptions options);
}