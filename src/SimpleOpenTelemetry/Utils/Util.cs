namespace SimpleOpenTelemetry.Utils;

/// <summary>
/// Utility methods for OpenTelemetry operations.
/// </summary>
public static class Util
{
    /// <summary>
    /// Gets the signal name for a given builder type.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <returns>The signal name (trace, metric, or log).</returns>
    public static string GetSignalName<TBuilder>() => typeof(TBuilder).Name switch
        {
            "TracerProviderBuilder" => "trace",
            "MeterProviderBuilder" => "metric",
            "LoggerProviderBuilder" => "log",
            _ => "Unknown"
        };

}