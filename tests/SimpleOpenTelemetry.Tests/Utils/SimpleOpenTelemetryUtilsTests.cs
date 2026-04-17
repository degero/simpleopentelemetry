using System.Diagnostics.Tracing;
using System.Diagnostics.Eventing.Reader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Utils;
using Xunit;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;

namespace SimpleOpenTelemetryTests.Utils;

public class SimpleOpenTelemetryUtilsTests
{
    private readonly TestEventListener _listener;

    public SimpleOpenTelemetryUtilsTests()
    {
        _listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
    }

    public void Dispose()
    {
        _listener.Dispose(); // Always dispose — disables the EventSource for this _listener
    }
    
    private static IConfiguration BuildConfigWithOtelValues(string otelServiceName, string otelResourceAttributes) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = otelServiceName,
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] = otelResourceAttributes
            })
            .Build();

    [Fact]
    public void OtelServiceName_ReturnsValuesFromConfiguration()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";

        var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

        Assert.Equal(serviceName, SettingsHelper.OtelServiceName(config));
    }

    [Fact]
    public void OtelResourceAttributes_ReturnsValuesFromConfiguration()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";

        var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

        Assert.Equal(resourceAttributes, SettingsHelper.OtelResourceAttributes(config));
    }

    [Fact]
    public void AddTracingInstrumentation_ThrowsWhenInstrumentationAssemblyMissing()
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

        Assert.Contains(_listener.Events, r => r.EventId == 3 && 
            r.Level == System.Diagnostics.Tracing.EventLevel.Error &&
            r.Payload.Any(r => r.ToString().Contains("Cannot load assembly")));
    }

    [Fact]
    public void AddMetricsInstrumentation_ThrowsWhenInstrumentationAssemblyMissing()
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

        Assert.Contains(_listener.Events, r => r.EventId == 3 && 
            r.Level == System.Diagnostics.Tracing.EventLevel.Error &&
            r.Payload.Any(r => r.ToString().Contains("Cannot load assembly")));
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

    [Fact]
    public void AddTracingInstrumentation_WrapsTypeLookupErrors()
    {
        var previousDescriptor =
            InstrumentationAssemblies.KnownTraceInstrumentations[TraceInstrumentationEnum.AspNetCore];

        InstrumentationAssemblies.KnownTraceInstrumentations[TraceInstrumentationEnum.AspNetCore] =
            new InstrumentationExtensionDescriptor(
                "System.Runtime",
                "Does.Not.Exist.Type",
                "AddAspNetCoreInstrumentation",
                null);

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = "test-service",
                    [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] =
                        "service.version=1.2.3,deployment.environment.name=dev",
                    ["SimpleOpenTelemetry:Trace:Instrumentations:0"] =
                        nameof(TraceInstrumentationEnum.AspNetCore)
                })
                .Build();

            var services = new ServiceCollection();
             services.AddSimpleOpenTelemetry(config);

            Assert.Contains(_listener.Events, r => r.EventId == 3 && 
                r.Level == System.Diagnostics.Tracing.EventLevel.Error &&
                r.Payload.Any(r => r.ToString().Contains("Failed to register trace instrumentation")));
        }
        finally
        {
            InstrumentationAssemblies.KnownTraceInstrumentations[TraceInstrumentationEnum.AspNetCore] =
                previousDescriptor;
        }
    }
}

