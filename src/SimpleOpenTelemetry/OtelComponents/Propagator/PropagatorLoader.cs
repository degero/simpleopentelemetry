using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Propagator;

internal class PropagatorLoader : LoaderBase, IPropagatorLoader
{
    protected override string ComponentKind => "Propagator";
    private readonly string eventCategory = nameof(PropagatorLoader);

    private readonly Dictionary<PropagatorEnum, AssemblyDescriptor> _descriptors = PropagatorAssemblies.KnownPropagators;

    /// <summary>
    /// Initializes a new instance of the PropagatorLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public PropagatorLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution)
    {
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
                if (noopPropagator is null)
                    throw new Exception("Cannot create a 'NoopTextMapPropagator'.");
                Sdk.SetDefaultTextMapPropagator(noopPropagator);
                EventSource.Log.Verbose(eventCategory, "Registered OpenTelemetry Propagator 'NoopTextMapPropagator' as SimpleOpenTelemetry propagators config included 'none'.");
                return;
            }

            var propagatorsList = new List<TextMapPropagator>();

            for (var i = 0; i < propagators.Count(); i++)
            {
                var item = propagators[i];

                if (TryParseKnown<PropagatorEnum>(item, out var matchedPropagator))
                {
                    if (TryGetDescriptor<PropagatorEnum, TextMapPropagator>(item, 
                            PropagatorAssemblies.KnownPropagators, 
                            out var descriptor, 
                            out var matchedEnum))
                    {
                        // If any fail the whoe propagator set is aborted
                        var propagatorInstance = CreatePropagator(descriptor);
                        if (propagatorInstance is not null)
                            propagatorsList.Add(propagatorInstance);   
                    }
                    else 
                        throw new Exception("Could not get descriptor for OpenTelemetry Propagator 'item'");
                }
                else 
                {
                    throw new InvalidOperationException($"Unsupported OpenTelemetry Propagator '{item}'. Please check your SimpleOpenTelemetry configuration.");
                }
            }

            // Register propagator
            var defaultPropagator = propagatorsList.Count > 1 ? (TextMapPropagator)new CompositeTextMapPropagator(propagatorsList) : propagatorsList[0] ?? throw new InvalidOperationException("No valid propagators configured.");
            Sdk.SetDefaultTextMapPropagator(defaultPropagator);
            EventSource.Log.Verbose(eventCategory, $"Registered OpenTelemetry Propagator(s) '{string.Join(", ", propagators)}'.");
        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register OpenTelemetry Propagators(s) '{string.Join(", ", propagators!)}'.", ex.Message);
        }
    }

    /// <summary>
    /// This may change as the current only supported vendor propagator (aws xray remote propagator)
    /// comes out of alpha / other vender patterns appear
    /// </summary>
    /// <param name="descriptor"></param>
    private TextMapPropagator CreatePropagator(
        AssemblyDescriptor descriptor)
    {
       
        var (assemblyName, typeName, _, _, _) = descriptor;

        // Dont need to load using AssemblyExec lib if OpenTelemetrySDK propagator
        var assembly = assemblyName == "OpenTelemetry.Api" ? typeof(OpenTelemetry.Context.RuntimeContext).Assembly : _assemblyExec.GetAssembly(assemblyName);

        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

        var instance = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException($"Failed to create instance of type '{typeName}'");
        
        return (TextMapPropagator)instance;
    }

}