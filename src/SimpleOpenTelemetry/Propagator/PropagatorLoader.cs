using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;

namespace SimpleOpenTelemetry.Propagator;


public class PropagatorLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    // Available 3rd parter propagators
    internal readonly Array _Propagators = Enum.GetValues<PropagatorEnum>();

    internal readonly Dictionary<PropagatorEnum, PropagatorDescriptor> _descriptors = PropagatorAssemblies.KnownPropagators;

    /// <summary>
    /// Initializes a new instance of the PropagatorLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing resource extension settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public PropagatorLoader(IConfiguration configuration)
    {
        // TODO Chad seems wrong Configuration is loaded in as the section for this lib
        _configuration = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        _assemblyExec = new AssemblyExecution();
    }

    /// <summary>
    /// Sets up propagator using a Builder currently only used with AWS Xray remote propagator.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource propagators from registered assemblies.
    /// </remarks>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when resource extension registration fails.</exception>
    internal void AddPropagators(
        SimpleOpenTelemetryBuilderOptions options,
        ILogger? logger = null)
    {
        var propagators = options.Propagators;

        if (propagators is not null && propagators.Any())
        {
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
                    var matchedPropagator = Enum.Parse(typeof(PropagatorEnum), item, ignoreCase: true);

                    if (!_descriptors.TryGetValue((PropagatorEnum)matchedPropagator , out var descriptor))
                        throw new InvalidOperationException(
                            $"Critical: {typeof(PropagatorEnum).Name} type not found: {matchedPropagator} to initialise propagator");

                    propagatorsList.Add(CreatePropagator(descriptor, (PropagatorEnum)matchedPropagator, logger));
                }
                else 
                {
                    // Throw an exception on an unknown exporter type
                    throw new InvalidOperationException($"Unsupported Propagator type: {item}. Please check your SimpleOpenTelemetry Configuration.");
                }
            }

            // TODO add try catch

            // Register propagator
            Sdk.SetDefaultTextMapPropagator(propagatorsList.Count > 1 ? new CompositeTextMapPropagator(propagatorsList) : propagatorsList[0]);
        
            logger?.LogInformation($"Successfully registered Propagator(s): {String.Join(", ", propagators)}.");

        }
    }

    /// <summary>
    /// This may change as the current only supported vendor propagator (aws xray remote propagator)
    /// comes out of alpha / other vender patterns appear
    /// </summary>
    /// <param name="descriptor"></param>
    /// <param name="logger"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    private TextMapPropagator CreatePropagator(
    PropagatorDescriptor descriptor,
    PropagatorEnum propagatorEnum,
    ILogger? logger = null)
    {
       
        var (assemblyName, typeName) = descriptor;


        try
        {
            // Dont need to load if OpenTelemetrySDK propagator
            var assembly = assemblyName == "OpenTelemetry.Api" ? typeof(OpenTelemetry.Context.RuntimeContext).Assembly : _assemblyExec.GetAssembly(assemblyName, logger);

            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

            var instance = Activator.CreateInstance(type);
          
            return (TextMapPropagator)instance;
        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel propagator {typeName}", ex);
        }
    }
}