using OpenTelemetry;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.OtelComponents.Distro;

internal interface IDistroLoader
{
    bool LoadDistro(IOpenTelemetryBuilder builder, SimpleOpenTelemetryOptions options);
}