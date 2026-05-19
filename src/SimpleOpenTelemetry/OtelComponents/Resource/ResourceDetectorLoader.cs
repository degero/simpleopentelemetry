using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Reflection;

namespace SimpleOpenTelemetry.OtelComponents.Resource;

/// <summary>
/// Load vendor / otel-contrib assembly and invoke resourcebuilder detector extension method based on the available types
/// </summary>
internal class ResourceDetectorLoader : LoaderBase, IResourceDetectorLoader
{
    protected override string ComponentKind => "ResourceDetector";

    /// <summary>
    /// Initializes a new instance of the ResourceExtensionLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public ResourceDetectorLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution)
    {
    }

    /// <summary>
    /// Sets up resource detectors using detector method invocations on the provided ResourceBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource detectors from registered assemblies.
    /// </remarks>
    /// <param name="builder">The ResourceBuilder to configure.</param>
    /// <param name="options">The SimpleOpenTelemetry configuration containing resource detector settings.</param>
    public void AddResourceDetectors(ResourceBuilder builder,
        SimpleOpenTelemetryOptions options)
    {
        TryInvokeComponents(options.Resource?.Detectors, 
            builder, 
            ResourceDetectorAssemblies.KnownResourceDetectors, 
            options,
            (descriptor, options, component) =>
                {
                    return descriptor.OptionsClassName is not null ? options.Resource?.DetectorConfig?.GetSection(component.ToString()) : null;
                }
            );
    }
}
