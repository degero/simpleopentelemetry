namespace SimpleOpenTelemetry.Exporters.NewRelic.Extensions;

using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

/// <summary>
/// Extension methods for adding New Relic exporter to Simple OpenTelemetry
/// </summary>
public static class NewRelicExporterExtensions
{
    private const string DefaultNewRelicEndpoint = "https://otlp.nr-data.net:4317";

    ///// <summary>
    ///// Adds New Relic exporter with API key
    ///// </summary>
    ///// <param name="builder">The builder</param>
    ///// <param name="apiKey">New Relic Ingest License Key</param>
    ///// <param name="endpoint">New Relic OTLP endpoint (defaults to US endpoint)</param>
    ///// <param name="configure">Optional additional configuration</param>
    //private static ISimpleOpenTelemetryBuilder WithNewRelicExporter(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    string apiKey,
    //    string? endpoint = null,
    //    Action<OtlpExporterOptions>? configure = null)
    //{
    //    if (builder == null) throw new ArgumentNullException(nameof(builder));
    //    if (string.IsNullOrWhiteSpace(apiKey))
    //        throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

       

    //    return builder;
    //}

    /// <summary>
    /// Adds New Relic exporter using API key from environment variable
    /// Looks for NEWRELIC_API_KEY environment variable
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <param name="endpoint">New Relic OTLP endpoint (defaults to US endpoint)</param>
    /// <param name="configure">Optional additional configuration</param>
    public static ISimpleOpenTelemetryBuilder WithNewRelicExporter(
        this ISimpleOpenTelemetryBuilder builder,
        string? apiKey = null,
        string? endpoint = null,
        Action<OtlpExporterOptions>? configure = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        var foundApiKey = apiKey ?? Environment.GetEnvironmentVariable("NEWRELIC_API_KEY");

        if (string.IsNullOrWhiteSpace(foundApiKey))
        {
            throw new InvalidOperationException(
                "New Relic API key not found. " +
                "Set NEWRELIC_API_KEY environment variable or use the overload that accepts an API key.");
        }

        var foundEndpoint = endpoint ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? DefaultNewRelicEndpoint;
         
        var finalEndpoint = foundEndpoint ?? DefaultNewRelicEndpoint;

        builder.OtelBuilder.WithTracing(tracing =>
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(finalEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                // TODO use OTEL_EXPORTER_OTLP_HEADERS instead
                options.Headers = $"api-key={apiKey}";
                
                configure?.Invoke(options);
            });
        });
        return builder;
    }

//      public static void RegisterNewRelicExporter(this OpenTelemetryBuilder builder, params string[] args)
//   {

//     // New Relic OTLP endpoint
//     var otlpEndpoint = "https://otlp.nr-data.net:4317";
//     var nrLicenseKey = args[0]; // Set in env var OTEL_NEWRELIC_API_KEY

//     var exportOptions = (OtlpExporterOptions opts) =>
//     {
//       opts.Endpoint = new Uri(otlpEndpoint);
//       // TODO use OTEL_EXPORTER_OTLP_HEADERS instead
//       opts.Headers = $"api-key={nrLicenseKey}";
//     };

//     builder.WithTracing(tracing => tracing.AddOtlpExporter(exportOptions))
//            .WithMetrics(metrics => metrics.AddOtlpExporter(exportOptions))
//            .WithLogging(logging => logging.AddOtlpExporter(exportOptions));
//   }
}

