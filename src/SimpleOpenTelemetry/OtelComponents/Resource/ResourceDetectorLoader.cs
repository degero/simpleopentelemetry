using Microsoft.Extensions.Configuration;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Resource;

/// <summary>
/// Load vendor / otel-contrib assembly and invoke resourcebuilder detector extension method based on the available types
/// </summary>
internal class ResourceDetectorLoader : IResourceDetectorLoader
{
    private readonly string eventCategory = nameof(ResourceDetectorLoader);
    private readonly IAssemblyExecution _assemblyExec;

    internal readonly Dictionary<ResourceDetectorEnum, ResourceDetectorDescriptor> _descriptors = ResourceDetectorAssemblies.KnownResourceDetectors;

    /// <summary>
    /// Initializes a new instance of the ResourceExtensionLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public ResourceDetectorLoader(IAssemblyExecution assemblyExecution)
    {
        _assemblyExec = assemblyExecution;
    }

    /// <summary>
    /// Sets up resource detectors using detector method invocations on the provided ResourceBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource detectors from registered assemblies.
    /// </remarks>
    /// <param name="builder">The ResourceBuilder to configure.</param>
    public void AddResourceDetectors(ResourceBuilder builder,
        SimpleOpenTelemetryOptions options)
    {
        var detectors = options.Resource?.Detectors;

        if (detectors is not null && detectors.Any())
        {
            for (var i = 0; i < detectors.Count(); i++)
            {
                var item = detectors[i];

                try
                {
                    if (LoaderEnumHelper.TryParseKnown<ResourceDetectorEnum>(item, out var matchedResourceExtension))
                    {
                        if (!_descriptors.TryGetValue(matchedResourceExtension, out var descriptor))
                            throw new InvalidOperationException(
                                $"{typeof(ResourceDetectorEnum).Name} type '{matchedResourceExtension}' not found to initialise resource detector.");

                        AddResourceDetector(matchedResourceExtension, builder, options, descriptor);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported Resource Detector type '{item}'. Please check your SimpleOpenTelemetry configuration.");
                    }
                }
                catch(Exception ex)
                {
                    EventSource.Log.Error(eventCategory, $"Failed to add OpenTelemetry resource detector '{item}'.", ex.Message);
                }
            }
        }
    }

    private void AddResourceDetector(
        ResourceDetectorEnum resourceDetector,
        ResourceBuilder builder,
        SimpleOpenTelemetryOptions options,
        ResourceDetectorDescriptor descriptor)
    {

        var (assemblyName, typeName, methodNames, confgurationSection) = descriptor;
        var builderType = typeof(ResourceBuilder);

        try
        {
            var assembly = _assemblyExec.GetAssembly(assemblyName);
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}.");

            var section = descriptor.optionsClassName is not null ? options.Resource?.DetectorConfig?.GetSection(resourceDetector.ToString()) : null;

            methodNames.ToList().ForEach(methodName =>
            {
                var parameterlessMethod = _assemblyExec.FindParameterlessMethodWithAllDefaultValues(type, builderType, methodName);
                var actionMethod = _assemblyExec.FindActionOverload(type, builderType, methodName);

                if (section is not null && section.Exists() && actionMethod is not null)
                    _assemblyExec.InvokeWithAction(actionMethod, builder, section);
                else
                    _assemblyExec.InvokeParameterlessOrDefaultedParameters(parameterlessMethod, builderType, builder);
            });

            EventSource.Log.Verbose(eventCategory, $"Registered resource detector '{resourceDetector}' with registration methods '{string.Join(',', methodNames)}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register resource detector '{resourceDetector}' via '{typeName}.{string.Join(',', methodNames)}'.", ex.Message);
        }
    }

}
