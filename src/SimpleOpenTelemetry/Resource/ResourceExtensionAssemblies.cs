namespace SimpleOpenTelemetry.Resource;

public record ResourceExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string[] MethodNames
);

public enum ResourceExtensionEnum
{
    /* otel-dotnet-contrib */
    Azure
}

/// <summary>
///
/// </summary>
public static class ResourceExtensionAssemblies
{
    public static readonly Dictionary<ResourceExtensionEnum, ResourceExtensionDescriptor>
       
        KnownResourceExtensions = new()
        {
            /* Contrib detectors */
            [ResourceExtensionEnum.Azure] = new(
                "OpenTelemetry.Resources.Azure",
                "OpenTelemetry.Resources.AzureResourceBuilderExtensions",
                new string[] {"AddAzureAppServiceDetector", "AddAzureContainerAppsDetector", "AddAzureVMDetector"}),
        };

}

