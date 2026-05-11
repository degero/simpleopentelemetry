namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

/// <summary>
/// Enumeration of supported metrics instrumentations.
/// </summary>
public enum MetricInstrumentationEnum
{
    /* opentelemetry-dotnet-contrib */
    /// <summary>
    /// ASP.NET Core metrics instrumentation.
    /// </summary>
    AspNetCore,
    /// <summary>
    /// HTTP client metrics instrumentation.
    /// </summary>
    HttpClient,
    /// <summary>
    /// SQL client metrics instrumentation.
    /// </summary>
    SqlClient,
    /// <summary>
    /// Runtime metrics instrumentation.
    /// </summary>
    Runtime,
    /// <summary>
    /// Process metrics instrumentation.
    /// </summary>
    Process,
    /// <summary>
    /// AWS metrics instrumentation.
    /// </summary>
    AWS
}
