namespace SimpleOpenTelemetry.Exporters.PrometheusScrape;

// TODO Chad is this adding anything useful?
public class PrometheusScrape
{
    public static ISimpleOpenTelemetryBuilder WithPrometheusScrapeExporter(
            this ISimpleOpenTelemetryBuilder builder
            )
    {
        // Configure the Prometheus scraping endpoint
        app.MapPrometheusScrapingEndpoint();
    }
}
