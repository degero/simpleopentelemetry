using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Extensions;
using Xunit;

namespace SimpleOpenTelemetryIntegrationTests;


[CollectionDefinition("GenericHostTests", DisableParallelization = true)]
public class GenericHostTestsCollection { }

[Collection("GenericHostTests")]
public class GenericHostTests : IDisposable
{
    private readonly TestEventListener _openTelemetrySdkEventListener;
    private readonly TestEventListener _simpleOpenTelemetryEventListener;

    List<Metric> _exportedMetrics;
    List<LogRecord> _exportedLogs;
    List<Activity> _exportedTraces;
    


    public GenericHostTests()
    {
        _openTelemetrySdkEventListener = new("OpenTelemetry-");
        _simpleOpenTelemetryEventListener = new();
        _exportedMetrics = new();
        _exportedLogs = new();
        _exportedTraces = new();
    }

    public void Dispose()
    {
        _openTelemetrySdkEventListener.Dispose();
        _simpleOpenTelemetryEventListener.Dispose();
    }

    [Fact]
    public async Task AddSimpleOpenTelemetry_OnHostApplicationBuilder_ShouldBuildAndStartWithoutError_And_RegistersOpenTelemetryProviders()
    {
        // ARRANGE - Create a config dictionary with SimpleOpenTelemetry settings
        var configDict = AllSignalBaseConfig();

        // ACT
        using var host = GetHost(configDict);

        // Start the host in a background task
        var hostTask = host.StartAsync();

        try
        {
            // Give the host a moment to start
            await Task.Delay(500);

            // ASSERT - Verify OpenTelemetry services are registered and configured
            var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
            Assert.NotNull(tracerProvider);

            var meterProvider = host.Services.GetRequiredService<MeterProvider>();
            Assert.NotNull(meterProvider);

            var loggerProvider = host.Services.GetRequiredService<LoggerProvider>();
            Assert.NotNull(loggerProvider);

            // Verify the host resource attributes are set correctly
            var traceResource = tracerProvider.GetResource();
            Assert.NotNull(traceResource);
            Assert.NotEmpty(traceResource.Attributes);

            await Task.Delay(500);
            var diagObserver = host.Services.GetServices<IHostedService>()
                .FirstOrDefault(s => s.GetType().Name.Contains("Telemetry"));

            AssertNoErrorEvents(_openTelemetrySdkEventListener.Events);
            AssertNoErrorEvents(_simpleOpenTelemetryEventListener.Events);

            // Verify we can get a logger and use it to check exported logs
            var logger = host.Services.GetRequiredService<ILogger<GenericHostTests>>();
            Assert.NotNull(logger);

            logger.LogInformation("Test information message");
            logger.LogDebug("Test debug message");
            logger.LogWarning("Test warning message");
            logger.LogCritical("Test critical message");
            logger.LogTrace("Test trace message");

            // trigger http call to verify exporter metrics
            using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api.github.com");

            var zenResponse = await httpClient.GetAsync("/zen");

            // need to force telemetry through early
            tracerProvider.ForceFlush();
            loggerProvider.ForceFlush();
            meterProvider.ForceFlush();

            await Task.Delay(500);

            VerifyTelemetryExport();
        }
        finally
        {
            // Clean up - stop the host
            await host.StopAsync();
            await hostTask;
            host.Dispose();
        }
    }

    private void AssertNoErrorEvents(IReadOnlyList<EventWrittenEventArgs> events)
    {
        var errorEvents = events.Where(x => 
            x.Level == EventLevel.Error || x.Level == EventLevel.Critical);

        Assert.Empty(errorEvents);
        Assert.NotEmpty(events);
    }

    private IHost GetHost(Dictionary<string, string?> configDict)
    {
        var builder = Host.CreateApplicationBuilder();

        // Add the config dictionary to the builder's configuration
        builder.Configuration.AddInMemoryCollection(configDict);
        builder.Configuration.AddEnvironmentVariables();

        // Add minimal logging services
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        // Add SimpleOpenTelemetry extension method on the host builder before build
        var otelBuilder = builder.AddSimpleOpenTelemetry();
        Assert.NotNull(otelBuilder);
        otelBuilder.WithLogging(l => l.AddInMemoryExporter(_exportedLogs));
        otelBuilder.WithMetrics(l => l.AddInMemoryExporter(_exportedMetrics));
        otelBuilder.WithTracing(l => l.AddInMemoryExporter(_exportedTraces));

        var host = builder.Build();

        return host;
    }

    private Dictionary<string, string?> AllSignalBaseConfig()
    {
        // These are set to enable all signal providers
        return new Dictionary<string, string?>()
        {
            [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Instrumentations:0"] = "HttpClient",
            [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Instrumentations:0"] = "Process",
            [$"{SimpleOpenTelemetryOptions.SectionName}:Log:Settings:IncludeFormattedMessage"] = "true",
        };
    }

    private void VerifyTelemetryExport()
    {
        foreach (var signal in new[] { "trace", "information", "critical", "warning", "debug" })
            Assert.Contains(_exportedLogs, r => r.FormattedMessage!.Contains($"Test {signal} message"));

        Assert.NotEmpty(_exportedMetrics.Where(r => r.Name.StartsWith("process.")).AsEnumerable());
        
        Assert.Equal(1, _exportedTraces.Count(r => r.TagObjects.Any(t => t.Key == "url.full" && (t.Value?.ToString() == "https://api.github.com/zen"))));

    }
}
