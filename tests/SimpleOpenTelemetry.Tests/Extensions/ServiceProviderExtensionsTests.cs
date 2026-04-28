using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

public class ServiceProviderExtensionsTests
{
    
    // TODO Chad change these to not throw
    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhen_AddSimpleOpenTelemetry_NotCalled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(app.Services.SimpleOpenTelemetryValidate);
        Assert.Contains("OpenTelemetry has not been registered", exception.Message);
    }

    // Just for testing purposes to trigger validation - a fake of OpenTelemetry's TelemetryHostedService
    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Extensions.Hosting/Implementation/TelemetryHostedService.cs
    internal class TelemetryHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhen_AddSimpleOpenTelemetryCalled_ButNoSignalsConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>() {
                ["SimpleOpenTelemetry:ExporterOptions"] =  "{}"
            })
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IHostedService, TelemetryHostedService>();
        builder.Configuration.AddConfiguration(config);
        var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(app.Services.SimpleOpenTelemetryValidate);
        Assert.Contains("No OpenTelemetry signal providers have been registered.", exception.Message);
    }

    [Theory]
    [InlineData("test-service", null)]
    [InlineData("test-service","service.version=1.2.3,deployment.environment.name=dev")]
    [InlineData("test-service","service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev", true)]
    [InlineData(null,"service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev", true)] // opentelemetry sets a default servicename 'unknown_serice'
    public void SimpleOpenTelemetryValidate_Throws_When_ResourceAttribute_Missing(
        string? serviceName,
        string? resourceAttributes,
        bool valid = false
    )
    {
        Dictionary<string, string?> dict = new() {
            ["SimpleOpenTelemetry:Trace"] =  "{}"
        };
        
        if (serviceName is not null)
            dict.Add(OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME, serviceName);
        if (resourceAttributes is not null)
            dict.Add(OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES, resourceAttributes);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
        
        // ACT
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();
        using var app = builder.Build();
        
        // ASSERT
        if (valid)
           app.Services.SimpleOpenTelemetryValidate();
        else
        {
            var ex = Assert.Throws<InvalidOperationException>(app.Services.SimpleOpenTelemetryValidate);
            Assert.Contains("Missing required OpenTelemetry resource attributes", ex.Message);
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
        const string resourceAttributes = "service.namespace=test-namespace;service.version=1.2.3,deployment.environment.name=dev";

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
                        ["service.namespace"] = "test-namespace",
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
        const string resourceAttributes = "service.namespace=test-namespace;service.version=1.2.3,deployment.environment.name=dev";

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
                        ["service.namespace"] = "test-namespace",
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
        const string resourceAttributes = "service.namespace=test-namespace;service.version=1.2.3,deployment.environment.name=dev";

        using (new OtelEnvironmentScope(
               [
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME,
                       serviceName),
                   new KeyValuePair<string, string>(
                       OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES,
                       resourceAttributes)
               ]))
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
                        ["service.namespace"] = "test-namespace",
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
}