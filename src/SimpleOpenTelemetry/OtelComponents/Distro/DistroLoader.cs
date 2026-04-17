using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Distro;

internal interface IDistroLoader
{
    bool LoadDistro(IOpenTelemetryBuilder builder, SimpleOpenTelemetryOptions options);
}

/// <summary>
/// Load distro based on the available types linked to DistroEnum
/// </summary>
internal class DistroLoader : IDistroLoader
{
    private readonly string eventCategory = nameof(DistroLoader);
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;
    private readonly Array _distros = Enum.GetValues<DistroEnum>();

    private readonly Dictionary<DistroEnum, DistroDescriptor> _descriptors = DistroAssemblies.KnownDistros;

    /// <summary>
    /// Initializes a new instance of the DistroLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public DistroLoader(IConfiguration configuration)
    {
        _configuration = configuration;
        _assemblyExec = new AssemblyExecution();
    }

    public bool LoadDistro(IOpenTelemetryBuilder builder,
        SimpleOpenTelemetryOptions options)
    {
        var distro = options.Distro;

        if (!string.IsNullOrWhiteSpace(distro))
        {
            var validDistros = _distros.Cast<object>()
                .Select(e => e.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (validDistros.Cast<object>().Any(e => string.Equals(e.ToString(), distro, StringComparison.OrdinalIgnoreCase)))
            {
                var matchedDistro = (DistroEnum) Enum.Parse(typeof(DistroEnum), distro, ignoreCase: true);

                if (!_descriptors.TryGetValue(matchedDistro, out var descriptor))
                {
                    EventSource.Log.Error(eventCategory,
                        $"{typeof(DistroEnum).Name} type '{matchedDistro}' not found to initialise distro.");
                    return false;
                }
                else
                {
                    TryInvokeExtension(matchedDistro, builder as OpenTelemetryBuilder, descriptor);
                    return true;
                }
            }
        }
        return false;
    }

    private void TryInvokeExtension(
        DistroEnum distroEnum,
        OpenTelemetryBuilder builder,
        DistroDescriptor descriptor)
    {

        var (assemblyName, typeName, methodName, configurationSection) = descriptor;

        try
        {
            var assembly = _assemblyExec.GetAssembly(assemblyName);
            var builderType = typeof(OpenTelemetryBuilder);
            var builderTypeName = builder.GetType().Name;

            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

            var parameterlessMethod = _assemblyExec.FindParameterlessMethod(type, builderType, descriptor.MethodName);
            var actionMethod = _assemblyExec.FindActionOverload(type, builderType, descriptor.MethodName);

            // attempt Action<TOptions> path only when section exists in config
            if (descriptor.ConfigurationSection is not null &&
                actionMethod is not null &&
                parameterlessMethod is null)
            {
                throw new InvalidOperationException( // TODO chad add tests around these scenarios
                    $"Failed registration {builderTypeName} distro: '{methodName}'. " +
                    $"A configuration section '{configurationSection}' is required but not found in config file.");
            }

            var section = descriptor.ConfigurationSection is not null ? _configuration.GetSection(descriptor.ConfigurationSection) : null;

            if (section is not null && section.Exists())
                _assemblyExec.InvokeWithAction(actionMethod, builder, section);
            else
                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

            EventSource.Log.Verbose(eventCategory, $"registered distro '{distroEnum}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register distro '{distroEnum}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }

}
