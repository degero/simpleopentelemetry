using System.Diagnostics.Tracing;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Amazon.Runtime.Endpoints;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.OtelComponents.Exporter;

[Collection("ExporterLoaderTests")]
public class ExporterLoaderTests : IDisposable
{
    private readonly TestEventListener _listener;
    private readonly AssemblyExecution _assemblyExec = new AssemblyExecution();

    public ExporterLoaderTests()
    {
        _listener = new();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact]
    public void ConfigureExporters_WithOtlpExporter_AppliesDefaultOptionsCorrectly()
    {
        // Arrange
        Assert.Empty(_listener.Events);

        var (target, config) = InitExporter(new()
        {
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Type", "Otlp" }
        });
        var services = new ServiceCollection();

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
        });

        using var sp = services.BuildServiceProvider();

        // Assert
        var monitor = sp.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
        var primaryOptions = monitor.Get("OTLPExporter-trace-0");
        Assert.Equal("http://localhost:4317/", primaryOptions.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.Grpc, primaryOptions.Protocol);
    }

    [Fact]
    public void ConfigureExporters_WithOtlpExporter_AppliesCustomOptionsCorrectly()
    {
        // Arrange
        Assert.Empty(_listener.Events);

        var (target, config) = InitExporter(new()
        {
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Type", "Otlp" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:1:Type", "Otlp" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:1:Options:Endpoint", "http://localhost:6317/" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:1:Options:Protocol", "grpc" }
        });

        var services = new ServiceCollection();
        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config!);
        });

        using var sp = services.BuildServiceProvider();

        // Assert
        var monitor = sp.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
        var primaryOptions = monitor.Get("OTLPExporter-trace-1");
        Assert.Equal("http://localhost:6317/", primaryOptions.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.Grpc, primaryOptions.Protocol);
    }

    [Fact]
    public void ConfigureExporters_WithMultipleExporters_RegistersAllExporters_AndIndependentOptions()
    {
        // Arrange
        Assert.Empty(_listener.Events);

        var (target, config) = InitExporter(new()
        {
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Type", "Otlp" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Options:Endpoint", "http://localhost:8317" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Options:Protocol", "HttpProtobuf" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:1:Type", "Otlp" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:1:Options:Endpoint", "http://localhost:6317" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:1:Options:Protocol", "Grpc" }
        });
        var services = new ServiceCollection();

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
        });

        using var sp = services.BuildServiceProvider();

        // Assert
        var monitor = sp.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
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
    [InlineData(false)]
    [InlineData(true)]
    public void ConfigureExporters_WithExportersOptions_OverridenBySignalEntry_Options(bool signalOverride)
    {
        // Arrange
        Assert.Empty(_listener.Events);

        var originalEndpoint = "http://localhost:1317/";
        var endpointOverride = "http://localhost:8317/";

        var dict = new Dictionary<string, string?>()
        {
            { $"{SimpleOpenTelemetryOptions.SectionName}:ExporterOptions:Otlp:Endpoint", originalEndpoint },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Type", "Otlp" },
            { $"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Options:Protocol", "HttpProtobuf" },
        };
        if (signalOverride)
            dict.Add($"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Options:Endpoint", endpointOverride);

        var (target, config) = InitExporter(dict);

        var services = new ServiceCollection();

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
        });
        // Assert

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<TracerProvider>();

        var monitor = sp.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
        var exporterOne = monitor.Get("OTLPExporter-trace-0");
        Assert.NotNull(exporterOne);
        Assert.Equal(signalOverride ? endpointOverride : originalEndpoint, exporterOne.Endpoint.ToString());
        Assert.Equal(OtlpExportProtocol.HttpProtobuf, exporterOne.Protocol);
    }

    [Theory]
    [MemberData(nameof(GetAllKnownTraceExporters), false)]
    [MemberData(nameof(GetAllKnownTraceExporters), true)]
    public void ConfigureExporters_WithAllKnownTraceExporters_SuccessfullyRegistered(TraceExporterEnum exporterType,
        bool createOptionsEntry)
    {
        // Arrange
        Assert.Empty(_listener.Events);

        // Add options if testing Skip if otlp as reflection not used
        var descriptor = !string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) ? 
            ExporterAssemblies.KnownTraceExporters[exporterType] : null;

        var setOptions = descriptor is not null &&
            !string.IsNullOrWhiteSpace(descriptor.OptionsClassName) && 
            (descriptor.OptionsRequired || createOptionsEntry);
       
        IConfigurationSection? exporterConfigSection = setOptions ? GetExporterConfigurationSection(descriptor!) : null;

        var exporterConfig = new SimpleOpenTelemetryExporterConfig<TraceExporterEnum>
        {
            Type = exporterType.ToString(),
            Options = exporterConfigSection
        };

        var config = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Exporters = [ exporterConfig ]
            }
        };

        var services = new ServiceCollection();
        var (target, _) = InitExporter([]);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithTracing(r =>
        {
            target.ConfigureExporters(r, config);
        });

        // Assert
        var registeredSuccessEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                    (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Exporter '{exporterType}' for builder '{nameof(TracerProviderBuilder)}'") ?? false) ?? false));

        var errorEvents = _listener.Events
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
        Assert.Empty(_listener.Events);

        // Add options if testing Skip if otlp as reflection not used
        var descriptor = !string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) ? 
            ExporterAssemblies.KnownMetricExporters[exporterType] : null;

        var setOptions = descriptor is not null &&
            !string.IsNullOrWhiteSpace(descriptor.OptionsClassName) && 
            (descriptor.OptionsRequired || createOptionsEntry);

        IConfigurationSection? exporterConfigSection = setOptions ? GetExporterConfigurationSection(descriptor!) : null;

        var exporterConfig = new SimpleOpenTelemetryExporterConfig<MetricExporterEnum>
        {
            Type = exporterType.ToString(),
            Options = exporterConfigSection
        };

        var config = new SimpleOpenTelemetryOptions
        {
            Metric = new SimpleOpenTelemetryMetricOptions
            {
                Exporters = [ exporterConfig ]
            }
        };

        var services = new ServiceCollection();
        var (target, _) = InitExporter([]);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithMetrics(m =>
        {
            target.ConfigureExporters(m, config);
        });

        // Assert
        var registeredSuccessEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                    (e.Payload?.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Exporter '{exporterType}' for builder '{nameof(MeterProviderBuilder)}'") ?? false) ?? false));

        var errorEvents = _listener.Events
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
        Assert.Empty(_listener.Events);
        
        
        // Add options if testing Skip if otlp as reflection not used
        var descriptor = !string.Equals(nameof(TraceExporterEnum.Otlp), exporterType.ToString(), StringComparison.OrdinalIgnoreCase) ? 
            ExporterAssemblies.KnownLogExporters[exporterType] : null;

        var setOptions = descriptor is not null &&
            !string.IsNullOrWhiteSpace(descriptor.OptionsClassName) && 
            (descriptor.OptionsRequired || createOptionsEntry);

        IConfigurationSection? exporterConfigSection = setOptions ? GetExporterConfigurationSection(descriptor!) : null;

        var exporterConfig =  new SimpleOpenTelemetryExporterConfig<LogExporterEnum>
        {
            Type = exporterType.ToString(),
            Options = exporterConfigSection
        };

        var config = new SimpleOpenTelemetryOptions
        {
            Log = new SimpleOpenTelemetryLogOptions
            {
                Exporters = [ exporterConfig ]
            }
        };

        var services = new ServiceCollection();
        var (target, _) = InitExporter([]);

        services.AddOpenTelemetry().WithLogging(r => r.AddAzureMonitorLogExporter());
        

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithLogging(l =>
        {
            target.ConfigureExporters(l, config);
        });

        // Assert
        var registeredSuccessEvent = _listener.Events
            .FirstOrDefault(e => e.Level == EventLevel.Verbose &&
                    e.Payload != null &&
                    e.Payload.Any(p => p?.ToString()?.Contains($"Registered OpenTelemetry Exporter '{exporterType}' for builder '{nameof(LoggerProviderBuilder)}'") ?? false));

        var errorEvents = _listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        Assert.NotNull(registeredSuccessEvent);
        Assert.Empty(errorEvents);
    }


    [Theory]
    [InlineData("AllSignals_TopLevelConfig", """
    {
        "ExporterOptions:Azure:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "Trace:Exporters:0:Type": "Azure",
        "Log:Exporters:0:Type": "Azure",
        "Metric:Exporters:0:Type": "Azure"
    }
    """, 3, false)]
    [InlineData("AllSignals_NoConfig_ShouldFail", """
    {
        "Trace:Exporters:0:Type": "Azure",
        "Log:Exporters:0:Type": "Azure",
        "Metric:Exporters:0:Type": "Azure"
    }
    """, 0, true)]
    [InlineData("OnlyTrace_TopLevelConfig", """
    {
        "ExporterOptions:Azure:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "Trace:Exporters:0:Type": "Azure"
    }
    """, 1, false)]
    [InlineData("OnlyTrace_EntryLevelConfig", """
    {
        "Trace:Exporters:0:Type": "Azure",
        "Trace:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf"
    }
    """, 1, false)]
    [InlineData("AllSignals_AllEntryLevelOptions", """
    {
        "Trace:Exporters:0:Type": "Azure",
        "Trace:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "Log:Exporters:0:Type": "Azure",
        "Log:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf",
        "Metric:Exporters:0:Type": "Azure",
        "Metric:Exporters:0:Options:ConnectionString": "InstrumentationKey=asdfasdf;IngestionEndpoint=https://asdfasdff.applicationinsights.azure.com/;LiveEndpoint=https://asdfasdf.livediagnostics.monitor.azure.com/;ApplicationId=asdfasdf"
    }
    """, 3, false)]
    public void ConfigureExporters_AzureExporter_SuccessfullyRegisters(string testName, string optionsJson, int registerEvents, bool failure)
    {
        // Arrange
        var item = testName;
        Assert.Empty(_listener.Events);
        var exporterType = LogExporterEnum.Azure;
        var services = new ServiceCollection();

        var (target, config) = InitExporter(JsonSerializer.Deserialize<Dictionary<string, string?>>(optionsJson)!);

        // Act
        // This is what AddSimpleOpenTelemetry() is doing but 
        // done manually to isolate closer to the SUT
        services.AddOpenTelemetry().WithLogging(l => 
        {
            target.ConfigureExporters(l, config);
        }).WithMetrics(l =>
        {
            target.ConfigureExporters(l, config);
        }).WithTracing(l =>
        {
            target.ConfigureExporters(l, config);
        });

        // Assert
        var registeredSuccessEvents = _listener.Events
            .Where(e => e.Level == EventLevel.Verbose &&
                    e.Payload != null &&
                    e.Payload.Any(p => (p?.ToString()?.Contains($"Registered OpenTelemetry Exporter '{exporterType}' for builder '{nameof(LoggerProviderBuilder)}'") ?? false) ||
                    (p?.ToString()?.Contains($"Registered OpenTelemetry Exporter '{exporterType}' for builder '{nameof(TracerProviderBuilder)}'") ?? false) ||
                    (p?.ToString()?.Contains($"Registered OpenTelemetry Exporter '{exporterType}' for builder '{nameof(MeterProviderBuilder)}'") ?? false)));

        var errorEvents = _listener.Events
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
        var prefixedDict = exporterConfig.ToDictionary(
            r => r.Key.StartsWith(SimpleOpenTelemetryOptions.SectionName) ? r.Key : $"{SimpleOpenTelemetryOptions.SectionName}:{r.Key}",
            r => r.Value
        );

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(prefixedDict).Build();
        var config = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName).Get<SimpleOpenTelemetryOptions>();
        var target = new ExporterLoader(_assemblyExec);
        return (target, config)!;
    }

    private IConfigurationSection? GetExporterConfigurationSection(AssemblyDescriptor descriptor)
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