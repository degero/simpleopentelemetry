using Amazon.Runtime.Telemetry.Metrics;
using Amazon.Runtime.Telemetry.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
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

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleOpenTelemetry(config);

        using var provider = services.BuildServiceProvider();

        // Assert: TracerProvider should not be registered when no Trace config exists
        var tracerProvider = provider.GetService<TracerProvider>();
        Assert.Null(tracerProvider);
    }

    [Fact]
    public void Configure_DoesNotSetupMetrics_WhenMetricConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleOpenTelemetry(config);

        using var provider = services.BuildServiceProvider();

        // Assert: MeterProvider should not be registered when no Metric config exists
        var meterProvider = provider.GetService<MeterProvider>();
        Assert.Null(meterProvider);
    }

    [Fact]
    public void Configure_DoesNotSetupLogging_WhenLogConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleOpenTelemetry(config);

        using var provider = services.BuildServiceProvider();

        // Assert: LoggerProvider should not be registered when no Log config exists
        var loggerProvider = provider.GetService<LoggerProvider>();
        Assert.Null(loggerProvider);
    }
}
