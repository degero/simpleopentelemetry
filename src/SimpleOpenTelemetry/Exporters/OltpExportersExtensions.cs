namespace SimpleOpenTelemetry.Exporters;

using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

/// <summary>
/// Extension methods for adding OTLP exporter to Simple OpenTelemetry
/// </summary>
public static class OtlpExporterExtensions
{
    /// <summary>
    /// Adds OTLP exporter with the specified endpoint
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <param name="endpoint">The OTLP endpoint (e.g., "http://localhost:4317")</param>
    /// <param name="configure">Optional additional configuration</param>
    public static ISimpleOpenTelemetryBuilder WithOtlpExporter(
        this ISimpleOpenTelemetryBuilder builder,
        string? endpoint,
        Action<OtlpExporterOptions>? configure = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        builder.OtelBuilder.WithTracing(tracing =>
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(endpoint);
                configure?.Invoke(options);
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds OTLP exporter with custom configuration
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <param name="configure">Configuration action</param>
    public static ISimpleOpenTelemetryBuilder WithOtlpExporter(
        this ISimpleOpenTelemetryBuilder builder,
        Action<OtlpExporterOptions> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        builder.OtelBuilder.WithTracing(tracing =>
        {
            tracing.AddOtlpExporter(configure);
        });

        return builder;
    }

    /// <summary>
    /// Adds OTLP exporter with default configuration (localhost:4317)
    /// </summary>
    public static ISimpleOpenTelemetryBuilder WithOtlpExporter(
        this ISimpleOpenTelemetryBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.OtelBuilder.WithTracing(tracing =>
        {
            tracing.AddOtlpExporter();
        });

        return builder;
    }
}