using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();

    [Fact]
    public void ConfigureExporters_WithOtlpExporter_AppliesDefaultOptionsCorrectly()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        var (target, config) = InitExporter(new()
        {
            { "SimpleOpenTelemetry:Trace:Exporters:0:Type", "Otlp" }
        });

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
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
        var builder = Host.CreateApplicationBuilder();
        var (target, config) = InitExporter(new()
        {
            { "SimpleOpenTelemetry:Trace:Exporters:0:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Endpoint", "http://localhost:6317/" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Protocol", "grpc" }
        });

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config!);
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
        var builder = Host.CreateApplicationBuilder();
        var (target, config) = InitExporter(new()
        {
            { "SimpleOpenTelemetry:Trace:Exporters:0:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:0:Options:Endpoint", "http://localhost:8317" },
            { "SimpleOpenTelemetry:Trace:Exporters:0:Options:Protocol", "HttpProtobuf" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Type", "Otlp" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Endpoint", "http://localhost:6317" },
            { "SimpleOpenTelemetry:Trace:Exporters:1:Options:Protocol", "Grpc" }
        });

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
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
    [MemberData(nameof(GetAllKnownTraceExporters), false)]
    [MemberData(nameof(GetAllKnownTraceExporters), true)]
    public void ConfigureExporters_WithAllKnownTraceExporters_SuccessfullyRegistered(TraceExporterEnum exporterType,
        bool createOptionsEntry)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);

         // Add options if testing Skip if otlp as reflection not used
        var descriptor = ExporterAssemblies.KnownTraceExporters[exporterType];
        var setOptions = !string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(descriptor.OptionsClassName) && 
            (descriptor.optionsRequired || createOptionsEntry);
       
        IConfigurationSection? exporterConfigSection = setOptions ? null : GetExporterConfigurationSection(descriptor);

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
        var (target, _) = InitExporter([]);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
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
    [MemberData(nameof(GetAllKnownMetricExporters), false)]
    [MemberData(nameof(GetAllKnownMetricExporters), true)]
    public void ConfigureExporters_WithAllKnownMetricExporters_SuccessfullyRegistered(MetricExporterEnum exporterType,
        bool createOptionsEntry)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
         // Add options if testing Skip if otlp as reflection not used
        var descriptor = ExporterAssemblies.KnownMetricsExporters[exporterType];
        var setOptions = !string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(descriptor.OptionsClassName) && 
            (descriptor.optionsRequired || createOptionsEntry);

        IConfigurationSection? exporterConfigSection = setOptions ? null : GetExporterConfigurationSection(descriptor);

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
        var (target, _) = InitExporter([]);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithMetrics(m =>
        {
            target.ConfigureExporters(m, config);
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
    [MemberData(nameof(GetAllKnownLogExporters), false)]
    [MemberData(nameof(GetAllKnownLogExporters), true)]
    public void ConfigureExporters_WithAllKnownLogExporters_SuccessfullyRegistered(LogExporterEnum exporterType,
        bool createOptionsEntry)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
         // Add options if testing Skip if otlp as reflection not used
        var descriptor = ExporterAssemblies.KnownLogExporters[exporterType];
        var setOptions = !string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(descriptor.OptionsClassName) && 
            (descriptor.optionsRequired || createOptionsEntry);

        IConfigurationSection? exporterConfigSection = setOptions ? null : GetExporterConfigurationSection(descriptor);

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
        var (target, _) = InitExporter([]);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithLogging(l =>
        {
            target.ConfigureExporters(l, config);
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


    [Theory]
    [InlineData("AllSignals_TopLevelConfig", """
    {
        "SimpleOpenTelemetry:ExporterOptions:Azure:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "SimpleOpenTelemetry:Trace:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Log:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Metric:Exporters:0:Type": "Azure"
    }
    """, 3, false)]
    [InlineData("AllSignals_NoConfig_ShouldFail", """
    {
        "SimpleOpenTelemetry:Trace:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Log:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Metric:Exporters:0:Type": "Azure"
    }
    """, 0, true)]
    [InlineData("OnlyTrace_TopLevelConfig", """
    {
        "SimpleOpenTelemetry:ExporterOptions:Azure:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "SimpleOpenTelemetry:Trace:Exporters:0:Type": "Azure"
    }
    """, 1, false)]
    [InlineData("OnlyTrace_EntryLevelConfig", """
    {
        "SimpleOpenTelemetry:Trace:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Trace:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf"
    }
    """, 1, false)]
    [InlineData("AllSignals_AllEntryLevelOptions", """
    {
        "SimpleOpenTelemetry:Trace:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Trace:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "SimpleOpenTelemetry:Log:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Log:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "SimpleOpenTelemetry:Metric:Exporters:0:Type": "Azure",
        "SimpleOpenTelemetry:Metric:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf"
    }
    """, 3, false)]
    public void ConfigureExporters_AzureExporter_SuccessfullyRegisters(string testName, string optionsJson, int registerEvents, bool failure)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        var exporterType = LogExporterEnum.Azure;
        var builder = Host.CreateApplicationBuilder();
        var (target, config) = InitExporter(JsonSerializer.Deserialize<Dictionary<string, string?>>(optionsJson)!);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        builder.Services.AddOpenTelemetry().WithLogging(l => 
        {
            target.ConfigureExporters(l, config);
        }).WithMetrics(l =>
        {
            target.ConfigureExporters(l, config);
        }).WithTracing(l =>
        {
            target.ConfigureExporters(l, config);
        });

        // build app to ensure this loads up
        var app = builder.Build();
        
        // Assert
        var registeredSuccessEvents = listener.Events
            .Where(e => e.Level == EventLevel.Verbose &&
                    e.Payload.Any(p => (p?.ToString()?.Contains($"Registered log exporter '{exporterType}'") ?? false) ||
                    (p?.ToString()?.Contains($"Registered trace exporter '{exporterType}'") ?? false) ||
                    (p?.ToString()?.Contains($"Registered metric exporter '{exporterType}'") ?? false)));

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        Assert.Equal(registerEvents,registeredSuccessEvents.Count());
        Assert.True(failure ? errorEvents.Count() > 0 : errorEvents.Count() == 0);
    }

    public static IEnumerable<object[]> GetAllKnownTraceExporters(bool createOptions)
    {
        foreach (var exporter in Enum.GetValues<TraceExporterEnum>())
        {
            yield return new object[] { exporter, createOptions };
        }
    }

    public static IEnumerable<object[]> GetAllKnownMetricExporters(bool createOptions)
    {
        foreach (var exporter in Enum.GetValues<MetricExporterEnum>())
        {
            yield return new object[] { exporter, createOptions };
        }
    }

    public static IEnumerable<object[]> GetAllKnownLogExporters(bool createOptions)
    {
        foreach (var exporter in Enum.GetValues<LogExporterEnum>())
        {
            yield return new object[] { exporter, createOptions };
        }
    }

    private (ExporterLoader, SimpleOpenTelemetryOptions) InitExporter(Dictionary<string, string?> exporterConfig)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(exporterConfig).Build();
        var config = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName).Get<SimpleOpenTelemetryOptions>();
        var target = new ExporterLoader(configuration);
        return (target, config);
    }

    private IConfigurationSection? GetExporterConfigurationSection(ExporterExtensionDescriptor descriptor)
    {
        // Just generate a section based on the options class structure, dont set an values
        IConfigurationSection? optionsConfigSection = null;

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
        
        return optionsConfigSection;
    }
}