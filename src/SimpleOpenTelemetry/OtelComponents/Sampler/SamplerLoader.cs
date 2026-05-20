using System.Reflection;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Sampler;

/// <summary>
/// Load vendor / contrib assembly and invoke static / exntion method creating a Builder based on the available types
/// </summary>
internal class SamplerLoader : LoaderBase, ISamplerLoader
{
    protected override string ComponentKind => "Sampler";

    private readonly string eventCategory = nameof(SamplerLoader);

    /// <summary>
    /// Initializes a new instance of the SamplerLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public SamplerLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution)
    {
    }


    /// <summary>
    /// Adds a sampler to the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures samplers from registered assemblies.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to register the sampler with.</param>
    /// <param name="options">The SimpleOpenTelemetry configuration containing sampler settings.</param>
    public void SetSampler(TracerProviderBuilder builder,
        SimpleOpenTelemetryOptions options)
    {
        var item = options.Trace?.Sampler;

        if (!string.IsNullOrWhiteSpace(item))
        {
            try
            {
                if (TryParseKnown<SamplerEnum>(item, out var matchedSampler))
                {
                    if (!SamplerAssemblies.KnownSamplers.TryGetValue(matchedSampler, out var descriptor))
                        throw new InvalidOperationException(
                            $"{typeof(SamplerEnum).Name} type not found: {matchedSampler} to initialize sampler");

                    AddSampler(builder, descriptor);

                    EventSource.Log.VerboseEvent(eventCategory, $"Registered OpenTelemetry Sampler '{matchedSampler}'.");

                }
                else
                {
                    EventSource.Log.ErrorEvent(eventCategory, $"OpenTelemetry Sampler {typeof(SamplerEnum).Name} type '{item}' not found to initialise. Please check your SimpleOpenTelemetry configuration.");
                }
            }
            catch (Exception ex)
            {
                EventSource.Log.ErrorEvent(eventCategory, $"Failed to register OpenTelemetry Sampler '{item}'.", ex);
            }
        }
    }

    /// <summary>
    /// This may change as the current only supported vendor sampler (aws xray remote sampler)
    /// comes out of alpha / other vender patterns appear
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="descriptor"></param>
    private void AddSampler(TracerProviderBuilder builder,
        AssemblyDescriptor descriptor)
    {

        var (assemblyName, typeName, methodNames, _, _) = descriptor;
        var assembly = _assemblyExec.GetAssembly(assemblyName);
        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}.");

        var method = type.GetMethod(methodNames[0], BindingFlags.Static | BindingFlags.Public);

        var instance = method?.Invoke(null, []);

        // As AWS Xray remote sampler only provides a static method to get a builder and requies a Build()
        // This is kept here for now
        var buildMethod = instance?.GetType().GetMethod("Build");

        var sampler = buildMethod?.Invoke(instance, []) as OpenTelemetry.Trace.Sampler;
        if (sampler is not null)
            builder.SetSampler(sampler);
        else
            throw new Exception($"Cannot initialise sampler: {descriptor.TypeName}.");
    }
}