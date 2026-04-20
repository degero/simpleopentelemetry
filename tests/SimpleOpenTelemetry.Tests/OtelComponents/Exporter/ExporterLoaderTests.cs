using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Exporter;

public class ExporterLoaderTests
{
    private readonly IConfiguration _configuration;
    private readonly ExporterLoader _loader;

    public ExporterLoaderTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        _loader = new ExporterLoader(_configuration);
    }

    private static Dictionary<string, string?> GetCustomExporterConfig()
    {
        return new Dictionary<string, string?>
        {
            { "SimpleOpenTelemetry:Trace:Exporters:0:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:0:Options:Endpoint", "http://localhost:6317/" },
            { "SimpleOpenTelemetry:Trace:Exporters:0:Options:Protocol", "grpc" }
        };
    }

    
    [Fact]
    public void ConfigureExporters_WithOtlpExporter_AppliesDefaultOptionsCorrectly()
    {
        // Arrange
        var config = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Exporters = new List<SimpleOpenTelemetryExporterConfig>
                {
                    new()
                    {
                        Type = SimpleOpenTelemetryExporterType.Otlp
                    }
                }
            }
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            _loader.ConfigureExporters(r, config);
        });

        using var app = builder.Build();

        // Assert
        var monitor = app.Services.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
        var primaryOptions = monitor.Get("OTLPExporter-trace-0");
        Assert.Equal("http://localhost:4317/", primaryOptions.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.Grpc, primaryOptions.Protocol);
    }

    [Fact]
    public void ConfigureExporters_WithOtlpExporter_AppliesOptionsCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(GetCustomExporterConfig()).Build();

        var config = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Exporters = new List<SimpleOpenTelemetryExporterConfig>
                {
                    new()
                    {
                        Type = SimpleOpenTelemetryExporterType.Otlp
                    },
                    new()
                    {
                        Type = SimpleOpenTelemetryExporterType.Otlp,
                        Options = configuration.GetSection("SimpleOpenTelemetry:Trace:Exporters:0:Options")
                    }
                }
            }
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            _loader.ConfigureExporters(r, config);
        });

        using var app = builder.Build();

        // Assert
        var monitor = app.Services.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
        var primaryOptions = monitor.Get("OTLPExporter-trace-1");
        Assert.Equal("http://localhost:6317/", primaryOptions.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.Grpc, primaryOptions.Protocol);
    }


    [Fact]
    public void ConfigureExporters_WithMultipleExporters_RegistersAllExporters_AndIndependentOptions()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "SimpleOpenTelemetry:Trace:Exporters:0:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:0:Options:Endpoint", "http://localhost:8317" },
            { "SimpleOpenTelemetry:Trace:Exporters:0:Options:Protocol", "HttpProtobuf" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Endpoint", "http://localhost:6317" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Protocol", "Grpc" }
        };

        var options = new SimpleOpenTelemetryOptions();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        var section = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        section.Bind(options);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            _loader.ConfigureExporters(r, options);
        });

        using var app = builder.Build();

        // Assert
        var monitor = app.Services.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
        var exporterOne = monitor.Get("OTLPExporter-trace-0");
        Assert.NotNull(exporterOne);
        Assert.Equal("http://localhost:8317/", exporterOne.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.HttpProtobuf, exporterOne.Protocol);
        var exporterTwo = monitor.Get("OTLPExporter-trace-1");
        Assert.NotNull(exporterTwo);
        Assert.Equal("http://localhost:6317/", exporterTwo.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.Grpc, exporterTwo.Protocol);
    }
}