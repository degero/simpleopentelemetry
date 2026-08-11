using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.OtelComponents.Extensions;

internal interface IExtensionLoader
{
    void AddMetricExtensions(MeterProviderBuilder builder, SimpleOpenTelemetryMetricOptions options);
    void AddLogExtensions(LoggerProviderBuilder builder, SimpleOpenTelemetryLogOptions options);
    void AddTraceExtensions(TracerProviderBuilder builder, SimpleOpenTelemetryTraceOptions options);
    void AddBuilderExtensions(IOpenTelemetryBuilder builder, SimpleOpenTelemetryOptions options);
}
