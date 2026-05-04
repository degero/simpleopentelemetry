namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;

internal static class ServiceCollectionExtensions
{
    internal static IOpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var configSection = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        var config = configSection.Get<SimpleOpenTelemetryOptions>();
        
        if (config is null)
            throw new Exception($"No configuration section '{SimpleOpenTelemetryOptions.SectionName}'. This is required for SimpleOpenTelemetry");

        bool atLeastOneExists = configSection.GetSection("Log").Exists()
            || configSection.GetSection("Metric").Exists()
            || configSection.GetSection("Trace").Exists();
            
        if (!atLeastOneExists)
            throw new Exception($"Signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.");

        // Structured this way for more testability on injecting otelBuilder to SimpleOpenTelemetryBuilder
        var otelBuilder = services.AddOpenTelemetry();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);
        builder.Configure(); // Reads configuration and runs OpenTelemetry Fluent API methods

        return otelBuilder;
    }
}
