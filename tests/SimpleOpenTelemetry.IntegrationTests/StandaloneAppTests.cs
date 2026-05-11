using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryIntegrationTests;

[CollectionDefinition("StandaloneAppTests", DisableParallelization = true)]
public class StandaloneAppTestsCollection { }

[Collection("StandaloneAppTests")]
public class StandaloneAppTests
{
    [Fact]
    public void AddSimpleOpenTelemetry_StandaloneApp_ShouldInitializeWithConfigDictionary()
    {
        // ARRANGE - Create a config dictionary with SimpleOpenTelemetry settings
        var configDict = new Dictionary<string, string?>()
        {
            [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Settings:SetErrorStatusOnException"] = "true",
            [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Settings:MetricLimit"] = "100",
            [$"{SimpleOpenTelemetryOptions.SectionName}:Log:Extensions:0"] = "None",
            [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = "test-standalone-app",
            [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] = "service.version=1.0.0,deployment.environment.name=test"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .AddEnvironmentVariables()
            .Build();

        // ACT - Call StandaloneApp.AddSimpleOpenTelemetry() which is the UoT (Unit of Test)
        var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);

        try
        {
            // ASSERT - Verify OpenTelemetry is properly initialized
            Assert.NotNull(sdk);

            // Verify we can get a logger factory from the SDK
            var loggerFactory = sdk.GetLoggerFactory();
            Assert.NotNull(loggerFactory);

            // Create a typed logger and verify it works
            var logger = loggerFactory.CreateLogger<StandaloneAppTests>();
            Assert.NotNull(logger);

            // Verify we can log messages without exceptions
            logger.LogInformation("Test information message from StandaloneApp");
            logger.LogDebug("Test debug message from StandaloneApp");
            logger.LogWarning("Test warning message from StandaloneApp");
            logger.LogError("Test error message from StandaloneApp");

            // Verify TracerProvider is configured
            var tracerProvider = sdk.TracerProvider;
            Assert.NotNull(tracerProvider);

            // Verify MeterProvider is configured
            var meterProvider = sdk.MeterProvider;
            Assert.NotNull(meterProvider);

            // Verify LoggerProvider is configured
            var loggerProvider = sdk.LoggerProvider;
            Assert.NotNull(loggerProvider);

            // Verify resource attributes are set from configuration
            var traceResource = tracerProvider.GetResource();
            Assert.NotNull(traceResource);
            Assert.NotEmpty(traceResource.Attributes);

            // Verify service name from config
            Assert.True(Environment.GetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME) == "test-standalone-app");
        }
        finally
        {
            // Clean up - dispose the SDK to flush any pending telemetry
            sdk!.Dispose();

            // Clean up environment variables set by AddSimpleOpenTelemetry
            Environment.SetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME, null);
            Environment.SetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES, null);
        }
    }
}
