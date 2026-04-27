namespace SimpleOpenTelemetry.OtelComponents.Resource;

public enum ResourceDetectorEnum
{
    /* SimpleOpenTelemetry built-in */
    AssemblyVersion,

    /* opentelemetry-dotnet-contrib */
    EnvVar,
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