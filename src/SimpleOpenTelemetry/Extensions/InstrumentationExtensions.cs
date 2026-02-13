namespace SimpleOpenTelemetry.Extensions;

using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

/// <summary>
/// Extension methods for adding instrumentation to SimpleOpenTelemetry
/// </summary>
public static class InstrumentationExtensions
{
    /// <summary>
    /// Adds ASP.NET Core instrumentation
    /// </summary>
    public static ISimpleOpenTelemetryBuilder WithAspNetCoreInstrumentation(
        this ISimpleOpenTelemetryBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.ConfigureTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
        });

        return builder;
    }

    /// <summary>
    /// Adds HTTP Client instrumentation
    /// </summary>
    public static ISimpleOpenTelemetryBuilder WithHttpClientInstrumentation(
        this ISimpleOpenTelemetryBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.ConfigureTracing(tracing =>
        {
            tracing.AddHttpClientInstrumentation();
        });

        return builder;
    }

    /// <summary>
    /// Adds SQL Client instrumentation
    /// </summary>
    public static ISimpleOpenTelemetryBuilder WithSqlClientInstrumentation(
        this ISimpleOpenTelemetryBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.ConfigureTracing(tracing =>
        {
            tracing.AddSqlClientInstrumentation();
        });

        return builder;
    }

    /// <summary>
    /// Adds Entity Framework Core instrumentation
    /// </summary>
    public static ISimpleOpenTelemetryBuilder WithEntityFrameworkCoreInstrumentation(
        this ISimpleOpenTelemetryBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.ConfigureTracing(tracing =>
        {
            tracing.AddEntityFrameworkCoreInstrumentation();
        });

        return builder;
    }
}
