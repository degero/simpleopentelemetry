namespace SimpleOpenTelemetry.Extensions;

using Microsoft.Extensions.Diagnostics.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;
using SimpleOpenTelemetry.Exporters;

/// <summary>
/// Extension methods for configuring SimpleOpenTelemetry from strongly-typed options
/// </summary>
public static class SimpleOpenTelemetryConfigurationExtensions
{
    /// <summary>
    /// Configures OTLP exporter from options.
    /// For other exporters (AzureMonitor, NewRelic), samples should implement their own exporter selection logic.
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <param name="options">The configuration options</param>
    /// <returns>The builder for chaining</returns>
    //public static ISimpleOpenTelemetryBuilder ConfigureOtlpExporterFromOptions(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    SimpleOpenTelemetryBuilderOptions options)
    //{
    //    if (builder == null) throw new ArgumentNullException(nameof(builder));
    //    if (options == null) throw new ArgumentNullException(nameof(options));

        // TODO chad leave these out as user can configure through std OTEL Env vars or functions
        //// Set service name and version from options if provided
        //if (!string.IsNullOrWhiteSpace(options.ServiceName))
        //{
        //    builder.WithServiceName(options.ServiceName);
        //}

        //if (!string.IsNullOrWhiteSpace(options.ServiceVersion))
        //{
        //    builder.WithServiceVersion(options.ServiceVersion);
        //}

        //// Configure exporter - defaults to OTLP
        //var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
        //    ?? "http://localhost:4317";

        //Console.WriteLine($"[OpenTelemetry] Configuring OTLP exporter with endpoint: {endpoint}");
        //return builder.WithOtlpExporter(endpoint);
   // }

    ///// <summary>
    ///// Conditionally adds ASP.NET Core instrumentation based on enabled flag
    ///// </summary>
    ///// <param name="builder">The OpenTelemetry builder</param>
    ///// <param name="enabled">Whether to enable instrumentation</param>
    ///// <returns>The builder for chaining</returns>
    //public static ISimpleOpenTelemetryBuilder ConditionalWithAspNetCoreInstrumentation(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    bool enabled)
    //{
    //    return enabled ? builder.WithAspNetCoreInstrumentation() : builder;
    //}

    ///// <summary>
    ///// Conditionally adds HTTP client instrumentation based on enabled flag
    ///// </summary>
    ///// <param name="builder">The OpenTelemetry builder</param>
    ///// <param name="enabled">Whether to enable instrumentation</param>
    ///// <returns>The builder for chaining</returns>
    //public static ISimpleOpenTelemetryBuilder ConditionalWithHttpClientInstrumentation(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    bool enabled)
    //{
    //    return enabled ? builder.WithHttpClientInstrumentation() : builder;
    //}

    ///// <summary>
    ///// Conditionally adds SQL client instrumentation based on enabled flag
    ///// </summary>
    ///// <param name="builder">The OpenTelemetry builder</param>
    ///// <param name="enabled">Whether to enable instrumentation</param>
    ///// <returns>The builder for chaining</returns>
    //public static ISimpleOpenTelemetryBuilder ConditionalWithSqlClientInstrumentation(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    bool enabled)
    //{
    //    return enabled ? builder.WithSqlClientInstrumentation() : builder;
    //}

    //public static ISimpleOpenTelemetryBuilder WithLogging(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    Action<LoggerProviderBuilder> action)
    //{
    //    builder.OtelBuilder.WithLogging(action);
    //    return builder;
    //}

    //public static ISimpleOpenTelemetryBuilder WithTracing(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    Action<TracerProviderBuilder> action)
    //{
    //    builder.OtelBuilder.WithTracing(action);
    //    return builder;
    //}


    //public static ISimpleOpenTelemetryBuilder WithMetrics(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    Action<MeterProviderBuilder> action)
    //{
    //    builder.OtelBuilder.WithMetrics(action);
    //    return builder;
    //}
}
