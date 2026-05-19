using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Propagator;

/// <summary>
/// A list of known opentelemetry-dotnet-contrib and vendor propagators
/// </summary>
internal static class PropagatorAssemblies
{
    public static readonly Dictionary<PropagatorEnum, AssemblyDescriptor>
       
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

            [PropagatorEnum.TraceContext] = new(
                "OpenTelemetry.Api",
                "OpenTelemetry.Context.Propagation.TraceContextPropagator"),

            /* opentelemetry-dotnet extensions propagators - OpenTelemetry.Extensions.Propagators.nupkg  */
            [PropagatorEnum.B3] = new(
                "OpenTelemetry.Extensions.Propagators",
                "OpenTelemetry.Extensions.Propagators.B3Propagator"
            ),

            /* opentelemetry-dotnet-contrib propagators - OpenTelemetry.Extensions.AWS.nupkg */
            [PropagatorEnum.AWS] = new(
                "OpenTelemetry.Extensions.AWS",
                "OpenTelemetry.Extensions.AWS.Trace.AWSXRayPropagator"
            )
        };

}

