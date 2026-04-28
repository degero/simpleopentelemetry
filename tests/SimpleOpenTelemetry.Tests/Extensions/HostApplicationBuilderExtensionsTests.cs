
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

public class HostApplicationBuilderExtensionsTests
{
    private readonly TestEventListener _listener;

    public HostApplicationBuilderExtensionsTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose(); // Always dispose — disables the EventSource for this _listener
    }

    [Fact] // TODO Chad reinstate with eventlogging only option as this throws before app is built
    public void AddSimpleOpenTelemetry_ThrowsWhenSimpleOpenTelemetryConfigSignalSubSections_AreUndefined()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                ["SimpleOpenTelemetry"] = "{}"
            })
            .Build();

        var services = new ServiceCollection();

        // Act/assert
        Assert.ThrowsAny<Exception>(() => services.AddSimpleOpenTelemetry(config)); // Config section missing - no providers are created
    }
    
    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsWhen_AddSimpleOpenTelemetry_NotCalled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.SimpleOpenTelemetryValidate());
        Assert.Contains("OpenTelemetry has not been registered", exception.Message);
    }

    // TODO chad cleanup gentests
    [Fact]
    public void AddTracingInstrumentation_LogsExpectedResult_WhenInstrumentationAssemblyIsPresentOrMissing()
    {
        // Trigger the loader via the public builder entrypoint so we don't need to instantiate
        // internal/abstract OpenTelemetry builder types.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SimpleOpenTelemetry:Trace:Instrumentations:0"] =
                    nameof(TraceInstrumentationEnum.AspNetCore)
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSimpleOpenTelemetry(config);

        Assert.Contains(_listener.Events, r =>
            (r.EventId == 3 &&
             r.Level == EventLevel.Error &&
             r.Payload.Any(p => p?.ToString()?.Contains("Cannot load assembly") ?? false))
            ||
            (r.EventId == 4 &&
             r.Level == EventLevel.Verbose &&
             r.Payload.Any(p => p?.ToString()?.Contains("Registered trace instrumentation") ?? false)));
    }

    [Fact]
    public void AddMetricsInstrumentation_LogsExpectedResult_WhenInstrumentationAssemblyIsPresentOrMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SimpleOpenTelemetry:Metric:Instrumentations:0"] =
                    nameof(MetricInstrumentationEnum.AspNetCore)
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSimpleOpenTelemetry(config);

        Assert.Contains(_listener.Events, r =>
            (r.EventId == 3 &&
             r.Level == EventLevel.Error &&
             r.Payload.Any(p => p?.ToString()?.Contains("Cannot load assembly") ?? false))
            ||
            (r.EventId == 4 &&
             r.Level == EventLevel.Verbose &&
             r.Payload.Any(p => p?.ToString()?.Contains("Registered metric instrumentation") ?? false)));
    }

    [Fact]
    public void AddTracingInstrumentation_ThrowsForInvalidEnumValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = "test-service",
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] =
                    "service.version=1.2.3,deployment.environment.name=dev",
                ["SimpleOpenTelemetry:Trace:Instrumentations:0"] = "999"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSimpleOpenTelemetry(config);
        Assert.Contains(_listener.Events, r => r.EventId == 3 && 
            r.Level == System.Diagnostics.Tracing.EventLevel.Error &&
            r.Payload.Any(r => r.ToString().Contains("type '999' not found ")));
    }
}