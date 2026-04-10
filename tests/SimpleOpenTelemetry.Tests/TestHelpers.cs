using System.Diagnostics.Tracing;

namespace SimpleOpenTelemetryTests;
internal sealed class TestEventListener : EventListener
{
    private readonly string _eventSourceName;
    private readonly List<EventWrittenEventArgs> _events = new();
    private readonly object _lock = new();

    public IReadOnlyList<EventWrittenEventArgs> Events
    {
        get { lock (_lock) return _events.ToList(); }
    }

    public TestEventListener(string eventSourceName)
    {
       _eventSourceName = eventSourceName;
        // Trigger OnEventSourceCreated for already-existing sources
        // (EventSource may already exist as a static singleton)
        foreach (var source in EventSource.GetSources())
            OnEventSourceCreated(source);
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == _eventSourceName)
            EnableEvents(eventSource, EventLevel.Verbose);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        lock (_lock)
            _events.Add(eventData);
    }
}