
using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Distro;

internal static class DistroAssemblies
{
    /// <summary>
    /// These require a concrete OpenTelemetryBuilder only in a Generic Host Host builder setup (eg AddSimpleOpenTelemetry())
    /// </summary>
    public static readonly Dictionary<DistroEnum, AssemblyDescriptor>
        KnownGenericHostDistros = new()
        {
            [DistroEnum.AzureMonitorAspNetCore] = new(
                "Azure.Monitor.OpenTelemetry.AspNetCore",
                "Azure.Monitor.OpenTelemetry.AspNetCore.OpenTelemetryBuilderExtensions",
                "UseAzureMonitor")
        };
}