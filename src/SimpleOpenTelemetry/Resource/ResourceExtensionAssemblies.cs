namespace SimpleOpenTelemetry.Resource;

internal record ResourceExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string[] MethodNames
);

public enum ResourceExtensionEnum
{
    /* otel-dotnet-contrib */
    Azure,
    AWS
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
                new string[] {"AddAzureAppServiceDetector", "AddAzureContainerAppsDetector", "AddAzureVMDetector"}),
             
            [ResourceExtensionEnum.AWS] = new(
                "OpenTelemetry.Resources.AWS",
                "OpenTelemetry.Resources.AWSResourceBuilderExtensions",
                new string[] {"AddAWSEBSDetector", "AddAWSEC2Detector", "AddAWSECSDetector", "AddAWSEKSDetector"}),
        };

}

