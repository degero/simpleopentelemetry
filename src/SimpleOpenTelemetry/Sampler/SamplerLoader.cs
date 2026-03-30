using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;

namespace SimpleOpenTelemetry.Sampler;

/// <summary>
/// Load vendor / contrib assembly and invoke TracerProviderBuilder detector extension method based on the available types
/// linked to [Log/Trace/Metric]SamplerEnum
/// </summary>
internal class SamplerLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    // Available 3rd parter extensions
    internal readonly Array _Samplers = Enum.GetValues<SamplerEnum>();

    internal readonly Dictionary<SamplerEnum, SamplerDescriptor> _descriptors = SamplerAssemblies.KnownSamplers;

    /// <summary>
    /// Initializes a new instance of the SamplerLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing resource extension settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public SamplerLoader(IConfiguration configuration)
    {
        // TODO Chad seems wrong Configuration is loaded in as the section for this lib
        _configuration = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        _assemblyExec = new AssemblyExecution();
    }

    /// <summary>
    /// Sets up sampler using a Builder currently only used with AWS Xray remote sampler.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures resource extensions from registered assemblies.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to register the sampler with.</param>
    /// <param name="resource">The Resource builder resource to configure with.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when resource extension registration fails.</exception>
    public void AddSampler(TracerProviderBuilder builder,
        OpenTelemetry.Resources.Resource resource,
        SimpleOpenTelemetryBuilderOptions options,
        ILogger? logger = null)
    {
        var entry = options.Sampler;
        
        if (!string.IsNullOrWhiteSpace(entry))
        {
            // Determine the valid extensions for the given builder type
            var validSamplers = _Samplers.Cast<object>()
                .Select(e => e.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var item = entry;

            if (validSamplers.Cast<object>().Any(e => string.Equals(e.ToString(), item, StringComparison.OrdinalIgnoreCase)))
            {
                var matchedSampler = Enum.Parse(typeof(SamplerEnum), item, ignoreCase: true);

                if (!_descriptors.TryGetValue((SamplerEnum)matchedSampler , out var descriptor))
                    throw new InvalidOperationException(
                        $"Critical: {typeof(SamplerEnum).Name} type not found: {matchedSampler} to initialise sampler");

                AddSampler(builder, resource, descriptor, logger);
            }
            else 
            {
                // Throw an exception on an unknown exporter type
                throw new InvalidOperationException($"Unsupported Sampler type: {item}. Please check your SimpleOpenTelemetry Configuration.");
            }
        }
    }

    private void AddSampler(TracerProviderBuilder builder,
    OpenTelemetry.Resources.Resource  resource,
    SamplerDescriptor descriptor,
    ILogger? logger = null)
    {
       
        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);
        var (assemblyName, typeName, methodName) = descriptor;

        try
        {
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

            var instance = method.Invoke(null, new object[] { resource });

            var buildMethod = instance.GetType().GetMethod("Build");

            var sampler = buildMethod.Invoke(instance, new object[] {}) as OpenTelemetry.Trace.Sampler;

            builder.SetSampler(sampler);

            logger?.LogInformation("Successfully registered Sampler : {typeName}", typeName);

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel sampler via Builder {typeName}", ex);
        }
    }

}
