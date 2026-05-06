namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.OtelComponents.Propagator;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.OtelComponents.Sampler;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

internal static class ServiceCollectionExtensions
{
    private static readonly string eventCategory = nameof(ServiceCollectionExtensions);

    internal static IOpenTelemetryBuilder? AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            EventSource.Log.Error(eventCategory, $"AddSimpleOpenTelemetry() IServiceCollection services parameter is null.");
            return null;
        }

        if (configuration == null)
          {
            EventSource.Log.Error(eventCategory, $"AddSimpleOpenTelemetry() IConfiguration configuration parameter is null.");
            return null;
        }
        
        if (!SimpleOpenTelemetryBuilder.ValidateConfiguration(configuration))
            return null;

        // Structured this way for more testability on injecting otelBuilder to SimpleOpenTelemetryBuilder
        var otelBuilder = services.AddOpenTelemetry();

        var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, configuration);
        builder.Configure(); // Reads configuration and runs OpenTelemetry Fluent API methods

        return otelBuilder;
    }
}
