using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Propagator;

internal class PropagatorLoader : IPropagatorLoader
{
    private readonly string eventCategory = nameof(PropagatorLoader);

    private readonly IAssemblyExecution _assemblyExec;

    // Available 3rd parter propagators
    private readonly Array _Propagators = Enum.GetValues<PropagatorEnum>();

    private readonly Dictionary<PropagatorEnum, PropagatorDescriptor> _descriptors = PropagatorAssemblies.KnownPropagators;

    /// <summary>
    /// Initializes a new instance of the PropagatorLoader class.
    /// </summary>
    public PropagatorLoader(IAssemblyExecution assemblyExecution)
    {
        _assemblyExec = assemblyExecution;
    }

    /// <summary>
    /// Sets up propagator using a Builder currently only used with AWS Xray remote propagator.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource propagators from registered assemblies.
    /// </remarks>
    public void AddPropagators(
        SimpleOpenTelemetryOptions options)
    {
        var propagators = options.Trace?.Propagators;

        try
        {

            if (propagators is null || !propagators.Any())
            {
                // Leave as the default created by the SDK initialisation - CompositeTextMapPropagator: 'tracestate','traceparent','baggage' 
                return;
            }

            if (propagators.Any(p => string.Equals(p, PropagatorEnum.None.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                var noopPropagator = CreatePropagator(_descriptors[PropagatorEnum.None]);
                Sdk.SetDefaultTextMapPropagator(noopPropagator);
                EventSource.Log.Verbose(eventCategory, "Registered propagator NoopTextMapPropagator as SimpleOpenTelemetry propagators config included 'none'.");
                return;
            }

            // Determine the valid propagators for the given builder type
            var validPropagators = _Propagators.Cast<object>()
                .Select(e => e.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var propagatorsList = new List<TextMapPropagator>();

            for (var i = 0; i < propagators.Count(); i++)
            {
                var item = propagators[i];

                if (validPropagators.Cast<object>().Any(e => string.Equals(e.ToString(), item, StringComparison.OrdinalIgnoreCase)))
                {
                    var matchedPropagator = (PropagatorEnum)Enum.Parse(typeof(PropagatorEnum), item, ignoreCase: true);

                    if (!_descriptors.TryGetValue(matchedPropagator , out var descriptor))
                        throw new InvalidOperationException(
                            $"{typeof(PropagatorEnum).Name} type '{matchedPropagator}' not found to initialise propagator.");
                    
                    // If any fail the whoe propagator set is aborted
                    var propagatorInstance = CreatePropagator(descriptor);
                    
                    if (propagatorInstance is not null)
                        propagatorsList.Add(propagatorInstance);   
                }
                else 
                {
                    // Throw an exception on an unknown exporter type
                    throw new InvalidOperationException($"Unsupported otel propagator '{item}'. Please check your SimpleOpenTelemetry configuration.");
                }
            }

            // Register propagator
            Sdk.SetDefaultTextMapPropagator(propagatorsList.Count > 1 ? new CompositeTextMapPropagator(propagatorsList) : propagatorsList[0]);
            EventSource.Log.Verbose(eventCategory, $"registered propagator(s) '{string.Join(", ", propagators)}'.");
        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register propagators(s) '{string.Join(", ", propagators)}'.", ex.Message);
        }
    }

    /// <summary>
    /// This may change as the current only supported vendor propagator (aws xray remote propagator)
    /// comes out of alpha / other vender patterns appear
    /// </summary>
    /// <param name="descriptor"></param>
    private TextMapPropagator? CreatePropagator(
        PropagatorDescriptor descriptor)
    {
       
        var (assemblyName, typeName) = descriptor;

        // Dont need to load using AssemblyExec lib if OpenTelemetrySDK propagator
        var assembly = assemblyName == "OpenTelemetry.Api" ? typeof(OpenTelemetry.Context.RuntimeContext).Assembly : _assemblyExec.GetAssembly(assemblyName);

        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

        var instance = Activator.CreateInstance(type, nonPublic: true);
        
        return (TextMapPropagator)instance;
    }

}