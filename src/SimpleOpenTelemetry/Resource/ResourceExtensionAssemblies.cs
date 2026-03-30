namespace SimpleOpenTelemetry.Resource;

internal record ResourceExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string[] MethodNames,
     string? ConfigurationSection
);

public enum ResourceExtensionEnum
{
    /* opentelemetry-dotnet-contrib */
    Azure,
    AWS
    // GCP - Still in Development
}

/// <summary>
/// A list of known opentelemetry-dotnet-contrib and vendor resourcebuilder extensions
/// </summary>
internal static class ResourceExtensionAssemblies
{
    public static readonly Dictionary<ResourceExtensionEnum, ResourceExtensionDescriptor>
       
        KnownResourceExtensions = new()
        {
            /* Contrib detectors */
            [ResourceExtensionEnum.Azure] = new(
                "OpenTelemetry.Resources.Azure",
                "OpenTelemetry.Resources.AzureResourceBuilderExtensions",
                new string[] {"AddAzureAppServiceDetector", "AddAzureContainerAppsDetector", "AddAzureVMDetector"},
                null),
             
            [ResourceExtensionEnum.AWS] = new(
                "OpenTelemetry.Resources.AWS",
                "OpenTelemetry.Resources.AWSResourceBuilderExtensions",
                new string[] {"AddAWSEBSDetector", "AddAWSEC2Detector", "AddAWSECSDetector", "AddAWSEKSDetector"},
                "SimpleOpenTelemetry:ResourceDetectorConfig:AWS")
        
            // GCP - Still in Development
            // [ResourceExtensionEnum.GCP] = new(
            //     "OpenTelemetry.Resources.Gcp",
            //     "OpenTelemetry.Resources.GcpResourceBuilderExtensions",
            //     new string[] {"AddGcpDetector"}),
        };
}

