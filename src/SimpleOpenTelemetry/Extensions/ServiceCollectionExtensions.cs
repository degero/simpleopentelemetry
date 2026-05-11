namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;

internal static class ServiceCollectionExtensions
{
    internal static OpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Structured this way for more testability on injecting otelBuilder to SimpleOpenTelemetryBuilder
        var otelBuilder = services.AddOpenTelemetry();

        // Only configure if basic settings set, always return an OpenTelemetryBuilder regardless
        // to not disrupt any custom code dependencies in app
        if (SimpleOpenTelemetryBuilder.ValidateConfiguration(configuration))
        {
            var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, configuration);
            builder.Configure(); // Reads configuration and runs OpenTelemetry Fluent API methods
        }

        return otelBuilder;
    }
}
