using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.OtelComponents.Exporter;

internal interface IExporterLoader
{
    void ConfigureExporters(MeterProviderBuilder builder, SimpleOpenTelemetryOptions config);
    void ConfigureExporters(TracerProviderBuilder builder, SimpleOpenTelemetryOptions config);
    void ConfigureExporters(LoggerProviderBuilder builder, SimpleOpenTelemetryOptions config);
}
