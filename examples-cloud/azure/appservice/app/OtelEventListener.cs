using System.Diagnostics.Tracing;

public class OtelEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Capture all OpenTelemetry SDK internal events
        if (eventSource.Name.StartsWith("OpenTelemetry-", StringComparison.OrdinalIgnoreCase))
        {
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var message = eventData.Message != null && eventData.Payload?.Count > 0
            ? SafeFormat(eventData.Message, eventData.Payload.ToArray())
            : eventData.Message ?? eventData.EventName;

        Console.WriteLine($"[OTel/{eventData.Level}] [{eventData.EventSource.Name}] {message}");
    }

     
    private string SafeFormat(string message, object[] args)
    {
        if (args == null || args.Length == 0)
            return message;

        try
        {
            return string.Format(message, args);
        }
        catch (FormatException)
        {
            // Last resort: just join them manually
            return message + " | " + string.Join(", ", args);
        }
    }

}