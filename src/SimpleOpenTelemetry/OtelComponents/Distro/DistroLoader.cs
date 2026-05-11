using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Distro;

/// <summary>
/// Load distro based on the available types linked to DistroEnum
/// </summary>
internal class DistroLoader : IDistroLoader
{
    private readonly string eventCategory = nameof(DistroLoader);
    private readonly IAssemblyExecution _assemblyExec;

    private readonly Dictionary<DistroEnum, DistroDescriptor> _descriptors = DistroAssemblies.KnownDistros;

    /// <summary>
    /// Initializes a new instance of the DistroLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public DistroLoader(IAssemblyExecution assemblyExecution)
    {
        _assemblyExec = assemblyExecution;
    }

    /// <summary>
    /// Loads an opentelemetry distro. Returns false if none set
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns>If a distro to load was specified in config</returns>
    public bool LoadDistro(IOpenTelemetryBuilder builder,
        SimpleOpenTelemetryOptions options)
    {
        var distro = options.Distro;

        if (!string.IsNullOrWhiteSpace(distro))
        {
            if (LoaderEnumHelper.TryParseKnown<DistroEnum>(distro, out var matchedDistro))
            {
                if (!_descriptors.TryGetValue(matchedDistro, out var descriptor))
                {
                    EventSource.Log.Error(eventCategory,
                        $"{typeof(DistroEnum).Name} type '{matchedDistro}' not found to initialise distro.");
                }
                else
                {
                    TryInvokeExtension(matchedDistro, (OpenTelemetryBuilder) builder, descriptor);
                }
            }
            else
            {
                EventSource.Log.Error(eventCategory, $"Unsupported OpenTelemetry Distro '{distro}'. Please check your SimpleOpenTelemetry configuration.");
            }
            return true;

        }
        return false;
    }

    private void TryInvokeExtension(
        DistroEnum distroEnum,
        OpenTelemetryBuilder builder,
        DistroDescriptor descriptor)
    {

        var (assemblyName, typeName, methodName ) = descriptor;

        try
        {
            ReflectiveLoaderExecutor.InvokeBuilderExtension(
                _assemblyExec,
                builder,
                assemblyName,
                typeName,
                methodName,
                null,
                null,
                "distro");

            EventSource.Log.Verbose(eventCategory, $"Registered OpenTelemetry distro '{distroEnum}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register OpenTelemetry distro '{distroEnum}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }

}
