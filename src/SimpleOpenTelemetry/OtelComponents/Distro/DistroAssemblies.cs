
using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Distro;

internal static class DistroAssemblies
{
    public static readonly Dictionary<DistroEnum, AssemblyDescriptor>
        KnownDistros = new()
        {
            [DistroEnum.AzureMonitorAspNetCore] = new(
                "Azure.Monitor.OpenTelemetry.AspNetCore",
                "Azure.Monitor.OpenTelemetry.AspNetCore.OpenTelemetryBuilderExtensions",
                "UseAzureMonitor")
        };
}