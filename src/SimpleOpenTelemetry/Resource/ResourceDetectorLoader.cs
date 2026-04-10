using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Builder;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.Resource;

internal interface IResourceDetectorLoader
{
    void AddResourceDetectors(ResourceBuilder builder, SimpleOpenTelemetryBuilderOptions options);
}

/// <summary>
/// Load vendor / contrib assembly and invoke resourcebuilder detector extension method based on the available types
/// </summary>
internal class ResourceDetectorLoader : IResourceDetectorLoader
{
    private readonly string eventCategory = nameof(ResourceDetectorLoader);
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    // Available 3rd parter detectors
    internal readonly Array _resourceExtensions = Enum.GetValues<ResourceDetectorEnum>();

    internal readonly Dictionary<ResourceDetectorEnum, ResourceDetectorDescriptor> _descriptors = ResourceDetectorAssemblies.KnownResourceDetectors;

    /// <summary>
    /// Initializes a new instance of the ResourceExtensionLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing resource detector settings.</param>
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
    public void AddResourceDetectors(ResourceBuilder builder,
        SimpleOpenTelemetryBuilderOptions options)
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

                try
                {
                    if (validResourceExtensions.Cast<object>().Any(e => string.Equals(e.ToString(), item, StringComparison.OrdinalIgnoreCase)))
                    {
                        var matchedResourceExtension = (ResourceDetectorEnum)Enum.Parse(typeof(ResourceDetectorEnum), item, ignoreCase: true);

                        if (!_descriptors.TryGetValue(matchedResourceExtension, out var descriptor))
                            throw new InvalidOperationException(
                                $"{typeof(ResourceDetectorEnum).Name} type '{matchedResourceExtension}' not found to initialise exporter.");

                        AddResourceDetector(matchedResourceExtension, builder, descriptor);
                    }
                    else
                    {
                        // Throw an exception on an unknown exporter type
                        throw new InvalidOperationException($"Unsupported Resource Detector type: '{item}'. Please check your SimpleOpenTelemetry configuration.");
                    }
                }
                catch(Exception ex)
                {
                    EventSource.Log.Error(eventCategory, "Failed to add otel resource detector '{item}'.", ex.Message);
                }
            }
        }
    }

    private void AddResourceDetector(
        ResourceDetectorEnum resourceDetector,
        ResourceBuilder builder,
        ResourceDetectorDescriptor descriptor)
    {

        var (assemblyName, typeName, methodName, configSection) = descriptor;
        var builderType = typeof(ResourceBuilder);
        var builderTypeName = builderType.GetType().Name;

        try
        {
            var assembly = _assemblyExec.GetAssembly(assemblyName);
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}.");

            descriptor.MethodNames.ToList().ForEach(methodName =>
            {
                var parameterlessMethod = _assemblyExec.FindParameterlessMethodWithAllDefaultValues(type, builderType, methodName);
                var actionMethod = _assemblyExec.FindActionOverload(type, builderType, methodName);

                var section = descriptor.ConfigurationSection is not null ? _configuration.GetSection(descriptor.ConfigurationSection) : null;

                if (section is not null && section.Exists() && actionMethod is not null)
                    _assemblyExec.InvokeWithAction(actionMethod, builder, section);
                else
                    _assemblyExec.InvokeParameterlessOrDefaultedParameters(parameterlessMethod, builderType, builder);

                EventSource.Log.Verbose(eventCategory, $"Added otel resource detector '{resourceDetector}' method '{methodName}'.");
            });

            EventSource.Log.Verbose(eventCategory, $"registered resource detector '{resourceDetector}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register resource Detector '{resourceDetector}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }

}
