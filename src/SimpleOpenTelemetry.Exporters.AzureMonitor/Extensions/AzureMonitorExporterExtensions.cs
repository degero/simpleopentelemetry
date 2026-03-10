namespace SimpleOpenTelemetry.Exporters.AzureMonitor.Extensions;

using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

/// <summary>
/// Extension methods for adding Azure Monitor exporter to Simple OpenTelemetry
/// </summary>
public static class AzureMonitorExporterExtensions
{
    /// <summary>
    /// Adds Azure Monitor (Application Insights) exporter with connection string
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <param name="connectionString">Application Insights connection string</param>
    /// <param name="configure">Optional additional configuration</param>
    public static ISimpleOpenTelemetryBuilder WithAzureMonitorExporter(
        this ISimpleOpenTelemetryBuilder builder,
        string connectionString,
        Action<AzureMonitorExporterOptions>? configure = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        builder.OtelBuilder.WithTracing(tracing =>
        {
            tracing.AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = connectionString;
                configure?.Invoke(options);
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds Azure Monitor (Application Insights) exporter with custom configuration
    /// </summary>
    /// <param name="builder">The builder</param>
    /// <param name="configure">Configuration action</param>
    public static ISimpleOpenTelemetryBuilder WithAzureMonitorExporter(
        this ISimpleOpenTelemetryBuilder builder,
        Action<AzureMonitorExporterOptions> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        builder.OtelBuilder.WithTracing(tracing =>
        {
            tracing.AddAzureMonitorTraceExporter(configure);
        });

        return builder;
    }

    /// <summary>
    /// Adds Azure Monitor (Application Insights) exporter using connection string from environment variable
    /// Looks for APPLICATIONINSIGHTS_CONNECTION_STRING environment variable
    /// </summary>
    public static ISimpleOpenTelemetryBuilder WithAzureMonitorExporter(
        this ISimpleOpenTelemetryBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Azure Monitor connection string not found. " +
                "Set APPLICATIONINSIGHTS_CONNECTION_STRING environment variable or use the overload that accepts a connection string.");
        }

        // Use the first overload with optional configuration
        return WithAzureMonitorExporter(builder, connectionString);
    }
}