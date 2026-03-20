using System;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Instrumentation;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

public class SimpleOpenTelemetryBuilderOptionsTests
{
    [Fact]
    public void SimpleOpenTelemetryExportersOptions_HasEmptyListsByDefault()
    {
        var options = new SimpleOpenTelemetryExportersOptions();

        Assert.NotNull(options.Tracing);
        Assert.NotNull(options.Logging);
        Assert.NotNull(options.Metrics);
        Assert.Empty(options.Tracing);
        Assert.Empty(options.Logging);
        Assert.Empty(options.Metrics);
    }

    [Fact]
    public void SimpleOpenTelemetryExporterConfig_AllowsSettingValues()
    {
        var endpoint = new Uri("http://localhost:4317");
        var config = new SimpleOpenTelemetryExporterConfig
        {
            Type = SimpleOpenTelemetryExporterType.Otlp,
            Endpoint = endpoint,
            Protocol = SimpleOpenTelemetryExporterProtocol.Grpc,
            Headers = "api-key=test",
            TimeoutMilliseconds = 5000
        };

        Assert.Equal(SimpleOpenTelemetryExporterType.Otlp, config.Type);
        Assert.Equal(endpoint, config.Endpoint);
        Assert.Equal(SimpleOpenTelemetryExporterProtocol.Grpc, config.Protocol);
        Assert.Equal("api-key=test", config.Headers);
        Assert.Equal(5000, config.TimeoutMilliseconds);
    }

    [Fact]
    public void SimpleOpenTelemetryBuilderOptions_DefaultsExportersAndAllowsAssignments()
    {
        var options = new SimpleOpenTelemetryBuilderOptions
        {
            TracingInstrumentations = new[] { TracingInstrumentationEnum.AspNetCore },
            MetricsInstrumentations = new[] { MetricsInstrumentationEnum.Runtime },
            CustomMeters = new[] { "custom-meter" },
            TraceSources = new[] { "custom-source" }
        };

        Assert.NotNull(options.Exporters);
        Assert.Single(options.TracingInstrumentations!);
        Assert.Single(options.MetricsInstrumentations!);
        Assert.Single(options.CustomMeters!);
        Assert.Single(options.TraceSources!);
    }
}

