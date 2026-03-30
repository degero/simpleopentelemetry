namespace SimpleOpenTelemetry.Propagator;

internal record PropagatorDescriptor(
     string AssemblyName,
     string TypeName
);

public enum PropagatorEnum
{
    /* opentelemetry-dotnet */
    B3,

    Baggage,

    CompositeTextMapPropagator,

    TraceContextPropagator 
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

            [PropagatorEnum.Baggage] = new(
                "OpenTelemetry.Api",
                "OpenTelemetry.Context.Propagation.BaggagePropagator"),

            
        };

}

