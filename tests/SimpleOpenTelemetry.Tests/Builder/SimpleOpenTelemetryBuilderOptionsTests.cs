using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Builder;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

public class SimpleOpenTelemetryBuilderOptionsTests
{
    [Fact]
    public void SimpleOpenTelemetryExporterConfig_AllowsSettingValues()
    {
        var endpoint = new Uri("http://localhost:4317");
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Options:Endpoint", "http://localhost:4317" },
                { "Options:Protocol", "Grpc" },
                { "Options:Headers", "api-key=test" },
                { "Options:TimeoutMilliseconds", "5000" }
            });
        var root = configBuilder.Build();
        var optionsSection = root.GetSection("Options");
        
        var config = new SimpleOpenTelemetryExporterConfig
        {
            Type = SimpleOpenTelemetryExporterType.Otlp,
            Options = optionsSection
        };

        Assert.Equal(SimpleOpenTelemetryExporterType.Otlp, config.Type);
    }

}