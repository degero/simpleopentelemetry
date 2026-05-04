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
using System.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

[Collection("SimpleOpenTelemetryBuilderTests")]
public class SimpleOpenTelemetryBuilderTests : IDisposable
{
    private readonly TestEventListener _openTelemetrySdkEventListener;
    
    public SimpleOpenTelemetryBuilderTests()
    {
        _openTelemetrySdkEventListener = new("OpenTelemetry-");
    }

    public void Dispose()
    {
        _openTelemetrySdkEventListener.Dispose();
    }

    [Fact]
    public void Configure_SetsUpTracing_WhenTraceExportersAreConfigured()
    {
        // ARRANGE: Configuration with trace exporters (using OTLP which is built-in)
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
        var sotelBuilder = new SimpleOpenTelemetryBuilder(otelBuilder,config);
       
        // ACT
        sotelBuilder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();

        // ASSERT: TracerProvider should be registered when Trace exporters are configured
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
                ["SimpleOpenTelemetry:Trace:Sources:0"] = activitySourceName
            };

        if (setError == "true")
        {
           configDict.Add("SimpleOpenTelemetry:Trace:Settings:SetErrorStatusOnException", setError);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var tracerProvider = services.GetRequiredService<TracerProvider>();
        // not ideal to assert, but the otel sdk providers / provider builders aren't too transparent
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
           ["SimpleOpenTelemetry:Metric:Exporters:0:Type"] = "console"
        };
        if (value is not null)
            configDict.Add($"SimpleOpenTelemetry:Metric:Settings:{setting}",value);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var _ = services.GetRequiredService<MeterProvider>();

        // not ideal to assert, but the otel sdk providers / provider builders aren't too transparent
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
           ["SimpleOpenTelemetry:Trace:Exporters:0:Type"] = "console"
        };
        if (isSet)
        {
            var sourceList = sources.Split(',').ToList();
            for (var i=0; i < sourceList.Count; i++)
                configDict.Add($"SimpleOpenTelemetry:Trace:Sources:{i}", sourceList[i]);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict).Build();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

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
        {
            Assert.NotNull(activity);
        }
        else
        {
            Assert.Null(activity);
        }
    }

    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Configure_Calls_MetricsAddMeter_When_MetricCustomMeters_InConfig(
        bool isSet
    )
    {
        // ARRANGE
        var servicesCollection = new ServiceCollection();
        var otelBuilder = servicesCollection.AddOpenTelemetry();
        List<Metric> exportedMetrics = new();
        otelBuilder.WithMetrics(r => r.AddInMemoryExporter(exportedMetrics));

        var configDict = new Dictionary<string, string?>()
        {
           ["SimpleOpenTelemetry:Metric:Exporters:0:Type"] = "console"
        };

        if (isSet)
            configDict.Add("SimpleOpenTelemetry:Metric:CustomMeters:0", "MyTestMeter");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict).Build();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);


        // ACT
        builder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();
        var meterProvider = services.GetRequiredService<MeterProvider>();

        // not ideal to assert, but the otel sdk providers / provider builders aren't too transparent
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
        var sotelBuilder = new SimpleOpenTelemetryBuilder(otelBuilder,config);

        // ACT
        sotelBuilder.Configure();

        // ASSERT
        using var services = servicesCollection.BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;
        Assert.Equal(formatMsg, options.IncludeFormattedMessage);
        Assert.Equal(inclScope, options.IncludeScopes);
        Assert.Equal(parseState, options.ParseStateValues);
    }

    [Fact]
    public void Configure_WithDistroSet_LoadsDistroAndReturnsEarly()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SimpleOpenTelemetry:Distro"] = "AzureMonitorAspNetCore"
            })
            .Build();

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

        // Use reflection to mock private fields for testing
        var distroLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_distroLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockDistroLoader = new Mock<IDistroLoader>();
        mockDistroLoader.Setup(d => d.LoadDistro(It.IsAny<IOpenTelemetryBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>())).Returns(true);
        distroLoaderField?.SetValue(builder, mockDistroLoader.Object);

        var exporterLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_exporterLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExporterLoader = new Mock<IExporterLoader>();
        exporterLoaderField?.SetValue(builder, mockExporterLoader.Object);

        var instrumentationLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_instrumentationLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        Mock<IInstrumentationLoader> mockInstrumentationLoader = new Mock<IInstrumentationLoader>();
        instrumentationLoaderField?.SetValue(builder, mockInstrumentationLoader.Object);

        var resourceDetectorLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_resourceDetectorLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockResourceDetectorLoader = new Mock<IResourceDetectorLoader>();
        resourceDetectorLoaderField?.SetValue(builder, mockResourceDetectorLoader.Object);

        var samplerLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_samplerLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockSamplerLoader = new Mock<ISamplerLoader>();
        samplerLoaderField?.SetValue(builder, mockSamplerLoader.Object);

        var propagatorLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_propagatorLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockPropagatorLoader = new Mock<IPropagatorLoader>();
        propagatorLoaderField?.SetValue(builder, mockPropagatorLoader.Object);

        var extensionsLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_extensionsLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExtensionsLoader = new Mock<IExtensionLoader>();
        extensionsLoaderField?.SetValue(builder, mockExtensionsLoader.Object);

        // ACT
        builder.Configure();

        // ASSERT
        mockDistroLoader.Verify(d => d.LoadDistro(otelBuilder, It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);

        // Verify that other loaders' methods are not called (indicating early return)
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricInstrumentationEnum>()), Times.Never);
        mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceInstrumentationEnum>()), Times.Never);
        mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExtensionsLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Never);
        mockExtensionsLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceExtensionsEnum>()), Times.Never);
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

        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

        // Use reflection to mock private fields for testing
        var distroLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_distroLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockDistroLoader = new Mock<IDistroLoader>();
        mockDistroLoader.Setup(d => d.LoadDistro(It.IsAny<IOpenTelemetryBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>())).Returns(true);
        distroLoaderField?.SetValue(builder, mockDistroLoader.Object);

        var assemblyExecutionField = typeof(SimpleOpenTelemetryBuilder).GetField("_assemblyExecution", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockAssemblyExecution = new Mock<IAssemblyExecution>();
        assemblyExecutionField?.SetValue(builder, mockAssemblyExecution.Object);

        var exporterLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_exporterLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExporterLoader = new Mock<IExporterLoader>();
        exporterLoaderField?.SetValue(builder, mockExporterLoader.Object);

        var instrumentationLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_instrumentationLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        Mock<IInstrumentationLoader> mockInstrumentationLoader = new Mock<IInstrumentationLoader>();
        instrumentationLoaderField?.SetValue(builder, mockInstrumentationLoader.Object);

        var resourceDetectorLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_resourceDetectorLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockResourceDetectorLoader = new Mock<IResourceDetectorLoader>();
        resourceDetectorLoaderField?.SetValue(builder, mockResourceDetectorLoader.Object);

        var samplerLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_samplerLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockSamplerLoader = new Mock<ISamplerLoader>();
        samplerLoaderField?.SetValue(builder, mockSamplerLoader.Object);

        var propagatorLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_propagatorLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockPropagatorLoader = new Mock<IPropagatorLoader>();
        propagatorLoaderField?.SetValue(builder, mockPropagatorLoader.Object);

        var extensionsLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_extensionsLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExtensionsLoader = new Mock<IExtensionLoader>();
        extensionsLoaderField?.SetValue(builder, mockExtensionsLoader.Object);

        // ACT
        builder.Configure();

        // ASSERT

        mockDistroLoader.Verify(d => d.LoadDistro(otelBuilder, It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
        mockAssemblyExecution.Verify(r => r.GetAssembly(It.IsAny<string>()), Times.Never);

        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricInstrumentationEnum>()), Times.Never);
        mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceInstrumentationEnum>()), Times.Never);
        mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExtensionsLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Never);
        mockExtensionsLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceExtensionsEnum>()), Times.Never);
    
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
        
        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

        var resourceDetectorLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_resourceDetectorLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockResourceDetectorLoader = new Mock<IResourceDetectorLoader>();
        resourceDetectorLoaderField?.SetValue(builder, mockResourceDetectorLoader.Object);

        // ACT
        builder.Configure();

        // ASSERT
        using var services = serviceColl.BuildServiceProvider();
        var trace = services.GetService<TracerProvider>(); // need this to trigger the ResourceBuilder invocation
        mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetAllSignalTypeConfigs))]
    public void Configure_WithNoDistroSet_And_SignalSettings_Calls_Loaders_Instrumentation_Exporters_Extensions(
        string signal,
        Dictionary<string, string?> signalConfig
    )
    {
        // ARRANGE
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(signalConfig)
            .Build();

        var otelBuilder = services.AddOpenTelemetry();
        var builder = new SimpleOpenTelemetryBuilder(otelBuilder, config);

         
        // Use reflection to mock private fields for testing
        var exporterLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_exporterLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExporterLoader = new Mock<IExporterLoader>();
        exporterLoaderField?.SetValue(builder, mockExporterLoader.Object);

        var instrumentationLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_instrumentationLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockInstrumentationLoader = new Mock<IInstrumentationLoader>();
        instrumentationLoaderField?.SetValue(builder, mockInstrumentationLoader.Object);
     
        var extensionsLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_extensionLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExtensionsLoader = new Mock<IExtensionLoader>();
        extensionsLoaderField?.SetValue(builder, mockExtensionsLoader.Object);

        var samplerLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_samplerLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockSamplerLoader = new Mock<ISamplerLoader>();
        samplerLoaderField?.SetValue(builder, mockSamplerLoader.Object);

        var propagatorLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_propagatorLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockPropagatorLoader = new Mock<IPropagatorLoader>();
        propagatorLoaderField?.SetValue(builder, mockPropagatorLoader.Object);

        // ACT
        builder.Configure();

        // ASSERT
        if (signal == "metric")
        {
            mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricInstrumentationEnum>()), Times.Once);
            mockExtensionsLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Once);
        }
        if (signal == "trace")
        {
            mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceInstrumentationEnum>()), Times.Once);
            mockExtensionsLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceExtensionsEnum>()), Times.Once);
            mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
        }
        if (signal == "log")
        {
            mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);
            mockExtensionsLoader.Verify(e => e.AddLogExtension(It.IsAny<LoggerProviderBuilder>(), It.IsAny<LogExtensionsEnum>()), Times.Once);
        }    
    }

    [Fact]
    public void Configure_Will_Terminate_When_Invalid_IConfiguration_Passed()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var otelBuilder = services.AddOpenTelemetry();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var ex = Assert.Throws<Exception>(() => new SimpleOpenTelemetryBuilder(otelBuilder, config));

    }

    /// <summary>
    /// Gets config dictionary with settings to trigger off all Loaders for that signal
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<object[]> GetAllSignalTypeConfigs()
    {
        var configs = new Dictionary<string,Dictionary<string,string?>>()
        {
            // most values are enums and validated against
            ["trace"] = new ()
            {
                ["SimpleOpenTelemetry:Trace:Exporters:0:type"] = "console",
                ["SimpleOpenTelemetry:Trace:Instrumentations:0"] = "HttpClient",
                ["SimpleOpenTelemetry:Trace:Sources:0"] = "test",
                ["SimpleOpenTelemetry:Trace:Sampler"] = "test",
                ["SimpleOpenTelemetry:Trace:Propagators"] = "test",
                ["SimpleOpenTelemetry:Trace:Extensions:0"] = "AWSXRayTraceId"
            },
            ["log"] = new ()
            {
                ["SimpleOpenTelemetry:Log:Exporters:0:type"] = "console",
                ["SimpleOpenTelemetry:Log:Extensions:0"] = "None"
            },
            ["metric"] = new ()
            {
                ["SimpleOpenTelemetry:Metric:Exporters:0:type"] = "console",
                ["SimpleOpenTelemetry:Metric:Instrumentations:0"] = "Runtime",
                ["SimpleOpenTelemetry:Metric:Extensions:0"] = "None",
                ["SimpleOpenTelemetry:Metric:CustomMeters:0"] = "test"
            },
        };

        foreach (var key in configs.Keys)
        {
            yield return new object[] { key, configs[key] };
        }
    }

}