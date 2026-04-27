using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

internal interface IInstrumentationLoader
{
    void AddMetricsInstrumentation(MeterProviderBuilder builder, MetricInstrumentationEnum instrumentation);
    void AddTracingInstrumentation(TracerProviderBuilder builder, TraceInstrumentationEnum instrumentation);
}