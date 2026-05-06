namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
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

        var configSection = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        var config = configSection.Get<SimpleOpenTelemetryOptions>();
        
        if (config is null)
        {
            EventSource.Log.Error(eventCategory, $"No configuration section '{SimpleOpenTelemetryOptions.SectionName}'. This is required for SimpleOpenTelemetry");
            return null;
        }

        bool atLeastOneExists = configSection.GetSection("Log").Exists()
            || configSection.GetSection("Metric").Exists()
            || configSection.GetSection("Trace").Exists();
            
        if (!atLeastOneExists)
        {
            EventSource.Log.Error(eventCategory, $"Missing signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.");
            return null;
        }
        
        // Structured this way for more testability on injecting otelBuilder to SimpleOpenTelemetryBuilder
        var otelBuilder = services.AddOpenTelemetry();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);
        builder.Configure(); // Reads configuration and runs OpenTelemetry Fluent API methods

        return otelBuilder;
    }
}
