using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.OtelComponents.Propagator;

internal interface IPropagatorLoader 
{
     void AddPropagators(SimpleOpenTelemetryOptions options);
}