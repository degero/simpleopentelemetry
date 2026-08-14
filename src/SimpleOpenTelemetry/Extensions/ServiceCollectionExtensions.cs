namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Internal;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to initialise OpenTelemetry and process custom
    /// env var / json config (in section 'SimpleOpenTelemetry') into OpenTelemetry instrumentation
    /// logging and exporting etc setups.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns>OpenTelemetryBuilder for any additional code based configuration</returns>
    internal static OpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Guard.ThrowIfNull(services);
        Guard.ThrowIfNull(configuration);

        // Structured this way for more testability on injecting otelBuilder to SimpleOpenTelemetryBuilder
        // Always return an OpenTelemetrySdk regardless
        // to not disrupt any custom code dependencies in app code
        var otelBuilder = services.AddOpenTelemetry();

        // Only run SimpleOpenTelemetryBuilder if basic config fields set. Error Events logged if not set
        if (SimpleOpenTelemetryBuilder.ValidateConfigurationFormat(configuration))
        {
            // Initialize a builder and configure as a Generic host app would
            var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, configuration);
            builder.Configure();// Reads configuration and runs OpenTelemetry Fluent API methods
        }

        return otelBuilder;
    }
}
