using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.OtelComponents.Extension;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.OtelComponents.Propagator;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.OtelComponents.Sampler;
using SimpleOpenTelemetry.Reflection;
using SimpleOpenTelemetry.Utils;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

[Collection("SimpleOpenTelemetryBuilderTests")]
public class SimpleOpenTelemetryBuilderTests : IDisposable
{
    private readonly TestEventListener _openTelemetrySdkEventListener;
    private readonly TestEventListener _simpleOpenTelemetryEventListener;

    // Default mocks reused across tests that need them
    private readonly Mock<IAssemblyExecution> _mockAssemblyExecution = new();
    private readonly Mock<IInstrumentationLoader> _mockInstrumentationLoader = new();
    private readonly Mock<IExporterLoader> _mockExporterLoader = new();
    private readonly Mock<IResourceDetectorLoader> _mockResourceDetectorLoader = new();
    private readonly Mock<ISamplerLoader> _mockSamplerLoader = new();
    private readonly Mock<IPropagatorLoader> _mockPropagatorLoader = new();
    private readonly Mock<IExtensionLoader> _mockExtensionLoader = new();
    private readonly Mock<IDistroLoader> _mockDistroLoader = new();

    public SimpleOpenTelemetryBuilderTests()
    {
        _openTelemetrySdkEventListener = new("OpenTelemetry-");
        _simpleOpenTelemetryEventListener = new();
    }

    public void Dispose()
    {
        _openTelemetrySdkEventListener.Dispose();
        _simpleOpenTelemetryEventListener.Dispose();
    }

    /// <summary>
    /// Creates a <see cref="SimpleOpenTelemetryBuilder"/> with all loader dependencies injected
    /// as mocks, with optional overrides for specific loaders under test.
    /// </summary>
    private SimpleOpenTelemetryBuilder CreateBuilder(
        IOpenTelemetryBuilder otelBuilder,
        IConfiguration config,
        IAssemblyExecution? assemblyExecution = null,
        IInstrumentationLoader? instrumentationLoader = null,
        IExporterLoader? exporterLoader = null,
        IResourceDetectorLoader? resourceDetectorLoader = null,
        ISamplerLoader? samplerLoader = null,
        IPropagatorLoader? propagatorLoader = null,
        IExtensionLoader? extensionLoader = null,
        IDistroLoader? distroLoader = null)
    {
        return new SimpleOpenTelemetryBuilder(
            otelBuilder,
            config,
            assemblyExecution       ?? _mockAssemblyExecution.Object,
            instrumentationLoader   ?? _mockInstrumentationLoader.Object,
            resourceDetectorLoader  ?? _mockResourceDetectorLoader.Object,
            exporterLoader          ?? _mockExporterLoader.Object,
            samplerLoader           ?? _mockSamplerLoader.Object,
            propagatorLoader        ?? _mockPropagatorLoader.Object,
            extensionLoader         ?? _mockExtensionLoader.Object,
            distroLoader            ?? _mockDistroLoader.Object);
    }

    [Fact]
    public void Configure_SetsUpTracing_WhenTraceExportersAreConfigured()
    {
        // ARRANGE
        var jsonConfig = @"
        {
          ""SimpleOpenTelemetry"": {
            ""Trace"": {
              ""Exporters"": [
                { ""type"": ""otlp"" }
              ]
            }
          }
        }";

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        var sotelBuilder = SimpleOpenTelemetryBuilder.Create(otelBuilder, config);

        // ACT
        sotelBuilder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var tracerProvider = services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("true")]
    public void Configure_SetsUpTraceSettings_SetErrorStatusOnException_WhenConfigured(string setError)
    {
        // ARRANGE
        var activitySourceName = "Configure_SetsUpTraceSettings_SetErrorStatusOnException_WhenConfigured";
        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        var configDict = new Dictionary<string, string?>()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Sources:0"] = activitySourceName
            };

        if (setError == "true")
        {
           configDict.Add($"{SimpleOpenTelemetryOptions.SectionName}:Trace:Settings:SetErrorStatusOnException", setError);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var tracerProvider = services.GetRequiredService<TracerProvider>();
        Activity? activity = null;

        try
        {
            using var activitySource = new ActivitySource(activitySourceName);
            using (activity = activitySource.StartActivity("Activity"))
            {
                throw new InvalidOperationException("Oops!");
            }
        }
        catch {}

        Assert.NotNull(activity);
        if (setError == "true")
        {
            Assert.Equal(StatusCode.Error, activity.GetStatus().StatusCode);
            Assert.Equal(ActivityStatusCode.Error, activity.Status);
        }
        else
        {
            Assert.Null(activity.GetStatus());
        }
    }

    [Theory]
    [InlineData("MetricLimit", "10", "10")]
    [InlineData("MetricLimit", null, "1000")] // The default is 1000
    public void Configure_SetsUpMetricSettings_WhenConfigured(string setting, string? value,
        string expectedValue)
    {
        // ARRANGE
        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        var configDict = new Dictionary<string, string?>()
        {
           [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Exporters:0:Type"] = "console"
        };
        if (value is not null)
            configDict.Add($"{SimpleOpenTelemetryOptions.SectionName}:Metric:Settings:{setting}", value);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var _ = services.GetRequiredService<MeterProvider>();

        var match = _openTelemetrySdkEventListener.Events.FirstOrDefault(e =>
            e.EventSource.Name == "OpenTelemetry-Sdk" &&
            e.Payload != null &&
            e.Payload.Any(p => p?.ToString()?.Contains($"MetricLimit={expectedValue}") == true));

        Assert.NotNull(match);
    }

    [Theory]
    [InlineData("Test,Configure_Calls_TracingAddSource_When_TraceSources_InConfig", "Test", true)]
    [InlineData("Test,Configure_Calls_TracingAddSource_When_TraceSources_InConfig", "Configure_Calls_TracingAddSource_When_TraceSources_InConfig", true)]
    [InlineData("","",false)]
    public void Configure_Calls_TracingAddSource_When_TraceSources_InConfig(
        string sources,
        string activitySourceName,
        bool isSet)
    {
        // ARRANGE
        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        var configDict = new Dictionary<string, string?>()
        {
           [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:Type"] = "console"
        };
        if (isSet)
        {
            var sourceList = sources.Split(',').ToList();
            for (var i=0; i < sourceList.Count; i++)
                configDict.Add($"{SimpleOpenTelemetryOptions.SectionName}:Trace:Sources:{i}", sourceList[i]);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict).Build();

        var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var tracerProvider = services.GetRequiredService<TracerProvider>();

        Activity? activity = null;
        try
        {
            using var activitySource = new ActivitySource(activitySourceName);
            using (activity = activitySource.StartActivity("Activity"))
            {
                Console.Write("");
            }
        }
        catch{}

        if (isSet)
            Assert.NotNull(activity);
        else
            Assert.Null(activity);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Configure_Calls_MetricsAddMeter_When_MetricCustomMeters_InConfig(bool isSet)
    {
        // ARRANGE
        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        List<Metric> exportedMetrics = new();
        otelBuilder.WithMetrics(r => r.AddInMemoryExporter(exportedMetrics));

        var configDict = new Dictionary<string, string?>()
        {
           [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Exporters:0:Type"] = "console"
        };

        if (isSet)
            configDict.Add($"{SimpleOpenTelemetryOptions.SectionName}:Metric:CustomMeters:0", "MyTestMeter");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict).Build();

        var builder = SimpleOpenTelemetryBuilder.Create(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var meterProvider = services.GetRequiredService<MeterProvider>();

        var meter = new Meter("MyTestMeter");
        var counter = meter.CreateCounter<int>("requests");
        counter.Add(1);

        meterProvider.ForceFlush();
        if (isSet)
        {
            Assert.Single(exportedMetrics);
            Assert.Equal("requests", exportedMetrics[0].Name);
        }
        else
            Assert.Empty(exportedMetrics);
    }

    [Theory]
    [InlineData(@"""Settings"": { ""IncludeFormattedMessage"": true, ""IncludeScopes"": true, ""ParseStateValues"": true }", true, true, true)]
    [InlineData(@"""Settings"": { ""IncludeFormattedMessage"": true }", true, false, false)]
    [InlineData(@"""Exporters"": []", false, false, false)]
    public void Configure_SetsUpLoggingOptions_WhenConfigured(string jsonSeg, bool formatMsg, bool inclScope, bool parseState)
    {
        // ARRANGE
        var jsonConfig = $@"{{ ""SimpleOpenTelemetry"": {{ ""Log"": {{ {jsonSeg} }} }} }}";
        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();

        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        var sotelBuilder = SimpleOpenTelemetryBuilder.Create(otelBuilder, config);

        // ACT
        sotelBuilder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;
        Assert.Equal(formatMsg, options.IncludeFormattedMessage);
        Assert.Equal(inclScope, options.IncludeScopes);
        Assert.Equal(parseState, options.ParseStateValues);
    }

    // -------------------------------------------------------------------------
    // Tests that verify loader interactions use CreateBuilder() with mock loaders.
    // -------------------------------------------------------------------------

    [Fact]
    public void Configure_WithDistroSet_LoadsDistroAndReturnsEarly()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Distro"] = "AzureMonitorAspNetCore"
            })
            .Build();

        _mockDistroLoader
            .Setup(d => d.LoadDistro(It.IsAny<IOpenTelemetryBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()))
            .Returns(true);

        var builder = CreateBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT: distro loader called once
        _mockDistroLoader.Verify(d => d.LoadDistro(otelBuilder, It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);

        // ASSERT: all other loaders skipped due to early return
        _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(),   It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>(), It.IsAny<MetricInstrumentationEnum>()), Times.Never);
        _mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>(), It.IsAny<TraceInstrumentationEnum>()), Times.Never);
        _mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(),           It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockExtensionLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Never);
        _mockExtensionLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(),  It.IsAny<TraceExtensionsEnum>()), Times.Never);
    }

    [Fact]
    public void Configure_WithNoDistroSet_And_NoSignalConfiguration_DoesNot_CallDistroLoader_And_DependentSignalLoaderServices()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                [SimpleOpenTelemetryOptions.SectionName] = "{}"
            })
            .Build();

        _mockDistroLoader
            .Setup(d => d.LoadDistro(It.IsAny<IOpenTelemetryBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()))
            .Returns(false);

        var builder = CreateBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        _mockDistroLoader.Verify(d => d.LoadDistro(otelBuilder, It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockAssemblyExecution.Verify(r => r.GetAssembly(It.IsAny<string>()), Times.Never);

        _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(),   It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>(), It.IsAny<MetricInstrumentationEnum>()), Times.Never);
        _mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>(), It.IsAny<TraceInstrumentationEnum>()), Times.Never);
        _mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(),           It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        _mockExtensionLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Never);
        _mockExtensionLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(),  It.IsAny<TraceExtensionsEnum>()), Times.Never);
    }

    [Fact]
    public void Configure_WithNoDistroSet_And_NoSignalConfiguration_And_WithResourceDetectorConfiguration_Calls_ResourceLoader()
    {
        // ARRANGE
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Settings:SetErrorStatusOnException"] = "true"
            }
        ).Build();
        var serviceColl = new ServiceCollection();
        var otelBuilder = serviceColl.AddOpenTelemetry();

        var builder = CreateBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = serviceColl.BuildServiceProvider();
        var trace = services.GetService<TracerProvider>(); // triggers ResourceBuilder invocation
        _mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetAllSignalTypeConfigs))]
    public void Configure_WithNoDistroSet_And_SignalSettings_Calls_Loaders_Instrumentation_Exporters_Extensions(
        string signal,
        Dictionary<string, string?> signalConfig)
    {
        // ARRANGE
        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(signalConfig)
            .Build();

        var builder = CreateBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        if (signal == "metric")
        {
            _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            _mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>(), It.IsAny<MetricInstrumentationEnum>()), Times.Once);
            _mockExtensionLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Once);
        }
        if (signal == "trace")
        {
            _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            _mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>(), It.IsAny<TraceInstrumentationEnum>()), Times.Once);
            _mockExtensionLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(),  It.IsAny<TraceExtensionsEnum>()), Times.Once);
            _mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(),           It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            _mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
        }
        if (signal == "log")
        {
            _mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(),  It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            _mockExtensionLoader.Verify(e => e.AddLogExtension(It.IsAny<LoggerProviderBuilder>(),    It.IsAny<LogExtensionsEnum>()), Times.Once);
        }
    }

    [Fact]
    public void Configure_Will_LogErrorEvent_And_Terminate_When_NoSimpleOpenTelemetryConfigSection_Exists()
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var otelBuilder = services.AddOpenTelemetry();
        var builder = CreateBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        var events = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"No configuration section '{SimpleOpenTelemetryOptions.SectionName}'. This is required for SimpleOpenTelemetry.") ?? false));

        Assert.NotNull(events);
    }

    [Fact]
    public void Configure_Will_LogErrorEvent_And_Terminate_When_NoSimpleOpenTelemetryConfig_SignalSubSection_Exists()
    {
        // ARRANGE
        Assert.Empty(_simpleOpenTelemetryEventListener.Events);
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}"] = "{}"
            })
            .Build();

        var otelBuilder = services.AddOpenTelemetry();
        var builder = CreateBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        var events = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            e.Payload.Any(p => p?.ToString()?.Contains($"Missing signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.") ?? false));

        Assert.NotNull(events);
    }

    /// <summary>
    /// Gets config dictionary with settings to trigger off all Loaders for that signal
    /// </summary>
    public static IEnumerable<object[]> GetAllSignalTypeConfigs()
    {
        var configs = new Dictionary<string, Dictionary<string, string?>>()
        {
            ["trace"] = new()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Exporters:0:type"] = "console",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Instrumentations:0"] = "HttpClient",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Sources:0"] = "test",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Sampler"] = "test",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Propagators"] = "test",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Trace:Extensions:0"] = "AWSXRayTraceId"
            },
            ["log"] = new()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Log:Exporters:0:type"] = "console",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Log:Extensions:0"] = "None"
            },
            ["metric"] = new()
            {
                [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Exporters:0:type"] = "console",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Instrumentations:0"] = "Runtime",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:Extensions:0"] = "None",
                [$"{SimpleOpenTelemetryOptions.SectionName}:Metric:CustomMeters:0"] = "test"
            },
        };

        foreach (var key in configs.Keys)
        {
            yield return new object[] { key, configs[key] };
        }
    }
}