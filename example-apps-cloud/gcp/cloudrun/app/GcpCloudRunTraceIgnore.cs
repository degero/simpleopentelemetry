using System.Diagnostics;
using System.Collections.ObjectModel;

/// <summary>
/// For use when hosting app on GCP Cloud Run
/// Ignore all inbound traceparent (because of cloudrun generating traces)
/// Not ideal as it drops everything (eg if there are other apps / client side calls tracing parent is lost)
/// </summary>
public class IgnoreInboundContextPropagator : DistributedContextPropagator
{
    private readonly DistributedContextPropagator _default = CreateDefaultPropagator();

    public override IReadOnlyCollection<string> Fields { get; } =
        new ReadOnlyCollection<string>(new[] { "traceparent", "tracestate" });

    // Never extract an incoming parent — forces every request to start a new root Activity
    public override void ExtractTraceIdAndState(
        object? carrier, PropagatorGetterCallback? getter,
        out string? traceId, out string? traceState)
    {
        traceId = null;
        traceState = null;
    }

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(
        object? carrier, PropagatorGetterCallback? getter) => null;

    // Keep normal outbound propagation for any HttpClient calls you make downstream
    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
        => _default.Inject(activity, carrier, setter);
}

/// <summary>
/// Uses 'x-traceparent' instead of 'traceparent' as Cloud run is injecting parents automatically
/// Not ideal as other integrations need to use this non-standard traceparent name.
/// eg in client side trace correlation to backend
/// </summary>
public class RenamedHeaderPropagator : DistributedContextPropagator
{
    private readonly DistributedContextPropagator _default = CreateDefaultPropagator();
    private const string CustomTraceParent = "x-traceparent";
    private const string CustomTraceState = "x-tracestate";

    public override IReadOnlyCollection<string> Fields { get; } =
        new ReadOnlyCollection<string>(new[] { CustomTraceParent, CustomTraceState });

    public override void ExtractTraceIdAndState(
        object? carrier, PropagatorGetterCallback? getter,
        out string? traceId, out string? traceState)
    {
        traceId = null;
        traceState = null;
        if (getter is null) return;

        // Redirect the default's lookups for "traceparent"/"tracestate"
        // to our custom header names, which Cloud Run's GFE never touches.
        void RemappedGetter(object? c, string field, out string? value, out IEnumerable<string>? values)
        {
            var actual = field switch
            {
                "traceparent" => CustomTraceParent,
                "tracestate" => CustomTraceState,
                _ => field
            };
            getter(c, actual, out value, out values);
        }

        _default.ExtractTraceIdAndState(carrier, RemappedGetter, out traceId, out traceState);
    }

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(
        object? carrier, PropagatorGetterCallback? getter) => null;

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
    {
        if (activity is null || setter is null) return;

        void RemappedSetter(object? c, string field, string value)
        {
            var actual = field switch
            {
                "traceparent" => CustomTraceParent,
                "tracestate" => CustomTraceState,
                _ => field
            };
            setter(c, actual, value);
        }

        _default.Inject(activity, carrier, RemappedSetter);
    }
}