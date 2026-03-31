namespace SimpleOpenTelemetry.Instrumentation;

// TODO Chad check these as some may be missing
public enum MetricInstrumentationEnum
{
    /* opentelemetry-dotnet-contrib */
    AspNetCore,
    HttpClient,
    SqlClient,
    Runtime,
    Process,
    Hangfire,
    AWS
}
