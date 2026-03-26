using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Instrumentation;
using SimpleOpenTelemetry.Instrumenttaion;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryTests.Utils;

public class SimpleOpenTelemetryUtilsTests
{
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
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = "test-service",
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] =
                    "service.version=1.2.3,deployment.environment.name=dev",

                ["SimpleOpenTelemetry:TracingInstrumentations:0"] =
                    nameof(TracingInstrumentationEnum.AspNetCore)
            })
            .Build();

        var services = new ServiceCollection();

        var ex = Assert.ThrowsAny<System.Exception>(() => services.AddSimpleOpenTelemetry(config));
        Assert.Contains("Cannot load assembly", ex.ToString());
    }

    [Fact]
    public void AddMetricsInstrumentation_ThrowsWhenInstrumentationAssemblyMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = "test-service",
                [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] =
                    "service.version=1.2.3,deployment.environment.name=dev",

                ["SimpleOpenTelemetry:MetricsInstrumentations:0"] =
                    nameof(MetricsInstrumentationEnum.AspNetCore)
            })
            .Build();

        var services = new ServiceCollection();

        var ex = Assert.ThrowsAny<System.Exception>(() => services.AddSimpleOpenTelemetry(config));
        Assert.Contains("Cannot load assembly", ex.ToString());
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
                ["SimpleOpenTelemetry:TracingInstrumentations:0"] = "999"
            })
            .Build();

        var services = new ServiceCollection();

        var ex = Assert.ThrowsAny<System.Exception>(() => services.AddSimpleOpenTelemetry(config));
        Assert.Contains("type not found", ex.ToString());
    }

    [Fact]
    public void AddTracingInstrumentation_WrapsTypeLookupErrors()
    {
        var previousDescriptor =
            InstrumentationAssemblies.KnownTraceInstrumentations[TracingInstrumentationEnum.AspNetCore];

        InstrumentationAssemblies.KnownTraceInstrumentations[TracingInstrumentationEnum.AspNetCore] =
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
                    ["SimpleOpenTelemetry:TracingInstrumentations:0"] =
                        nameof(TracingInstrumentationEnum.AspNetCore)
                })
                .Build();

            var services = new ServiceCollection();
            var ex = Assert.ThrowsAny<System.Exception>(() => services.AddSimpleOpenTelemetry(config));
            Assert.Contains("Failed to register otel instrumentation", ex.ToString());
        }
        finally
        {
            InstrumentationAssemblies.KnownTraceInstrumentations[TracingInstrumentationEnum.AspNetCore] =
                previousDescriptor;
        }
    }
}

