using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    public void AddSimpleOpenTelemetry_DoesNotConfigureBuilderWhenSimpleOpenTelemetrySectionIsMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSimpleOpenTelemetry(config);

        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<TracerProvider>());
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
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

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
            var config = BuildConfigWithOtelValues(serviceName, resourceAttributes);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSimpleOpenTelemetry(config);

            var provider = services.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => provider.SimpleOpenTelemetryValidate());
        }
    }
}

