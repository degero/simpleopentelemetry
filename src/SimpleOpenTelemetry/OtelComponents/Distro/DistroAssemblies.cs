
namespace SimpleOpenTelemetry.OtelComponents.Distro;

// TODO normalise these and update InvokeBuilderExtension()
internal record DistroDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? ConfigurationSection,
     bool OptionsRequired = false
);

internal static class DistroAssemblies
{
    public static readonly Dictionary<DistroEnum, DistroDescriptor>
        KnownDistros = new()
        {
            [DistroEnum.AzureMonitorAspNetCore] = new(
                "Azure.Monitor.OpenTelemetry.AspNetCore",
                "Azure.Monitor.OpenTelemetry.AspNetCore.OpenTelemetryBuilderExtensions",
                "UseAzureMonitor",
                null)
        };
}