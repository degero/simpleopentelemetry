using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryTests;

[Collection("StandaloneAppTests")]
public class StandaloneAppTests
{

    private void ClearOTELEnvVars()
    {
        Array.ForEach([
            OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
            OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES
        ], key => Environment.SetEnvironmentVariable(key, null));
    }

    // todo add test for config validation

    [Fact]
    public void StandaloneBootstrap_AddSimpleOpenTelemetry_ShouldSet_OTEL_EnvVars_FromConfiguration()
    {
        try
        {
            // ARRANGE
            const string serviceName = "test-service";
            const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

            // ACT
            StandaloneApp.AddSimpleOpenTelemetry(config);

            // ASSERT
            Assert.Equal(serviceName, Environment.GetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME));
            Assert.Equal(resourceAttributes, Environment.GetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES));
        
        }
        finally
        {
            ClearOTELEnvVars();
        }
    }

    [Fact]
    public void StandaloneBootstrap_AddSimpleOpenTelemetry_ShouldSet_CallOpenTelemetryBuilder_Configure()
    {
        // 
        var originalPropagator = Propagators.DefaultTextMapPropagator;

        try
        {
            // ARRANGE
            const string serviceName = "test-service";
            const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes, new () {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators:0"] = "B3",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators:1"] = "Baggage"
            });

            // ACT
            var sdk = SimpleOpenTelemetry.StandaloneApp.AddSimpleOpenTelemetry(config);

            // ASSERT
            // Assert - not ideal but cant verify by mocked / injected services due to extension method calling 
            // AddOpenTelemetry extension method and creating a new SimpleOpenTelemetryBuilder
            var propagator = Propagators.DefaultTextMapPropagator;
            Assert.IsType<CompositeTextMapPropagator>(propagator);
            var innerPropagators = TestHelpers.GetCompositePropagators(propagator as CompositeTextMapPropagator).ToList();
            Assert.Equal(2, innerPropagators.Count);
            Assert.IsType<OpenTelemetry.Extensions.Propagators.B3Propagator>(innerPropagators[0]);
            Assert.IsType<BaggagePropagator>(innerPropagators[1]);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(originalPropagator);
            ClearOTELEnvVars();
        }
    }
    
    private IConfiguration BuildConfigWithOtelValues(
            string otelServiceName, string otelResourceAttributes, 
            Dictionary<string, string?>? otherValues = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0"] = "console",
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = otelServiceName,
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] = otelResourceAttributes
            })
            .AddInMemoryCollection(otherValues ?? new Dictionary<string, string?>())
            .Build();
}

