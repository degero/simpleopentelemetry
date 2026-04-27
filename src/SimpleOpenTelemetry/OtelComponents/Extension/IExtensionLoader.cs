using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Extension;

namespace SimpleOpenTelemetry.OtelComponents.Extensions;

internal interface IExtensionLoader
{
    void AddMetricsExtension(MeterProviderBuilder builder, MetricExtensionsEnum extension);
    void AddLogExtension(LoggerProviderBuilder builder, LogExtensionsEnum extension);
    void AddTraceExtension(TracerProviderBuilder builder, TraceExtensionsEnum extension);
}