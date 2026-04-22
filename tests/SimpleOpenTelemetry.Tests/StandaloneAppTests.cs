using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Utils;
using Xunit;

namespace SimpleOpenTelemetryTests;

public class StandaloneAppTests
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
    public void StandaloneBootstrap_AddSimpleOpenTelemetry_CreatesSdk()
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

            var sdk = SimpleOpenTelemetry.StandaloneApp.AddSimpleOpenTelemetry(config);
            // TODO fix up this crap gen ai test
            Assert.NotNull(sdk);
        }
    }
}

