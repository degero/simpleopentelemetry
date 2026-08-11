using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Resource;

/// <summary>
/// A list of known opentelemetry-dotnet-contrib and vendor resourcebuilder extensions
/// </summary>
internal static class ResourceDetectorAssemblies
{
    public static readonly Dictionary<ResourceDetectorEnum, AssemblyDescriptor>

        KnownResourceDetectors = new()
        {
            /* SimpleOpenTelemetry built-in */
            [ResourceDetectorEnum.AssemblyVersion] = new(
                "SimpleOpenTelemetry",
                "SimpleOpenTelemetry.Extensions.ResourceBuilderExtensions",
                ["AddAssemblyVersionDetector"]),

            /* opentelemetry-dotnet-contrib */
            [ResourceDetectorEnum.EnvVar] = new(
                "OpenTelemetry",
                "OpenTelemetry.Resources.ResourceBuilderExtensions",
                ["AddEnvironmentVariableDetector"]),

            [ResourceDetectorEnum.Host] = new(
                "OpenTelemetry.Resources.Host",
                "OpenTelemetry.Resources.HostResourceBuilderExtensions",
                ["AddHostDetector"]),

            [ResourceDetectorEnum.Container] = new(
                "OpenTelemetry.Resources.Container",
                "OpenTelemetry.Resources.ContainerResourceBuilderExtensions",
                ["AddContainerDetector"]),

            [ResourceDetectorEnum.OS] = new(
                "OpenTelemetry.Resources.OperatingSystem",
                "OpenTelemetry.Resources.OperatingSystemResourceBuilderExtensions",
                ["AddOperatingSystemDetector"]),

            [ResourceDetectorEnum.Process] = new(
                "OpenTelemetry.Resources.Process",
                "OpenTelemetry.Resources.ProcessResourceBuilderExtensions",
                ["AddProcessDetector"]),

            [ResourceDetectorEnum.ProcessRuntime] = new(
                "OpenTelemetry.Resources.ProcessRuntime",
                "OpenTelemetry.Resources.ProcessRuntimeResourceBuilderExtensions",
                ["AddProcessRuntimeDetector"]),


            /* opentelemetry-dotnet-contrib platform specific */
            [ResourceDetectorEnum.Azure] = new(
                "OpenTelemetry.Resources.Azure",
                "OpenTelemetry.Resources.AzureResourceBuilderExtensions",
                ["AddAzureAppServiceDetector", "AddAzureContainerAppsDetector", "AddAzureVMDetector"]),

            [ResourceDetectorEnum.AWS] = new(
                "OpenTelemetry.Resources.AWS",
                "OpenTelemetry.Resources.AWSResourceBuilderExtensions",
                ["AddAWSEBSDetector", "AddAWSEC2Detector", "AddAWSECSDetector", "AddAWSEKSDetector"],
                "AWSResourceBuilderOptions"),

            [ResourceDetectorEnum.GCP] = new(
                "OpenTelemetry.Resources.Gcp",
                "OpenTelemetry.Resources.GcpResourceBuilderExtensions",
                ["AddGcpDetector"]),
        };
}

