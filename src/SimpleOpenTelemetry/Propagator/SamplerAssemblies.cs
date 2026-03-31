namespace SimpleOpenTelemetry.Propagator;

internal record PropagatorDescriptor(
     string AssemblyName,
     string TypeName
);

public enum PropagatorEnum
{
    /* opentelemetry-dotnet */
    None,
    
    Baggage,

    CompositeTextMap,

    TraceContext,

    /* opentelemetry-dotnet - OpenTelemetry.Extensions.Propagators.nupkg*/
    B3,

    /* opentelemetry-dotnet-contrib */
}

/// <summary>
/// A list of known opentelemetry-dotnet-contrib and vendor extensions
/// </summary>
internal static class PropagatorAssemblies
{
    public static readonly Dictionary<PropagatorEnum, PropagatorDescriptor>
       
        KnownPropagators = new()
        {
            /* opentelemetry-dotnet propagators */
            [PropagatorEnum.None] = new(
                "OpenTelemetry.Api",
                "OpenTelemetry.Context.Propagation.NoopTextMapPropagator"
            ),

            [PropagatorEnum.Baggage] = new(
                "OpenTelemetry.Api",
                "OpenTelemetry.Context.Propagation.BaggagePropagator"),

            [PropagatorEnum.CompositeTextMap] = new(
                "OpenTelemetry.Api",
                "OpenTelemetry.Context.Propagation.CompositeTextMapPropagator"),

            [PropagatorEnum.TraceContext] = new(
                "OpenTelemetry.Api",
                "OpenTelemetry.Context.Propagation.TraceContextPropagator"),

            /* opentelemetry-dotnet extensions propagators - OpenTelemetry.Extensions.Propagators.nupkg  */
            [PropagatorEnum.B3] = new(
                "OpenTelemetry.Extensions.Propagators",
                "OpenTelemetry.Extensions.Propagators.B3Propagator"
            )


        };

}

