
namespace SimpleOpenTelemetry.OtelComponents.Distro;

internal record DistroDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? ConfigurationSection
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