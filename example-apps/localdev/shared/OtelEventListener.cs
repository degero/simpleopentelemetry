using System.Diagnostics.Tracing;

namespace SimpleOpenTelemetry.Examples.Shared;

public class OtelEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Capture all OpenTelemetry SDK internal events
        if (eventSource.Name.StartsWith("OpenTelemetry-", StringComparison.OrdinalIgnoreCase))
        {
            EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var message = eventData.Message != null && eventData.Payload?.Count > 0
            ? Utils.SafeFormat(eventData.Message, eventData.Payload?.ToArray())
            : eventData.Message ?? eventData.EventName;

        Console.WriteLine($"[OTel/{eventData.Level}] [{eventData.EventSource.Name}] {message}");
    }
}
