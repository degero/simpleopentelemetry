using System;
using System.Collections.Generic;

namespace SimpleOpenTelemetryTests;

/// <summary>
/// Temporarily sets OTEL environment variables for a test, then restores previous values.
/// </summary>
internal sealed class OtelEnvironmentScope : IDisposable
{
    private readonly Dictionary<string, string?> _previousValues = new();

    public OtelEnvironmentScope(IEnumerable<KeyValuePair<string, string>> newValues)
    {
        foreach (var kv in newValues)
        {
            _previousValues[kv.Key] = Environment.GetEnvironmentVariable(kv.Key);
            Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }
    }

    public void Dispose()
    {
        foreach (var kv in _previousValues)
            Environment.SetEnvironmentVariable(kv.Key, kv.Value);
    }
}

