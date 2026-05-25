namespace SimpleOpenTelemetry.OtelComponents.Extension;

/// <summary>
/// Enumeration of supported trace extensions.
/// </summary>
public enum TraceExtensionsEnum
{
    /// <summary>
    /// AWS X-Ray trace ID extension.
    /// </summary>
    AWSXRayTraceId
}

/// <summary>
/// Enumeration of supported metric extensions.
/// </summary>
public enum MetricExtensionsEnum
{
    /// <summary>
    /// No metric extension.
    /// </summary>
    None /* Placeholder for future use */    
}

/// <summary>
/// Enumeration of supported log extensions.
/// </summary>
public enum LogExtensionsEnum
{
    /// <summary>
    /// No log extension.
    /// </summary>
    None /* Placeholder for future use */    
}


/// <summary>
/// Enumeration of supported OpenTelemetryBuilder extensions.
/// </summary>
public enum BuilderExtensionsEnum
{
    AzureMonitorExporter
}
