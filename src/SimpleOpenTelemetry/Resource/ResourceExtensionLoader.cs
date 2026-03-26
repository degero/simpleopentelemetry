using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;

namespace SimpleOpenTelemetry.Resource;

/// <summary>
/// Load vendor / contrib assembly and invoke resourcebuilder extension method based on the available types
/// linked to [Log/Trace/Metric]ResourceExtensionEnum
/// </summary>
public class ResourceExtensionLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    private readonly string _exportersTopLevelConfigSectionName = "Resources";

   // Available 3rd parter extensions
    internal readonly Array _resourceExtensions = Enum.GetValues<ResourceExtensionEnum>();
  
    internal readonly Dictionary<ResourceExtensionEnum, ResourceExtensionDescriptor> _descriptors = ResourceExtensionAssemblies.KnownResourceExtensions;
    
    public ResourceExtensionLoader(IConfiguration configuration)
    {
        // TODO Chad seems wrong Configuration is loaded in as the section for this lib
        _configuration = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        _assemblyExec = new AssemblyExecution();
    }

    public void SetupResourceExtensions(ResourceBuilder builder,
        ILogger logger)
    {
        // TODO chad fix
        _configuration.GetSection(_exportersTopLevelConfigSectionName).GetChildren();
        
        var resources = new string[]{"azure"};

        ConfigureResourceExtensions(builder, resources,logger);
    }

    private void ConfigureResourceExtensions(ResourceBuilder builder,
        IList<string> extensions,
        ILogger logger)
    {
        // Determine the valid extensions for the given builder type
        var validResourceExtensions = _resourceExtensions.Cast<object>()
            .Select(e => e.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < extensions.Count; i++)
        {
            var item = extensions[i];

           if (validResourceExtensions.Cast<object>().Any(e => string.Equals(e.ToString(), item, StringComparison.OrdinalIgnoreCase)))
            {
                var matchedResourceExtension = Enum.Parse(typeof(ResourceExtensionEnum), item, ignoreCase: true);

                if (!_descriptors.TryGetValue((ResourceExtensionEnum)matchedResourceExtension , out var descriptor))
                    throw new InvalidOperationException(
                        $"Critical: {typeof(ResourceExtensionEnum).Name} type not found: {matchedResourceExtension} to initialise exporter");


                AddResourceExtension(builder, descriptor, logger);
            }
            else 
            {
                // Throw an exception on an unknown exporter type
                throw new InvalidOperationException($"Unsupported Resource Extension type: {item}. Please check your SimpleOpenTelemetry Configuration.");
            }
        }
    }
    

    private void AddResourceExtension(
    ResourceBuilder builder,
    ResourceExtensionDescriptor descriptor,
    ILogger? logger = null)
    {
       
        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension(builder, assembly, descriptor, logger);
    }


    private void TryInvokeExtension(
        ResourceBuilder builder,
        Assembly assembly,
        ResourceExtensionDescriptor descriptor,
        ILogger? logger)
    {
        var (assemblyName, typeName, methodName) = descriptor;
        var builderType = typeof(ResourceBuilder);
        var builderTypeName = builderType.GetType().Name;

        try
        {
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

            descriptor.MethodNames.ToList().ForEach(methodName =>
            {
                var parameterlessMethod = _assemblyExec.FindParameterlessMethod(type, builderType, methodName);

                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

                logger?.LogInformation("Successfully registered {TBuilder} Resource Extension: {Method}", builderTypeName, methodName);
            });

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel Resource Extension via {typeName}.{methodName}", ex);
        }
    }

}
