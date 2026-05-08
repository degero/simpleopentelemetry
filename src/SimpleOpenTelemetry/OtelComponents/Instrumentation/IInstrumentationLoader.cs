using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

internal interface IInstrumentationLoader
{
    void AddMetricsInstrumentation(MeterProviderBuilder builder, SimpleOpenTelemetryOptions options, MetricInstrumentationEnum instrumentation);
    void AddTracingInstrumentation(TracerProviderBuilder builder, SimpleOpenTelemetryOptions options, TraceInstrumentationEnum instrumentation);
}