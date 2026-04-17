using Microsoft.Extensions.Configuration;
using Moq;
using OpenTelemetry.Exporter;
using SimpleOpenTelemetry.Builder;
using Xunit;

namespace SimpleOpenTelemetryTests;

public class SimpleOpenTelemetryOptionsTests
{
    // TODO remove this AI slop
    [Fact]
    public void SimpleOpenTelemetryExporterConfig_AllowsSettingValues()
    {
        var config = new SimpleOpenTelemetryExporterConfig
        {
            Type = SimpleOpenTelemetryExporterType.Otlp
        };

        Assert.Equal(SimpleOpenTelemetryExporterType.Otlp, config.Type);
    }

    [Fact]
    public void SimpleOpenTelemetryExporterConfig_ShouldParseOTLPOptions()
    {
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["Endpoint"]).Returns("http://localhost:6317");
        sectionMock.Setup(s => s["Protocol"]).Returns("grpc");

        var configMock = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SimpleOpenTelemetry:Trace:Exporters:0:Options:Endpoint"] = "http://localhost:6317",
                ["SimpleOpenTelemetry:Trace:Exporters:0:Options:Protocol"] = "grpc"
            })
            .Build();

        var config = new SimpleOpenTelemetryExporterConfig
        {
            Type = SimpleOpenTelemetryExporterType.Otlp,
            Options = configMock.GetSection("SimpleOpenTelemetry:Trace:Exporters:0:Options")
        };
        
        OtlpExporterOptions options = new();
        var section = config.Options;
        section.Bind(options);

        Assert.Equal(OtlpExportProtocol.Grpc, options.Protocol);
        Assert.Equal(new Uri("http://localhost:6317"), options.Endpoint);
    }}