namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;

/// <summary>
/// Extension methods for adding OpenTelemetry to service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Runs SimpleOpenTelemetryBuilder to initialise OpenTelemetry and process custom
    /// env var / json config (in section 'SimpleOpenTelemetry') into OpenTelemetry instrumentation
    /// logging and exporting etc setups.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing SimpleOpenTelemetry settings</param>
    /// <returns>The service collection</returns>
    public static IOpenTelemetryBuilder AddSimpleOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        
        if (!section.Exists())
            throw new Exception($"No configuration section '{SimpleOpenTelemetryConfiguration.SectionName}'. This is required for SimpleOpenTelemetry");

        bool atLeastOneExists = 
            section.GetSection(SimpleOpenTelemetryBuilderOptions.LogSectionName).Exists() ||
            section.GetSection(SimpleOpenTelemetryBuilderOptions.MetricSectionName).Exists() ||
            section.GetSection(SimpleOpenTelemetryBuilderOptions.TraceSectionName).Exists();

        if (!atLeastOneExists)
            throw new Exception($"Signal configuration subsections in '{SimpleOpenTelemetryConfiguration.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.");

        // TODO Chad move elsewhere? 
        var otelBuilder = services.AddOpenTelemetry();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, configuration);
        return builder.Configure();
    }
}
