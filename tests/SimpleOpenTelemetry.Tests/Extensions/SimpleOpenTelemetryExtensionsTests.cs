using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

public class SimpleOpenTelemetryExtensionsTests
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
    public void AddSimpleOpenTelemetry_ThrowsOnNullServices()
    {
        var config = new ConfigurationBuilder().Build();

        IServiceCollection? services = null;

        Assert.Throws<System.ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddSimpleOpenTelemetry(services!, config));
    }

    [Fact]
    public void AddSimpleOpenTelemetry_ThrowsWhenSimpleOpenTelemetrySectionIsMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<Exception>(() => services.AddSimpleOpenTelemetry(config));
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_PassesWhenRequiredResourceAttributesPresent()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";

        using (new OtelEnvironmentScope(new[]
               {
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
                       serviceName),
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES,
                       resourceAttributes)
               }))
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SimpleOpenTelemetry:ServiceName"] = serviceName,
                    ["SimpleOpenTelemetry:ServiceVersion"] = "1.0.0",
                    ["SimpleOpenTelemetry:Trace:Exporters:0:type"] = "console"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            // Explicitly register a TracerProvider with required resource attributes
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["service.version"] = "1.2.3",
                        ["deployment.environment.name"] = "dev"
                    }))
                .Build();
            services.AddSingleton(tracerProvider);
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

            // Should not throw when all required attributes are present
            provider.SimpleOpenTelemetryValidate();
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhenRequiredResourceAttributesMissing()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3"; // Missing: deployment.environment.name

        using (new OtelEnvironmentScope(new[]
               {
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
                       serviceName),
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES,
                       resourceAttributes)
               }))
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SimpleOpenTelemetry:ServiceName"] = serviceName,
                    ["SimpleOpenTelemetry:ServiceVersion"] = "1.0.0",
                    ["SimpleOpenTelemetry:Trace:Exporters:0:type"] = "console"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            // Register a TracerProvider without deployment.environment.name attribute
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["service.version"] = "1.2.3"
                    }))
                .Build();
            services.AddSingleton(tracerProvider);
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => provider.SimpleOpenTelemetryValidate());
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhenServiceProviderIsNull()
    {
        IServiceProvider? services = null;

        Assert.Throws<ArgumentNullException>(() =>
            ServiceProviderExtensions.SimpleOpenTelemetryValidate(services!));
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_WorksWithTracingOnlyConfiguration()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";

        using (new OtelEnvironmentScope(new[]
               {
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
                       serviceName),
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES,
                       resourceAttributes)
               }))
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SimpleOpenTelemetry:ServiceName"] = serviceName,
                    ["SimpleOpenTelemetry:ServiceVersion"] = "1.0.0",
                    ["SimpleOpenTelemetry:Trace:Exporters:0:type"] = "console"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            // Create a basic TracerProvider with valid resource attributes
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["service.version"] = "1.2.3",
                        ["deployment.environment.name"] = "dev"
                    }))
                .Build();
            services.AddSingleton(tracerProvider);
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

            // Should not throw - TracerProvider with valid resource is available
            provider.SimpleOpenTelemetryValidate();
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_WorksWithMetricsOnlyConfiguration()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";

        using (new OtelEnvironmentScope(new[]
               {
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
                       serviceName),
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES,
                       resourceAttributes)
               }))
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SimpleOpenTelemetry:ServiceName"] = serviceName,
                    ["SimpleOpenTelemetry:ServiceVersion"] = "1.0.0",
                    ["SimpleOpenTelemetry:Metric:Exporters:0:type"] = "console"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            // Create a basic MeterProvider with valid resource attributes
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["service.version"] = "1.2.3",
                        ["deployment.environment.name"] = "dev"
                    }))
                .Build();
            services.AddSingleton(meterProvider);
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

            // Should not throw - MeterProvider with valid resource is available
            provider.SimpleOpenTelemetryValidate();
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_WorksWithLoggingOnlyConfiguration()
    {
        const string serviceName = "test-service";
        const string resourceAttributes = "service.version=1.2.3,deployment.environment.name=dev";

        using (new OtelEnvironmentScope(new[]
               {
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
                       serviceName),
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES,
                       resourceAttributes)
               }))
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SimpleOpenTelemetry:ServiceName"] = serviceName,
                    ["SimpleOpenTelemetry:ServiceVersion"] = "1.0.0",
                    ["SimpleOpenTelemetry:Log:Exporters:0:type"] = "console"
                })
                .Build();

            var services = new ServiceCollection();
            // Create and register MeterProvider (since LoggerProvider registration is complex)
            // In real applications, providers are registered when tools themselves are configured
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["service.version"] = "1.2.3",
                        ["deployment.environment.name"] = "dev"
                    }))
                .Build();
            services.AddSingleton(meterProvider);
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

            // Should not throw - MeterProvider with valid resource is available
            provider.SimpleOpenTelemetryValidate();
        }
    }

    [Fact(Skip = "true")] // TODO Chad reinstate with eventlogging only option as this throws before app is built
    public void SimpleOpenTelemetryValidate_ThrowsWhenSimpleOpenTelemetryConfigSignalSubSections_AreUndefined()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                ["SimpleOpenTelemetry"] = "{}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSimpleOpenTelemetry(config); // Config section missing - no providers are created

        var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.SimpleOpenTelemetryValidate());
        Assert.Contains("No OpenTelemetry signal providers have been registered.", exception.Message);
    }
    
    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhen_AddSimpleOpenTelemetry_NotCalled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.SimpleOpenTelemetryValidate());
        Assert.Contains("OpenTelemetry has not been registered", exception.Message);
    }

}

