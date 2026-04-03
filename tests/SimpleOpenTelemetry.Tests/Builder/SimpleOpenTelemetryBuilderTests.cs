using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Extensions;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

public class SimpleOpenTelemetryBuilderTests
{
    [Fact]
    public void Configure_DoesNotSetupTracing_WhenTraceConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        // Assert: TracerProvider should not be registered when no Trace config exists
        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.Null(tracerProvider);
    }

    [Fact]
    public void Configure_DoesNotSetupMetrics_WhenMetricConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        // Assert: MeterProvider should not be registered when no Metric config exists
        var meterProvider = host.Services.GetService<MeterProvider>();
        Assert.Null(meterProvider);
    }

    [Fact]
    public void Configure_DoesNotSetupLogging_WhenLogConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        // Note: HostApplicationBuilder automatically provides LoggerProvider via AddLogging
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        // Assert: LoggerProvider is created but with no OpenTelemetry processors configured
        // (Since SimpleOpenTelemetry Log config is missing)
        var loggerProvider = host.Services.GetService<LoggerProvider>();

        // LoggerProvider exists from Host.AddLogging(), but OTel logging processors not added
        Assert.Null(loggerProvider);
    }

}