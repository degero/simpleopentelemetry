using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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


[CollectionDefinition("WebApplicationTests", DisableParallelization = true)]
public class WebApplicationTestsCollection { }


[Collection("WebApplicationTests")]
public class WebApplicationTests : IDisposable
{
    private readonly TestEventListener _openTelemetrySdkEventListener;
    private readonly TestEventListener _simpleOpenTelemetryEventListener;

    List<Metric> _exportedMetrics;
    List<LogRecord> _exportedLogs;
    List<Activity> _exportedTraces;
    


    public WebApplicationTests()
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
    public async Task AddSimpleOpenTelemetry_OnWebApplicationBuilder_ShouldBuildAndStartWithoutError_And_RegistersOpenTelemetryProviders()
    {
        // ARRANGE - Create a config dictionary with SimpleOpenTelemetry settings
        var configDict = AllSignalBaseConfig();

        // ACT
        using var app = GetWebApplication(configDict);

        // Start the app in a background task
        var appTask = app.RunAsync();

        try
        {
            // Give the app a moment to start
            await Task.Delay(500);

            // ASSERT - Verify OpenTelemetry services are registered and configured
            var tracerProvider = app.Services.GetRequiredService<TracerProvider>();
            Assert.NotNull(tracerProvider);

            var meterProvider = app.Services.GetRequiredService<MeterProvider>();
            Assert.NotNull(meterProvider);

            var loggerProvider = app.Services.GetRequiredService<LoggerProvider>();
            Assert.NotNull(loggerProvider);

            // Verify the app resource attributes are set correctly
            var traceResource = tracerProvider.GetResource();
            Assert.NotNull(traceResource);
            Assert.NotEmpty(traceResource.Attributes);

            // Verify app can function and handle HTTP requests (via health check)
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:5000");
            
            var healthResponse = await httpClient.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

            var echoResponse = await httpClient.GetAsync("/echo/test");
            Assert.Equal(HttpStatusCode.OK, echoResponse.StatusCode);
            var echoContent = await echoResponse.Content.ReadAsStringAsync();
            Assert.Equal("Echo: test", echoContent);

            // Verify we can get a logger and use it to check exported logs
            var logger = app.Services.GetRequiredService<ILogger<WebApplicationTests>>();
            Assert.NotNull(logger); 
            logger.LogInformation("Test information message");
            logger.LogDebug("Test debug message");
            logger.LogWarning("Test warning message");
            logger.LogCritical("Test critical message");
            logger.LogTrace("Test trace message");

            await Task.Delay(500);

            AssertNoErrorEvents(_openTelemetrySdkEventListener.Events);
            AssertNoErrorEvents(_simpleOpenTelemetryEventListener.Events);

            // need to force telemetry through early
            tracerProvider.ForceFlush();
            loggerProvider.ForceFlush();
            meterProvider.ForceFlush();

            await Task.Delay(500);
            VerifyTelemetryExport();

        }
        finally
        {
            // Clean up - stop the app
            await app.StopAsync();
            await appTask;

        }
    }

    private void AssertNoErrorEvents(IReadOnlyList<EventWrittenEventArgs> events)
    {
        var errorEvents = events.Where(x => 
            x.Level == EventLevel.Error || x.Level == EventLevel.Critical);

        Assert.Empty(errorEvents);
        Assert.NotEmpty(events);
        // return Task.CompletedTask;
    }

    private WebApplication GetWebApplication(Dictionary<string, string?> configDict)
    {
        
        var builder = WebApplication.CreateBuilder();
        
        // Add the config dictionary to the builder's configuration
        builder.Configuration.AddInMemoryCollection(configDict);
        builder.Configuration.AddEnvironmentVariables();

        // Add minimal logging services for headless app
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        // Add SimpleOpenTelemetry extension method on the host builder before build
        var otelBuilder = builder.AddSimpleOpenTelemetry();
        Assert.NotNull(otelBuilder);
        otelBuilder.WithLogging(l => l.AddInMemoryExporter(_exportedLogs));
        otelBuilder.WithMetrics(l => l.AddInMemoryExporter(_exportedMetrics));
        otelBuilder.WithTracing(l => l.AddInMemoryExporter(_exportedTraces));

        // Add a simple health check endpoint for testing
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // Map the health check endpoint
        app.MapHealthChecks("/health");

        // Add a simple echo endpoint to verify the app is working
        app.MapGet("/echo/{message}", (string message) => $"Echo: {message}");

        return app;
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

        Assert.Equal(2, _exportedTraces.Count(r => r.TagObjects.Any(t => t.Key == "url.full" && (t.Value?.ToString() == "http://localhost:5000/echo/test" ||
            t.Value?.ToString() == "http://localhost:5000/health"))));

    }
}
