using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;

namespace SimpleOpenTelemetry.Resource;

/// <summary>
/// Load vendor / contrib assembly and invoke resourcebuilder detector extension method based on the available types
/// linked to [Log/Trace/Metric]ResourceExtensionEnum
/// </summary>
internal class ResourceDetectorLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    // Available 3rd parter extensions
    internal readonly Array _resourceExtensions = Enum.GetValues<ResourceExtensionEnum>();

    internal readonly Dictionary<ResourceExtensionEnum, ResourceExtensionDescriptor> _descriptors = ResourceExtensionAssemblies.KnownResourceExtensions;

    /// <summary>
    /// Initializes a new instance of the ResourceExtensionLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing resource extension settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public ResourceDetectorLoader(IConfiguration configuration)
    {
        // TODO Chad seems wrong Configuration is loaded in as the section for this lib
        _configuration = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        _assemblyExec = new AssemblyExecution();
    }

    /// <summary>
    /// Sets up resource detectors using extension method invocations on the provided ResourceBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource extensions from registered assemblies.
    /// </remarks>
    /// <param name="builder">The ResourceBuilder to configure.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when resource extension registration fails.</exception>
    public void AddResourceDetectors(ResourceBuilder builder,
        SimpleOpenTelemetryBuilderOptions options,
        ILogger? logger = null)
    {
        var extensions = options.ResourceDetectors;

        if (extensions is not null && extensions.Any())
        {
            // Determine the valid extensions for the given builder type
            var validResourceExtensions = _resourceExtensions.Cast<object>()
                .Select(e => e.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < extensions.Count(); i++)
            {
                var item = extensions[i];

                if (validResourceExtensions.Cast<object>().Any(e => string.Equals(e.ToString(), item, StringComparison.OrdinalIgnoreCase)))
                {
                    var matchedResourceExtension = Enum.Parse(typeof(ResourceExtensionEnum), item, ignoreCase: true);

                    if (!_descriptors.TryGetValue((ResourceExtensionEnum)matchedResourceExtension , out var descriptor))
                        throw new InvalidOperationException(
                            $"Critical: {typeof(ResourceExtensionEnum).Name} type not found: {matchedResourceExtension} to initialise exporter");

                    AddResourceDetectorExtension(builder, descriptor, logger);
                }
                else 
                {
                    // Throw an exception on an unknown exporter type
                    throw new InvalidOperationException($"Unsupported Resource Extension type: {item}. Please check your SimpleOpenTelemetry Configuration.");
                }
            }
        }
    }

    private void AddResourceDetectorExtension(
    ResourceBuilder builder,
    ResourceExtensionDescriptor descriptor,
    ILogger? logger = null)
    {
       
        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);
        var (assemblyName, typeName, methodName) = descriptor;
        var builderType = typeof(ResourceBuilder);
        var builderTypeName = builderType.GetType().Name;

        try
        {
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

            descriptor.MethodNames.ToList().ForEach(methodName =>
            {
                var parameterlessMethod = _assemblyExec.FindParameterlessMethodWithAllDefaultValues(type, builderType, methodName);

                _assemblyExec.InvokeParameterlessOrDefaultedParameters(parameterlessMethod, builderType, builder);

                logger?.LogInformation("Successfully registered {TBuilder} Resource Extension: {Method}", builderTypeName, methodName);
            });

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel Resource Detector Extension via {typeName}.{methodName}", ex);
        }
    }

}
