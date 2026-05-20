using System.Diagnostics.Tracing;

namespace SimpleOpenTelemetry.Diagnostics;

/// <summary>
/// Diagnostics for SimpleOpenTelemetry are emitted via EventSource with IDs like a 
/// logging platform for simplicity
/// To listen, enable the source named "SimpleOpenTelemetry-Core".
///
/// Event IDs:
///   1 = Critical - payload[2] is exception detail (optional)
///   2 = Error    — payload[2] is exception detail (optional)
///   3 = Warn    
///   4 = Info    
///   5 = Verbose
///
/// Example with dotnet-trace and published app assembly:
///   dotnet-trace collect --providers SimpleOpenTelemetry-Core "SimpleOpenTelemetry-Core:0xFFFFFFFF:5" -- dotnet .\AspNetCore.dll
/// </summary>
[EventSource(Name = EventSourceName)]
public sealed class SimpleOpenTelemetryEventSource : EventSource
{
    /// <summary>
    /// 
    /// </summary>
    public const string EventSourceName = "SimpleOpenTelemetry-Core";

    /// <summary>
    /// 
    /// </summary>
    public static readonly SimpleOpenTelemetryEventSource Log = new();

    private const int CriticalId    = 1;
    private const int ErrorId       = 2;
    private const int WarnId        = 3;
    private const int InfoId        = 4;
    private const int VerboseId     = 5;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="category"></param>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    [NonEvent]
    public void CriticalEvent(string category, string message, Exception? ex = null)
    {
        if (IsEnabled(EventLevel.Critical, EventKeywords.All))
            Critical(category, message, ex?.ToString() ?? "none");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="category"></param>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    [NonEvent]
    public void ErrorEvent(string category, string message, Exception? ex = null)
    {
        if (IsEnabled(EventLevel.Error, EventKeywords.All))
            Error(category, message, ex?.ToString() ?? "none");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="category"></param>
    /// <param name="message"></param>
    [NonEvent]
    public void WarnEvent(string category, string message)
    {
        if (IsEnabled(EventLevel.Warning, EventKeywords.All))
            Warn(category, message);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="category"></param>
    /// <param name="message"></param>
    [NonEvent]
    public void InfoEvent(string category, string message)
    {
        if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            Info(category, message);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="category"></param>
    /// <param name="message"></param>
    [NonEvent]
    public void VerboseEvent(string category, string message)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
            Verbose(category, message);
    }

    [Event(CriticalId, Level = EventLevel.Critical, Message = "[Critical] {0}: {1} | {2}")]
    private void Critical(string category, string message, string ex)
        => WriteEvent(CriticalId, category, message, ex);

    [Event(ErrorId, Level = EventLevel.Error, Message = "[Error] {0}: {1} | {2}")]
    private void Error(string category, string message, string ex)
        => WriteEvent(ErrorId, category, message, ex);

    [Event(WarnId, Level = EventLevel.Warning, Message = "[Warn] {0}: {1}")]
    private void Warn(string category, string message)
        => WriteEvent(WarnId, category, message);

    [Event(InfoId, Level = EventLevel.Informational, Message = "[Info] {0}: {1}")]
    private void Info(string category, string message)
        => WriteEvent(InfoId, category, message);

    [Event(VerboseId, Level = EventLevel.Verbose, Message = "[Verbose] {0}: {1}")]
    private void Verbose(string category, string message)
        => WriteEvent(VerboseId, category, message);
}