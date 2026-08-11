namespace SimpleOpenTelemetry.OtelComponents.Resource;

/// <summary>
/// Enumeration of supported resource detectors.
/// </summary>
public enum ResourceDetectorEnum
{
    /* SimpleOpenTelemetry built-in */
    /// <summary>
    /// Assembly version resource detector.
    /// </summary>
    AssemblyVersion,

    /* opentelemetry-dotnet-contrib */
    /// <summary>
    /// Environment variable resource detector.
    /// </summary>
    EnvVar,
    /// <summary>
    /// Host resource detector.
    /// </summary>
    Host,
    /// <summary>
    /// Container resource detector.
    /// </summary>
    Container,
    /// <summary>
    /// Operating system resource detector.
    /// </summary>
    OS,
    /// <summary>
    /// Process resource detector.
    /// </summary>
    Process,
    /// <summary>
    /// Process runtime resource detector.
    /// </summary>
    ProcessRuntime,

    /* opentelemetry-dotnet-contrib platform specific */
    /// <summary>
    /// Azure resource detector.
    /// </summary>
    Azure,
    /// <summary>
    /// AWS resource detector.
    /// </summary>
    AWS,
    /// <summary>
    /// Google Cloud Platform resource detector.
    /// </summary>
    GCP
}
