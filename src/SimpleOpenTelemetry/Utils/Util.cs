namespace SimpleOpenTelemetry.Utils;

public static class Util
{
    public static string GetSignalName<TBuilder>() => typeof(TBuilder).Name switch
        {
            "TracerProviderBuilder" => "trace",
            "MeterProviderBuilder" => "metric",
            "LoggerProviderBuilder" => "log",
            _ => "Unknown"
        };

}