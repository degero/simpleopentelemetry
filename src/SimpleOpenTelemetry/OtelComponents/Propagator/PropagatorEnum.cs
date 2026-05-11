namespace SimpleOpenTelemetry.OtelComponents.Propagator;

/// <summary>
/// Enumeration of supported propagators.
/// </summary>
public enum PropagatorEnum
{
    /* opentelemetry-dotnet */
    /// <summary>
    /// No propagator.
    /// </summary>
    None,
    /// <summary>
    /// Baggage propagator.
    /// </summary>
    Baggage,
    /// <summary>
    /// W3C Trace Context propagator.
    /// </summary>
    TraceContext,

    /* opentelemetry-dotnet - OpenTelemetry.Extensions.Propagators.nupkg*/
    /// <summary>
    /// B3 propagator.
    /// </summary>
    B3,

    /* opentelemetry-dotnet-contrib */
    /// <summary>
    /// AWS propagator.
    /// </summary>
    AWS,
}
