namespace SimpleOpenTelemetry.OtelComponents.Propagator;

public enum PropagatorEnum
{
    /* opentelemetry-dotnet */
    None,
    Baggage,
    TraceContext,

    /* opentelemetry-dotnet - OpenTelemetry.Extensions.Propagators.nupkg*/
    B3,

    /* opentelemetry-dotnet-contrib */
    AWS,
}
