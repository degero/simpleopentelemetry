namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Configuration;

/// <summary>
/// Interface for the fluent OpenTelemetry configuration builder
/// </summary>
public interface ISimpleOpenTelemetryBuilder
{
    ISimpleOpenTelemetryBuilder ConfigureExporterFromOptions(
         IConfiguration configuration);
}
