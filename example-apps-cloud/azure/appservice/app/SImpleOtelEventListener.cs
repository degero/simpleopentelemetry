using System.Diagnostics.Tracing;

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
            ? SafeFormat(eventData.Message, eventData.Payload.ToArray())
            : eventData.Message ?? eventData.EventName;
        
        Console.WriteLine($"[S-Otel/{eventData.Level}] [{eventData.EventSource.Name}] {message}");
    }

    
     
    private string SafeFormat(string message, object[] args)
    {
        if (args == null || args.Length == 0)
            return message;

        // Escape stray closing braces that aren't part of a {N} placeholder
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(?<!\{)\}(?!\})",   // a } NOT preceded by { and NOT followed by }
            "}}");

        try
        {
            return string.Format(sanitized, args);
        }
        catch (FormatException)
        {
            // Last resort: just join them manually
            return message + " | " + string.Join(", ", args);
        }
    }
}