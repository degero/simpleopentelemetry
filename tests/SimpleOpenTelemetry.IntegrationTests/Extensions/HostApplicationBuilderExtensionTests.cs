
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryIntegrationTests.Extensions;

public class HostApplicationBuilderExtensionsTests
{
    [Theory]
    [InlineData("test-service",
        "service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev",
        "service.name=test-service,service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev"
    )]
    [InlineData(null,
        "service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev",
        "service.name=unknown_service,service.namespace=testapp,service.version=1.2.3,deployment.environment.name=dev"
    )] // otel sets a default
    public void AddSimpleOpenTelemetry_ShouldSetServiceName_And_ResourceAttributes_FromConfig(
        string serviceName,
        string resourceAttributes,
        string expectedResourceAttributes
    )
    {
        // ARRANGE - a config list a webApplicationbuilder would pick up
        var config = BuildConfig(new Dictionary<string, string?>()
                {
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Settings:SetErrorStatusOnException"] = "true",
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Settings:MetricLimit"] = "100",
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Log:Extensions:0"] = "None",
                    [OpenTelemetryConstants.EnvironmentVariables.OTEL_SERVICE_NAME] = serviceName,
                    [OpenTelemetryConstants.EnvironmentVariables.OTEL_RESOURCE_ATTRIBUTES] = resourceAttributes
                }
            );

        var host = Host.CreateApplicationBuilder();
        host.Configuration.AddConfiguration(config);
        host.AddSimpleOpenTelemetry();

        // ACT
        using var app = host.Build();

        // ASSERT
        VerifyOTELSettings(app, expectedResourceAttributes);
    }

    [Fact]
    public void AddSimpleOpenTelemetry_Should_SetCorrectResourceDetector_AssemblyVersion(
    )
    {
        // Test to make sure AWS needing a prebuilt sampler doesnt mess up resourcedetectors

        // ARRANGE - a config list a webApplicationbuilder would pick up
        var config = BuildConfig(new Dictionary<string, string?>()
                {
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Resource:Detectors:0"] = "assemblyversion",
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Settings:SetErrorStatusOnException"] = "true",
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Settings:MetricLimit"] = "100",
                    [$"{SimpleOpenTelemetryOptions.SectionName}:Log:Extensions:0"] = "None",
                }
            );

        var host = Host.CreateApplicationBuilder();
        host.Configuration.AddConfiguration(config);
        host.AddSimpleOpenTelemetry();

        // ACT
        using var app = host.Build();

        // ASSERT
        app.Services.GetRequiredService<TracerProvider>(); // needed to trigger a resource build
        // Not ideal, a tad brittle, The version of the test framework
        VerifyOTELSettings(app, "service.version=18.4.0");
    }

    private IConfiguration BuildConfig(Dictionary<string, string?> dict)
    {
         return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .AddEnvironmentVariables()
            .Build();
    }

    private void VerifyOTELSettings(IHost app, string expectedResourceAttributes)
    {
        var traceResource = app.Services.GetService<TracerProvider>().GetResource();
        var meterResource = app.Services.GetService<MeterProvider>().GetResource();
        var loggerResource = app.Services.GetService<LoggerProvider>().GetResource();
        var expected = expectedResourceAttributes.Split(',').Select(pair => pair.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .ToDictionary(
            parts => parts[0].Trim(),
            parts => (object)parts[1].Trim());
        
        new List<Resource>() {traceResource, meterResource, loggerResource}.ForEach(r =>
        {
            var actual = r.Attributes
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            
            foreach (var (key, value) in expected)
            {
                Assert.True(actual.ContainsKey(key), $"Missing attribute: {key}");
                Assert.True(actual[key].ToString().Contains(value.ToString()));
            }
        });
    }

}