
namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

/// <summary>
/// Enumeration of supported tracing instrumentations.
/// </summary>
public enum TraceInstrumentationEnum
{
    /// <summary>
    /// ASP.NET Core tracing instrumentation.
    /// </summary>
    AspNetCore,
    /// <summary>
    /// HTTP client tracing instrumentation.
    /// </summary>
    HttpClient,
    /// <summary>
    /// SQL client tracing instrumentation.
    /// </summary>
    SqlClient,
    /// <summary>
    /// Entity Framework Core tracing instrumentation.
    /// </summary>
    EFCore,
    /// <summary>
    /// Windows Communication Foundation tracing instrumentation.
    /// </summary>
    Wcf,
    /// <summary>
    /// AWS tracing instrumentation.
    /// </summary>
    AWS,
    /// <summary>
    /// AWS Lambda tracing instrumentation.
    /// </summary>
    AWSLambda
}
