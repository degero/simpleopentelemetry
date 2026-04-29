using Amazon.Runtime.Telemetry.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Utils;
using Xunit;


namespace SimpleOpenTelemetryTests.Extensions;

public class ServiceProviderExtensionsTests
{
    
    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhenServiceProviderIsNull()
    {
        // ARRANGE
        IServiceProvider? services = null;
        // aCT/ASSERT
        Assert.Throws<ArgumentNullException>(() =>
            ServiceProviderExtensions.SimpleOpenTelemetryValidate(services!));
    }

    // TODO Chad change these to not throw
    [Fact]
    public void SimpleOpenTelemetryValidate_ThrowsWhen_AddSimpleOpenTelemetry_NotCalled()
    {
        // ARRANGE
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();

        // aCT/ASSERT
        var exception = Assert.Throws<InvalidOperationException>(serviceProvider.SimpleOpenTelemetryValidate);
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
        // ARRANGE
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>() {
                ["SimpleOpenTelemetry:ExporterOptions"] =  "{}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostedService, TelemetryHostedService>();
        using var serviceProvider = services.BuildServiceProvider();

        // aCT/ASSERT
        var exception = Assert.Throws<InvalidOperationException>(serviceProvider.SimpleOpenTelemetryValidate);
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
        // ARRANGE
        var dict = new Dictionary<string, object>();
        if (serviceName is not null)
            dict.Add("service.name", serviceName);
        if (resourceAttributes is not null)
        {
            resourceAttributes.Split(',').ToList().ForEach(x =>
            {
                dict.Add(x.Split('=')[0], x.Split('=')[1]);
            });
        }
        
        var resorceBuilder = ResourceBuilder.CreateDefault()
            .AddAttributes(dict);

        if (serviceName is not null)
            resorceBuilder.AddService(serviceName);

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resorceBuilder).Build();

        // ACT
        var services = new ServiceCollection();
        services.AddSingleton<IHostedService, TelemetryHostedService>();
        services.AddSingleton(meterProvider);
        using var serviceProvider = services.BuildServiceProvider();
        
        // ASSERT
        if (valid)
           serviceProvider.SimpleOpenTelemetryValidate();
        else
        {
            var ex = Assert.Throws<InvalidOperationException>(serviceProvider.SimpleOpenTelemetryValidate);
            Assert.Contains("Missing required OpenTelemetry resource attributes", ex.Message);
        }
    }

    [Theory]
    [InlineData("metric")]
    [InlineData("log")]
    [InlineData("trace")]
    public void SimpleOpenTelemetryValidate_DoesNotThrow_WhenAllResourceAttributes_And_AtLeastOneSignalProviderExists(
        string signal
    )
    {
        // ARRANGE
        var serviceName = "test-service";
        var resourceAttributes = "service.namespace=test-namespace,service.version=1.2.3,deployment.environment.name=dev";
    
        var dict = new Dictionary<string, object>();
        if (serviceName is not null)
            dict.Add("service.name", serviceName);
        if (resourceAttributes is not null)
        {
            resourceAttributes.Split(',').ToList().ForEach(x =>
            {
                dict.Add(x.Split('=')[0], x.Split('=')[1]);
            });
        }
        var services = new ServiceCollection();
        services.AddSingleton<IHostedService, TelemetryHostedService>();

        if (signal == "metric")
        {
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName)
                .AddAttributes(dict)).Build();
            services.AddSingleton(meterProvider!);
        }
        else if (signal == "trace")
        {
            var traceProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName)
                .AddAttributes(dict)).Build();
            services.AddSingleton(traceProvider!);
        }
        else
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(
                        ResourceBuilder.CreateEmpty()
                            .AddAttributes(dict));
                });
            });
        }
        using var serviceProvider = services.BuildServiceProvider();
        
        // ACT/ASSERT
        // Should not throw when TracerProvider/LogProvider/MetricProvider is found 
        serviceProvider.SimpleOpenTelemetryValidate();
    }
}