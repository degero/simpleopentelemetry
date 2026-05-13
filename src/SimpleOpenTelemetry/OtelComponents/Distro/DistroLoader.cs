using OpenTelemetry;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Distro;

/// <summary>
/// Load distro based on the available types linked to DistroEnum
/// </summary>
internal class DistroLoader : LoaderBase, IDistroLoader
{
    protected override string ComponentKind => "Distro";

    /// <summary>
    /// Initializes a new instance of the DistroLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public DistroLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution) {}

    /// <summary>
    /// Loads an opentelemetry distro. Returns false if none set
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns>If a distro to load was specified in config</returns>
    public bool LoadDistro(IOpenTelemetryBuilder builder, SimpleOpenTelemetryOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Distro)) 
        {
            if (builder is not OpenTelemetryBuilder)
            {
                EventSource.Log.Error(ComponentKind, $"Unsupported OpenTelemetry Distro '{options.Distro}'. This Distro can not be used with OpenTelemetrySDKBuilder.");
                return true; // found a distro but it cannot be used, this will skip any opentelemetry setup.
            }
            TryInvokeComponent(options.Distro, (OpenTelemetryBuilder) builder, DistroAssemblies.KnownGenericHostDistros);
            return true; // return true regardless of distro invocation success as we wish to skip any opentelemetry setup
        }
        return false;
    }
}
