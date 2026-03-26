using System;
using Microsoft.Extensions.Configuration;
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
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Options:Endpoint", "http://localhost:4317" },
                { "Options:Protocol", "Grpc" },
                { "Options:Headers", "api-key=test" },
                { "Options:TimeoutMilliseconds", "5000" }
            });
        var root = configBuilder.Build();
        var optionsSection = root.GetSection("Options");
        
        var config = new SimpleOpenTelemetryExporterConfig
        {
            Type = SimpleOpenTelemetryExporterType.Otlp,
            Options = optionsSection
        };

        Assert.Equal(SimpleOpenTelemetryExporterType.Otlp, config.Type);
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

