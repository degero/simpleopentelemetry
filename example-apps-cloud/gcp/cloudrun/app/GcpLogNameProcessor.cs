using OpenTelemetry;
using OpenTelemetry.Logs;

namespace soteltestgcp;

/// <summary>
/// For use when using OTLP Export. Set log name to send logs in GCP under the prefix 'projects/--projname--/logs/--logname--
/// </summary>
/// <param name="logName"></param>
public sealed class GcpLogNameProcessor(string logName) : BaseProcessor<LogRecord>
{

    public override void OnEnd(LogRecord logRecord)
    {
        var attributes = new List<KeyValuePair<string, object?>>(
            logRecord.Attributes ?? Array.Empty<KeyValuePair<string, object?>>())
        {
            new("gcp.log_name", logName)
        };

        logRecord.Attributes = attributes;
    }
}