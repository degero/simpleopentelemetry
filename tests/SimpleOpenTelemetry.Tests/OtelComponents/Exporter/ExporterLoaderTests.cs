using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Diagnostics;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Exporter;

public class ExporterLoaderTests
{
    private readonly IConfiguration _configuration;
    private readonly ExporterLoader _loader;

    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();

    public ExporterLoaderTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        _loader = new ExporterLoader(_configuration);
    }

    [Fact]
    public void ConfigureExporters_WithOtlpExporter_AppliesDefaultOptionsCorrectly()
    {
        // Arrange
        var config = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Exporters = new List<SimpleOpenTelemetryExporterConfig<TraceExporterEnum>>
                {
                    new()
                    {
                        Type = TraceExporterEnum.Otlp
                    }
                }
            }
        };

        var builder = Host.CreateApplicationBuilder();

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
    public void ConfigureExporters_WithOtlpExporter_AppliesCustomOptionsCorrectly()
    {
        // Arrange
        var exporterConfig = new Dictionary<string, string?>
        {
            { "SimpleOpenTelemetry:Trace:Exporters:0:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Endpoint", "http://localhost:6317/" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Protocol", "grpc" }
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(exporterConfig).Build();
        var config = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName).Get<SimpleOpenTelemetryOptions>();

        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            _loader.ConfigureExporters(r, config!);
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

        var builder = Host.CreateApplicationBuilder();

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

    [Theory]
    [MemberData(nameof(GetAllKnownTraceExporters))]
    public void ConfigureExporters_WithAllKnownTraceExporters_SuccessfullyRegistered(TraceExporterEnum exporterType)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);

        // Skip if otlp as reflection not used
        IConfigurationSection? exporterConfigSection = string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) ?
            null
            :  GetExporterConfigurationSection(ExporterAssemblies.KnownTraceExporters[exporterType]);

        var exporterConfig = new SimpleOpenTelemetryExporterConfig<TraceExporterEnum>
        {
            Type = exporterType,
            Options = exporterConfigSection
        };

        var config = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Exporters = [ exporterConfig ]
            }
        };

        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            _loader.ConfigureExporters(r, config);
        });

        // Assert
        var registeredSuccessEvent = listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                    e.Payload.Any(p => p?.ToString()?.Contains($"Registered trace exporter '{exporterType}'") ?? false));

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        Assert.NotNull(registeredSuccessEvent);
        Assert.Empty(errorEvents);
        

    }

    [Theory]
    [MemberData(nameof(GetAllKnownMetricExporters))]
    public void ConfigureExporters_WithAllKnownMetricExporters_SuccessfullyRegistered(MetricExporterEnum exporterType)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
        // Skip if otlp as reflection not used
        IConfigurationSection? exporterConfigSection = string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) ?
            null
            : GetExporterConfigurationSection(ExporterAssemblies.KnownMetricsExporters[exporterType]);

        var exporterConfig = new SimpleOpenTelemetryExporterConfig<MetricExporterEnum>
        {
            Type = exporterType,
            Options = exporterConfigSection
        };

        var config = new SimpleOpenTelemetryOptions
        {
            Metric = new SimpleOpenTelemetryMetricOptions
            {
                Exporters = [ exporterConfig ]
            }
        };

        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithMetrics(m =>
        {
            _loader.ConfigureExporters(m, config);
        });

        // Assert
        var registeredSuccessEvent = listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                    e.Payload.Any(p => p?.ToString()?.Contains($"Registered metric exporter '{exporterType}'") ?? false));

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        Assert.NotNull(registeredSuccessEvent);
        Assert.Empty(errorEvents);
    }

    [Theory]
    [MemberData(nameof(GetAllKnownLogExporters))]
    public void ConfigureExporters_WithAllKnownLogExporters_SuccessfullyRegistered(LogExporterEnum exporterType)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
        // Skip if otlp as reflection not used
        IConfigurationSection? exporterConfigSection = string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) ?
            null
            : GetExporterConfigurationSection(ExporterAssemblies.KnownLogExporters[exporterType]);


        var exporterConfig =  new SimpleOpenTelemetryExporterConfig<LogExporterEnum>
        {
            Type = exporterType,
            Options = exporterConfigSection
        };

        var config = new SimpleOpenTelemetryOptions
        {
            Log = new SimpleOpenTelemetryLogOptions
            {
                Exporters = [ exporterConfig ]
            }
        };

        var builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithLogging(l =>
        {
            _loader.ConfigureExporters(l, config);
        });

        // Assert
        var registeredSuccessEvent = listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                    e.Payload.Any(p => p?.ToString()?.Contains($"Registered log exporter '{exporterType}'") ?? false));

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        Assert.NotNull(registeredSuccessEvent);
        Assert.Empty(errorEvents);
    }



    public static IEnumerable<object[]> GetAllKnownTraceExporters()
    {
        foreach (var exporter in Enum.GetValues<TraceExporterEnum>())
        {
            yield return new object[] { exporter };
        }
    }

    public static IEnumerable<object[]> GetAllKnownMetricExporters()
    {
        foreach (var exporter in Enum.GetValues<MetricExporterEnum>())
        {
            yield return new object[] { exporter };
        }
    }

    public static IEnumerable<object[]> GetAllKnownLogExporters()
    {
        foreach (var exporter in Enum.GetValues<LogExporterEnum>())
        {
            yield return new object[] { exporter };
        }
    }

    private IConfigurationSection? GetExporterConfigurationSection(ExporterExtensionDescriptor descriptor)
    {
        // Just generate a section based on the options class structure
        
        IConfigurationSection? optionsConfigSection = null;

        // Add options if mandatory
        if (descriptor.optionsRequired)
        {

            var className = descriptor.OptionsClassName;
            var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName);
            var classDef = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == className)!;

            var ctor = classDef.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            var instance = ctor!.Invoke(null);

            var inner = JsonSerializer.Serialize(instance, classDef);
            var wrapped = $"{{\"{classDef.Name}\": {inner}}}";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(wrapped));
            IConfiguration classOptionsBuilder = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            optionsConfigSection = classOptionsBuilder.GetSection(classDef.Name);
        }
        return optionsConfigSection;
    }
}