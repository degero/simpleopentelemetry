using System.Reflection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Sampler;

/// <summary>
/// Load vendor / contrib assembly and invoke static / exntion method creating a Builder based on the available types
/// </summary>
internal class SamplerLoader : ISamplerLoader
{
    private readonly string eventCategory = nameof(SamplerLoader);

    private readonly IAssemblyExecution _assemblyExec;

    // Available 3rd party samplers
    internal readonly Dictionary<SamplerEnum, SamplerDescriptor> _descriptors = SamplerAssemblies.KnownSamplers;

    /// <summary>
    /// Initializes a new instance of the SamplerLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public SamplerLoader(IAssemblyExecution assemblyExecution)
    {
        _assemblyExec = assemblyExecution;
    }

    /// <summary>
    /// Sets up sampler using a Builder currently only used with AWS Xray remote sampler.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource extensions from registered assemblies.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to register the sampler with.</param>
    public void AddSampler(TracerProviderBuilder builder,
        SimpleOpenTelemetryOptions options)
    {
        var item = options.Trace?.Sampler;

        if (!string.IsNullOrWhiteSpace(item))
        {
            try
            {
                if (LoaderEnumHelper.TryParseKnown<SamplerEnum>(item, out var matchedSampler))
                {
                    if (!_descriptors.TryGetValue(matchedSampler, out var descriptor))
                        throw new InvalidOperationException(
                            $"{typeof(SamplerEnum).Name} type not found: {matchedSampler} to initialize sampler");

                    AddSampler(builder, descriptor);

                    EventSource.Log.Verbose(eventCategory, $"Registered sampler '{matchedSampler}'.");

                }
                else
                {
                    EventSource.Log.Error(eventCategory, $"Unsupported OpenTelemetry sampler '{item}'. Please check your SimpleOpenTelemetry configuration.");
                }
            }
            catch (Exception ex)
            {
                EventSource.Log.Error(eventCategory, $"Failed to register sampler '{item}'.", ex.Message);
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
        SamplerDescriptor descriptor)
    {

        var (assemblyName, typeName, methodName) = descriptor;
        var assembly = _assemblyExec.GetAssembly(assemblyName);
        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}.");

        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

        var instance = method.Invoke(null, new object[] {  });

        // As AWS Xray remote sampler only provides a static method to get a builder and requies a Build()
        // This is kept here for now
        var buildMethod = instance.GetType().GetMethod("Build");

        var sampler = buildMethod.Invoke(instance, new object[] { }) as OpenTelemetry.Trace.Sampler;

        builder.SetSampler(sampler);
    }
}