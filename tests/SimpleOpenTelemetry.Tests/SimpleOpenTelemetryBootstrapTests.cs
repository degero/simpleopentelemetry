using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryTests;

[Collection("SimpleOpenTelemetryBootstrapTests")]
public class SimpleOpenTelemetryBootstrapTests : IDisposable
{

    private readonly TestEventListener _listener;

    public SimpleOpenTelemetryBootstrapTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
        SimpleOpenTelemetryBootstrap.Shutdown();
    }

    private void ClearOTELEnvVars()
    {
        Array.ForEach([
            OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
            OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES
        ], key => Environment.SetEnvironmentVariable(key, null));
    }


    [Fact]
    public void AddSimpleOpenTelemetry_ShouldSet_OTEL_EnvVars_FromConfiguration()
    {
        Assert.Empty(_listener.Events);

        try
        {
            // ARRANGE
            const string serviceName = "test-service";
            const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

            // ACT
            SimpleOpenTelemetryBootstrap.Add(config);

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
    public void AddSimpleOpenTelemetry_ShouldNotOverride_OTEL_EnvVars_FromConfigurationLoading()
    {
        Assert.Empty(_listener.Events);

        try
        {
            // ARRANGE
            const string serviceName = "test-service";
            const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

            Environment.SetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME, "the-real-servicename");
            Environment.SetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES, "the-real-attributes");

            // ACT
            SimpleOpenTelemetryBootstrap.Add(config);

            // ASSERT
            Assert.Equal("the-real-servicename", Environment.GetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME));
            Assert.Equal("the-real-attributes", Environment.GetEnvironmentVariable(OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES));

        }
        finally
        {
            ClearOTELEnvVars();

        }
    }

    [Fact]
    public void AddSimpleOpenTelemetry_Should_CallOpenTelemetryBuilder_Configure()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);

        var originalPropagator = Propagators.DefaultTextMapPropagator;

        try
        {
            const string serviceName = "test-service";
            const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes, new()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators:0"] = "B3",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators:1"] = "Baggage"
            });

            // ACT
            SimpleOpenTelemetryBootstrap.Add(config);

            // ASSERT
            // Assert - not ideal but cant verify by mocked / injected services due to extension method calling
            // AddOpenTelemetry extension method and creating a new SimpleOpenTelemetryBuilder
            var propagator = Propagators.DefaultTextMapPropagator;
            Assert.IsType<CompositeTextMapPropagator>(propagator);
            var compositeTextMapPropagator = propagator as CompositeTextMapPropagator;
            var innerPropagators = compositeTextMapPropagator != null ? TestHelpers.GetCompositePropagators(compositeTextMapPropagator).ToList() : new();
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


    [Fact]
    public void AddSimpleOpenTelemetry_Should_LogErrorWhen_UsingUnsupportedDistro()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);

        var originalPropagator = Propagators.DefaultTextMapPropagator;

        try
        {
            var distroName = DistroEnum.AzureMonitorAspNetCore.ToString();
            const string serviceName = "test-service";
            const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes, new()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Distro"] = distroName,
            });

            // ACT
            SimpleOpenTelemetryBootstrap.Add(config);

            // ASSERT
            var errorEvent = _listener.Events.FirstOrDefault(r => r.Level == System.Diagnostics.Tracing.EventLevel.Error &&
                r.Payload is not null &&
                r.Payload.Any(x => x?.ToString()?.Contains($"Unsupported OpenTelemetry Distro '{distroName}'. This Distro can not be used with OpenTelemetrySDKBuilder.") ?? false));
            Assert.NotNull(errorEvent);

        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(originalPropagator);
            ClearOTELEnvVars();
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_Should_ReturnTrue_When_SDK_Resource_And_Attributes_Set()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        OpenTelemetrySdk? sdk = null;

        try
        {
            const string serviceName = "test-service";
            const string resourceAttributes = "service.namespace=test,service.version=1.2.3,deployment.environment.name=dev";

            var dict = CreateResourceAttributeDict(serviceName, resourceAttributes);

            sdk = OpenTelemetrySdk.Create(x => x.WithLogging(z => z.ConfigureResource(c => c.AddAttributes(dict))));

            // ACT
            var result = SimpleOpenTelemetryBootstrap.SimpleOpenTelemetryValidate(sdk);

            // ASSERT
            Assert.True(result);

        }
        finally
        {
            ClearOTELEnvVars();
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_Should_ReturnFalse_When_OpenTelemetrySdk_NotRegistered()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);

        // ACT
        var result = SimpleOpenTelemetryBootstrap.SimpleOpenTelemetryValidate(null);

        // ASSERT
        Assert.False(result);
        var errorEvent = _listener.Events.FirstOrDefault(r => r.Level == System.Diagnostics.Tracing.EventLevel.Error &&
            r.Payload is not null &&
            r.Payload.Any(x => x?.ToString()?.Contains($"OpenTelemetry has not been registered") ?? false));
        Assert.NotNull(errorEvent);

    }

    [Fact]
    public void SimpleOpenTelemetryValidate_Should_ReturnFalse_When_NoSignalProvidersRegistered()
    {
        // ARRANGE
        Assert.Empty(_listener.Events);
        OpenTelemetrySdk? sdk = null;

        try
        {
            sdk = OpenTelemetrySdk.Create(t => { });

            // ACT
            var result = SimpleOpenTelemetryBootstrap.SimpleOpenTelemetryValidate(sdk);

            // ASSERT
            Assert.False(result);
            var errorEvent = _listener.Events.FirstOrDefault(r => r.Level == EventLevel.Error &&
                r.Payload is not null &&
                r.Payload.Any(x => x?.ToString()?.Contains($"No OpenTelemetry signal providers have been registered.") ?? false));
            Assert.NotNull(errorEvent);

        }
        finally
        {
            ClearOTELEnvVars();
            sdk?.Dispose();
        }
    }

    [Theory]
    [InlineData("test-service", null)]
    [InlineData("test-service", "service.version=1.2.3,deployment.environment.name=dev")]
    [InlineData("test-service", "service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev", true)]
    [InlineData(null, "service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev", true)] // opentelemetry sets a default servicename 'unknown_service'
    public void SimpleOpenTelemetryValidate_Should_ReturnFalse_When_CoreAttributeNotSet(
        string? serviceName,
        string? resourceAttributes,
        bool valid = false)
    {
        // ARRANGE
        Assert.Empty(_listener.Events);

        var dict = CreateResourceAttributeDict(serviceName, resourceAttributes);

        OpenTelemetrySdk? sdk = null;

        try
        {
            sdk = OpenTelemetrySdk.Create(x => x.WithLogging(z => z.ConfigureResource(c => c.AddAttributes(dict))));

            // ACT
            var result = SimpleOpenTelemetryBootstrap.SimpleOpenTelemetryValidate(sdk);

            // ASSERT
            if (valid)
            {
                Assert.True(result);
                Assert.DoesNotContain(_listener.Events, e => e.Level == EventLevel.Error);
            }
            else
            {
                Assert.False(result);
                var error = Assert.Single(_listener.Events, e => e.Level == EventLevel.Error);
                Assert.Contains("Missing required OpenTelemetry resource attributes", MessageOf(error));
            }

        }
        finally
        {
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

    private string? MessageOf(EventWrittenEventArgs e) =>
        e.Payload?.Count > 1 ? e.Payload[1]?.ToString() : null;

    private Dictionary<string, object> CreateResourceAttributeDict(string? serviceName, string? resourceAttributes)
    {
        var dict = new Dictionary<string, object>();
        if (serviceName is not null)
            dict.Add("service.name", serviceName);
        if (resourceAttributes is not null)
            resourceAttributes.Split(',').ToList().ForEach(x =>
            {
                dict.Add(x.Split('=')[0], x.Split('=')[1]);
            });
        return dict;
    }

}

