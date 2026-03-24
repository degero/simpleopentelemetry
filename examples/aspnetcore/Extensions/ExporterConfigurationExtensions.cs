using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;

namespace SimpleOpenTelemetry.Examples.AspNetCore.Extensions;


// TODO chad remove


/// <summary>
/// Sample-specific extension methods for configuring exporters based on SimpleOpenTelemetryOptions
/// </summary>
public static class ExporterConfigurationExtensions
{
    ///// <summary>
    ///// Configures the appropriate exporter (AzureMonitor, NewRelic, or OTLP) based on SimpleOpenTelemetryOptions
    ///// </summary>
    ///// <param name="builder">The OpenTelemetry builder</param>
    ///// <param name="options">The configuration options</param>
    ///// <returns>The builder for chaining</returns>
    //public static ISimpleOpenTelemetryBuilder Configure(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    SimpleOpenTelemetryBuilderOptions options)
    //{
    //    if (builder == null) throw new ArgumentNullException(nameof(builder));
    //    if (options == null) throw new ArgumentNullException(nameof(options));

    //    return options.Exporter?.ToUpper() switch
    //    {
    //        "AZUREMONITOR" => ConfigureAzureMonitor(builder),
    //        "NEWRELIC" => ConfigureNewRelic(builder),
    //        _ => ConfigureOtlp(builder)
    //    };
    //}

    ///// <summary>
    ///// Configures OTLP exporter
    ///// </summary>
    //private static ISimpleOpenTelemetryBuilder ConfigureOtlp(ISimpleOpenTelemetryBuilder builder)
    //{
    //    var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    //        ?? "http://localhost:4317";

    //    Console.WriteLine($"[OpenTelemetry] Configuring OTLP exporter with endpoint: {endpoint}");
    //    return builder.WithOtlpExporter(endpoint);
    //}

    ///// <summary>
    ///// Configures Azure Monitor exporter
    ///// </summary>
    //private static ISimpleOpenTelemetryBuilder ConfigureAzureMonitor(ISimpleOpenTelemetryBuilder builder)
    //{
    //    var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

    //    if (string.IsNullOrEmpty(connectionString))
    //    {
    //        Console.WriteLine("[OpenTelemetry] Azure Monitor exporter requires connection string in APPLICATIONINSIGHTS_CONNECTION_STRING environment variable");
    //        return ConfigureOtlp(builder);
    //    }

    //    Console.WriteLine("[OpenTelemetry] Configuring Azure Monitor exporter");
    //    return builder.WithAzureMonitorExporter(connectionString);
    //}

    ///// <summary>
    ///// Configures New Relic exporter (US endpoint)
    ///// </summary>
    //private static ISimpleOpenTelemetryBuilder ConfigureNewRelic(ISimpleOpenTelemetryBuilder builder)
    //{
    //    var apiKey = Environment.GetEnvironmentVariable("NEWRELIC_API_KEY");

    //    if (string.IsNullOrEmpty(apiKey))
    //    {
    //        Console.WriteLine("[OpenTelemetry] New Relic exporter requires API key in NEWRELIC_API_KEY environment variable");
    //        return ConfigureOtlp(builder);
    //    }

    //    Console.WriteLine("[OpenTelemetry] Configuring New Relic exporter (US endpoint)");
    //    return builder.WithNewRelicExporter(apiKey, null);
    //}

}

