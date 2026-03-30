namespace SimpleOpenTelemetry.Sampler;

internal record SamplerDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName
);

public enum SamplerEnum
{
    /* opentelemetry-dotnet */
    
    /* opentelemetry-dotnet-contrib */
    AWS
}

/// <summary>
/// A list of known opentelemetry-dotnet-contrib and vendor extensions
/// </summary>
internal static class SamplerAssemblies
{
    public static readonly Dictionary<SamplerEnum, SamplerDescriptor>
       
        KnownSamplers = new()
        {
            /* Contrib detectors */
            [SamplerEnum.AWS] = new(
                "OpenTelemetry.Sampler.AWS",
                "OpenTelemetry.Sampler.AWS.AWSXRayRemoteSampler",
                "Builder")
        };

}

