using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.Resource;

internal interface IResourceDetectorLoader
{
    void AddResourceDetectors(ResourceBuilder builder, SimpleOpenTelemetryBuilderOptions options, ILogger? logger = null);
}

/// <summary>
/// Load vendor / contrib assembly and invoke resourcebuilder detector extension method based on the available types
/// </summary>
internal class ResourceDetectorLoader : IResourceDetectorLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    // Available 3rd parter detectors
    internal readonly Array _resourceExtensions = Enum.GetValues<ResourceDetectorEnum>();

    internal readonly Dictionary<ResourceDetectorEnum, ResourceDetectorDescriptor> _descriptors = ResourceDetectorAssemblies.KnownResourceDetectors;

    /// <summary>
    /// Initializes a new instance of the ResourceExtensionLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing resource detector settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public ResourceDetectorLoader(IConfiguration configuration)
    {
        _configuration = configuration;
        _assemblyExec = new AssemblyExecution();
    }

    /// <summary>
    /// Sets up resource detectors using detector method invocations on the provided ResourceBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource detectors from registered assemblies.
    /// </remarks>
    /// <param name="builder">The ResourceBuilder to configure.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when resource detector registration fails.</exception>
    public void AddResourceDetectors(ResourceBuilder builder,
        SimpleOpenTelemetryBuilderOptions options,
        ILogger? logger = null)
    {
        var detectors = options.Resource?.Detectors;

        if (detectors is not null && detectors.Any())
        {
            // Determine the valid detectors for the given builder type
            var validResourceExtensions = _resourceExtensions.Cast<object>()
                .Select(e => e.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < detectors.Count(); i++)
            {
                var item = detectors[i];

                if (validResourceExtensions.Cast<object>().Any(e => string.Equals(e.ToString(), item, StringComparison.OrdinalIgnoreCase)))
                {
                    var matchedResourceExtension = Enum.Parse(typeof(ResourceDetectorEnum), item, ignoreCase: true);

                    if (!_descriptors.TryGetValue((ResourceDetectorEnum)matchedResourceExtension, out var descriptor))
                        throw new InvalidOperationException(
                            $"Critical: {typeof(ResourceDetectorEnum).Name} type not found: {matchedResourceExtension} to initialise exporter");

                    AddResourceDetector(builder, descriptor, logger);
                }
                else
                {
                    // Throw an exception on an unknown exporter type
                    throw new InvalidOperationException($"Unsupported Resource Detector type: {item}. Please check your SimpleOpenTelemetry Configuration.");
                }
            }
        }
    }

    private void AddResourceDetector(
    ResourceBuilder builder,
    ResourceDetectorDescriptor descriptor,
    ILogger? logger = null)
    {

        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);
        var (assemblyName, typeName, methodName, configSection) = descriptor;
        var builderType = typeof(ResourceBuilder);
        var builderTypeName = builderType.GetType().Name;

        try
        {
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

            descriptor.MethodNames.ToList().ForEach(methodName =>
            {
                var parameterlessMethod = _assemblyExec.FindParameterlessMethodWithAllDefaultValues(type, builderType, methodName);
                var actionMethod = _assemblyExec.FindActionOverload(type, builderType, methodName);

                var section = descriptor.ConfigurationSection is not null ? _configuration.GetSection(descriptor.ConfigurationSection) : null;

                if (section is not null && section.Exists() && actionMethod is not null)
                    _assemblyExec.InvokeWithAction(actionMethod, builder, section);
                else
                    _assemblyExec.InvokeParameterlessOrDefaultedParameters(parameterlessMethod, builderType, builder);

                logger?.LogInformation("Successfully registered {TBuilder} Resource Detector: {Method}", builderTypeName, methodName);
            });

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel Resource Detector via {typeName}.{methodName}", ex);
        }
    }

}
