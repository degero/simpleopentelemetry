namespace SimpleOpenTelemetry.Resource;

internal record ResourceDetectorDescriptor(
     string AssemblyName,
     string TypeName,
     string[] MethodNames,
     string? ConfigurationSection
);

public enum ResourceDetectorEnum
{
    /* opentelemetry-dotnet-contrib */
    Host, 

    Container,

    OS,

    Process,

    ProcessRuntime,

    /* opentelemetry-dotnet-contrib platform specific */
    Azure,
    AWS
    // GCP - Still in unreleased Development state
}

/// <summary>
/// A list of known opentelemetry-dotnet-contrib and vendor resourcebuilder extensions
/// </summary>
internal static class ResourceDetectorAssemblies
{
    public static readonly Dictionary<ResourceDetectorEnum, ResourceDetectorDescriptor>
       
        KnownResourceDetectors = new()
        {
            /* opentelemetry-dotnet-contrib */
            [ResourceDetectorEnum.Host] = new(
                "OpenTelemetry.Resources.Host",
                "OpenTelemetry.Resources.HostResourceBuilderExtensions",
                new string[] {"AddHostDetector"},
                null),

            [ResourceDetectorEnum.Container] = new(
                "OpenTelemetry.Resources.Container",
                "OpenTelemetry.Resources.ContainerResourceBuilderExtensions",
                new string[] {"AddContainerDetector"},
                null),

            [ResourceDetectorEnum.OS] = new(
                "OpenTelemetry.Resources.OperatingSystem",
                "OpenTelemetry.Resources.OperatingSystemResourceBuilderExtensions",
                new string[] {"AddOperatingSystemDetector"},
                null),

            [ResourceDetectorEnum.Process] = new(
                "OpenTelemetry.Resources.Process",
                "OpenTelemetry.Resources.ProcessResourceBuilderExtensions",
                new string[] {"AddProcessDetector"},
                null),

            [ResourceDetectorEnum.ProcessRuntime] = new(
                "OpenTelemetry.Resources.ProcessRuntime",
                "OpenTelemetry.Resources.ProcessRuntimeResourceBuilderExtensions",
                new string[] {"AddProcessRuntimeDetector"},
                null),
                

            /* opentelemetry-dotnet-contrib platform specific */
            [ResourceDetectorEnum.Azure] = new(
                "OpenTelemetry.Resources.Azure",
                "OpenTelemetry.Resources.AzureResourceBuilderExtensions",
                new string[] {"AddAzureAppServiceDetector", "AddAzureContainerAppsDetector", "AddAzureVMDetector"},
                null),
             
            [ResourceDetectorEnum.AWS] = new(
                "OpenTelemetry.Resources.AWS",
                "OpenTelemetry.Resources.AWSResourceBuilderExtensions",
                new string[] {"AddAWSEBSDetector", "AddAWSEC2Detector", "AddAWSECSDetector", "AddAWSEKSDetector"},
                "SimpleOpenTelemetry:ResourceDetectorConfig:AWS")
        
            // GCP - Still in Development 
            // TODO add when available
            // [ResourceExtensionEnum.GCP] = new(
            //     "OpenTelemetry.Resources.Gcp",
            //     "OpenTelemetry.Resources.GcpResourceBuilderExtensions",
            //     new string[] {"AddGcpDetector"}),
        };
}

