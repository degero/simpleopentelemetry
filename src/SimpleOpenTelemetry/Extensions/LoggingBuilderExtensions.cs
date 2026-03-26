using Microsoft.Extensions.Logging;

namespace SimpleOpenTelemetry.Extensions;

/// <summary>
/// Extension methods for configuring SimpleOpenTelemetry with ILoggingBuilder.
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds SimpleOpenTelemetry logging instrumentation to the logging builder.
    /// </summary>
    /// <remarks>
    /// This method clears existing logging providers and configures OpenTelemetry logging
    /// with formatted messages and scopes enabled. Requires exporters to be configured
    /// via AddSimpleOpenTelemetry on the service collection.
    /// </remarks>
    /// <param name="logging">The logging builder instance.</param>
    /// <returns>The logging builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when logging is null.</exception>
    public static ILoggingBuilder AddSimpleOpenTelemetry(this ILoggingBuilder logging)
    {
        // Setup otel logging
        logging.ClearProviders();

        // TODO chad move to simple open tel config ?
        logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return logging;
    }
}
