namespace SimpleOpenTelemetry.OtelComponents.Common;

/// <summary>
/// Complex types supported on Otel component options
/// </summary>
internal static class ComponentOptionsTypes
{
    private const string AzureIdentityAssembly = "Azure.Identity";

    public static readonly Dictionary<string, AssemblyDescriptor> SupportedTypes = new()
    {
        ["Azure.Identity.DefaultAzureCredential"] = new(
            AzureIdentityAssembly, "Azure.Identity.DefaultAzureCredential")
    };
}
