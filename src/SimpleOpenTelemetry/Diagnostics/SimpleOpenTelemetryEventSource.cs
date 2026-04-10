using System.Diagnostics.Tracing;

namespace SimpleOpenTelemetry.Diagnostics;

/// <summary>
/// Diagnostics for MyLibrary are emitted via EventSource.
/// To listen, enable the source named "MyCompany-MyLibrary".
///
/// Event IDs:
///   1 = Info    (Informational)
///   2 = Warn    (Warning)
///   3 = Error   (Error)    — payload[2] is exception detail
///   4 = Debug   (Verbose)
///
/// Example with dotnet-trace:
///   dotnet-trace collect --providers SimpleOpenTelemetry-Core
/// </summary>
[EventSource(Name = EventSourceName)]
internal sealed class SimpleOpenTelemetryEventSource : EventSource
{
    internal const string EventSourceName = "SimpleOpenTelemetry-Core";
    public static readonly SimpleOpenTelemetryEventSource Log = new();

    [Event(1, Level = EventLevel.Informational, Message = "{0}: {1}")]
    public void Info(string category, string message)
    {
        if (IsEnabled()) WriteEvent(1, category, message);
    }

    [Event(2, Level = EventLevel.Warning, Message = "{0}: {1}")]
    public void Warn(string category, string message)
    {
        if (IsEnabled()) WriteEvent(2, category, message);
    }

    [Event(3, Level = EventLevel.Error, Message = "{0}: {1} | Exception: {2}")]
    public void Error(string category, string message, string? exceptionDetails = null)
    {
        if (IsEnabled()) WriteEvent(3, category, message, exceptionDetails ?? "none");
    }

    [Event(4, Level = EventLevel.Verbose, Message = "{0}: {1}")]
    public void Verbose(string category, string message)
    {
        if (IsEnabled()) WriteEvent(4, category, message);
    }
}