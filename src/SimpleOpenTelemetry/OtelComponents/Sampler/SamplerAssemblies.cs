namespace SimpleOpenTelemetry.OtelComponents.Sampler;

internal record SamplerDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName
);


/// <summary>
/// A list of known opentelemetry-contrib and vendor samplers
/// </summary>
internal static class SamplerAssemblies
{
    public static readonly Dictionary<SamplerEnum, SamplerDescriptor>
        KnownSamplers = new()
        {
            /* Contrib samplers */ 
            // Disabled until this lib is corrected inline with Otels fluent builder 
            // (ie not needing a prebuilt resourceprovider)
            // [SamplerEnum.AWS] = new(
            //     "OpenTelemetry.Sampler.AWS",
            //     "OpenTelemetry.Sampler.AWS.AWSXRayRemoteSampler",
            //     "Builder")
        };

}

