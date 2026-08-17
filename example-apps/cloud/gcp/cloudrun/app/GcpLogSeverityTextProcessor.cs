using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace soteltestgcp;

/// <summary>
/// For use when using OTLP Export. Adjust Loglevel format to be compatible with GCP
/// Particularly 'Information' which would be dropped
/// </summary>
public sealed class GcpLogSeverityTextProcessor : BaseProcessor<LogRecord>
{
   private static readonly PropertyInfo? SeverityTextProperty =
        typeof(LogRecord).GetProperty("SeverityText", BindingFlags.Instance | BindingFlags.NonPublic);

    public override void OnEnd(LogRecord logRecord)
    {
        var normalized = logRecord.LogLevel switch
        {
            LogLevel.Trace       => "TRACE",
            LogLevel.Debug       => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning     => "WARN",
            LogLevel.Error       => "ERROR",
            LogLevel.Critical    => "FATAL",
            _                    => null
        };

        SeverityTextProperty?.SetValue(logRecord, normalized);
    }
}