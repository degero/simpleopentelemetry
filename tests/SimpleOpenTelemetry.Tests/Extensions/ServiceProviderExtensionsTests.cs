using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Extensions;
using System.Diagnostics.Tracing;
using Xunit;

namespace SimpleOpenTelemetryTests.Extensions;

[CollectionDefinition("ServiceProviderExtensionsTests", DisableParallelization = true)]
public class ServiceProviderExtensionsTestsCollection { }

[Collection("ServiceProviderExtensionsTests")]
public class ServiceProviderExtensionsTests : IDisposable
{
    private readonly TestEventListener _simpleOpenTelemetryEventListener;

    public ServiceProviderExtensionsTests()
    {
        _simpleOpenTelemetryEventListener = new();
    }

    public void Dispose()
    {
        _simpleOpenTelemetryEventListener.Dispose();
    }

    // Just for testing purposes to trigger validation - a fake of OpenTelemetry's TelemetryHostedService
    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Extensions.Hosting/Implementation/TelemetryHostedService.cs
    internal class TelemetryHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    
    [Fact]
    public void SimpleOpenTelemetryValidate_LogsError_WhenServiceProviderIsNull()
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);
        
        IServiceProvider? services = null;

        // ACT
        var result = ServiceProviderExtensions.SimpleOpenTelemetryValidate(services!);

        // ASSERT
        Assert.False(result);
        var error = Assert.Single(_simpleOpenTelemetryEventListener.Events, e => e.Level == EventLevel.Error);
        Assert.Contains("services argument is null", MessageOf(error));
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_LogsError_When_AddSimpleOpenTelemetry_NotCalled()
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);
        
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();

        // ACT
        var result = serviceProvider.SimpleOpenTelemetryValidate();

        // ASSERT
        Assert.False(result);
        var error = Assert.Single(_simpleOpenTelemetryEventListener.Events, e => e.Level == EventLevel.Error);
        Assert.Contains("OpenTelemetry has not been registered", MessageOf(error));
    }

    [Fact]
    public void SimpleOpenTelemetryValidate_LogsError_When_AddSimpleOpenTelemetryCalled_ButNoSignalsConfigured()
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>() {
                [$"{SimpleOpenTelemetryOptions.SectionName}:ExporterOptions"] = "{}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostedService, TelemetryHostedService>();
        using var serviceProvider = services.BuildServiceProvider();

        // ACT
        var result = serviceProvider.SimpleOpenTelemetryValidate();

        // ASSERT
        Assert.False(result);

        var error = Assert.Single(_simpleOpenTelemetryEventListener.Events, e => e.Level == EventLevel.Error);
        Assert.Contains("No OpenTelemetry signal providers have been registered.", MessageOf(error));
    }

    [Theory]
    [InlineData("test-service", null)]
    [InlineData("test-service", "service.version=1.2.3,deployment.environment.name=dev")]
    [InlineData("test-service", "service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev", true)]
    [InlineData(null, "service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev", true)] // opentelemetry sets a default servicename 'unknown_service'
    public void SimpleOpenTelemetryValidate_LogsError_When_ResourceAttribute_Missing(
        string? serviceName,
        string? resourceAttributes,
        bool valid = false)
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);
        

        var dict = new Dictionary<string, object>();
        if (serviceName is not null)
            dict.Add("service.name", serviceName);
        if (resourceAttributes is not null)
            resourceAttributes.Split(',').ToList().ForEach(x =>
            {
                dict.Add(x.Split('=')[0], x.Split('=')[1]);
            });

        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(dict);
        if (serviceName is not null)
            resourceBuilder.AddService(serviceName);

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostedService, TelemetryHostedService>();
        services.AddSingleton(meterProvider);
        using var serviceProvider = services.BuildServiceProvider();

        // ACT
        var result = serviceProvider.SimpleOpenTelemetryValidate();

        // ASSERT
        if (valid)
        {
            Assert.True(result);
            Assert.DoesNotContain(_simpleOpenTelemetryEventListener.Events, e => e.Level == EventLevel.Error);
        }
        else
        {
            Assert.False(result);
            var error = Assert.Single(_simpleOpenTelemetryEventListener.Events, e => e.Level == EventLevel.Error);
            Assert.Contains("Missing required OpenTelemetry resource attributes", MessageOf(error));
        }
    }

    [Theory]
    [InlineData("metric")]
    [InlineData("log")]
    [InlineData("trace")]
    public void SimpleOpenTelemetryValidate_DoesNotLogError_WhenAllResourceAttributes_And_AtLeastOneSignalProviderExists(
        string signal)
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);

        var serviceName = "test-service";
        var resourceAttributes = "service.namespace=test-namespace,service.version=1.2.3,deployment.environment.name=dev";

        var dict = new Dictionary<string, object>();
        dict.Add("service.name", serviceName);
        resourceAttributes.Split(',').ToList().ForEach(x =>
        {
            dict.Add(x.Split('=')[0], x.Split('=')[1]);
        });

        var services = new ServiceCollection();
        services.AddSingleton<IHostedService, TelemetryHostedService>();

        if (signal == "metric")
        {
            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName).AddAttributes(dict)).Build();
            services.AddSingleton(meterProvider!);
        }
        else if (signal == "trace")
        {
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName).AddAttributes(dict)).Build();
            services.AddSingleton(tracerProvider!);
        }
        else
        {
            services.AddLogging(logging =>
                logging.AddOpenTelemetry(options =>
                    options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddAttributes(dict))));
        }

        using var serviceProvider = services.BuildServiceProvider();

        // ACT
        var result = serviceProvider.SimpleOpenTelemetryValidate();
        
        // ASSERT
        Assert.True(result);
        Assert.DoesNotContain(_simpleOpenTelemetryEventListener.Events, e => e.Level == EventLevel.Error);
    }

    private static string? MessageOf(EventWrittenEventArgs e) =>
        e.Payload?.Count > 1 ? e.Payload[1]?.ToString() : null;

}