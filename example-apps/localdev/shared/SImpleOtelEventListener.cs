using System.Diagnostics.Tracing;

namespace SimpleOpenTelemetry.Examples.Shared;

public class SimpleOtelEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Capture all SimpleOpenTelemetry internal events
        if (eventSource.Name.StartsWith("SimpleOpenTelemetry-", StringComparison.OrdinalIgnoreCase))
        {
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var message = eventData.Message != null && eventData.Payload?.Count > 0
            ? Utils.SafeFormat(eventData.Message, eventData.Payload?.ToArray())
            : eventData.Message ?? eventData.EventName;

        Console.WriteLine($"[S-Otel/{eventData.Level}] [{eventData.EventSource.Name}] {message}");
    }
}
