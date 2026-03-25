using Microsoft.Extensions.Logging;

namespace SimpleOpenTelemetry.Extensions;

public static class LoggingBuilderExtensions
{

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
