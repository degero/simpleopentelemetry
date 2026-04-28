using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry;
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
using System.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.Builder;

public class SimpleOpenTelemetryBuilderTests
{
    [Fact]
    public void Configure_ThrowsWhenSimpleOpenTelemetryConfigSectionIsMissing()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);

        var exception = Assert.Throws<Exception>(() => builder.AddSimpleOpenTelemetry());
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void Configure_ThrowsWhenSimpleOpenTelemetryConfigSectionIsMissing_ForMetrics()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);

        var exception = Assert.Throws<Exception>(() => builder.AddSimpleOpenTelemetry());
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void Configure_ThrowsWhenSimpleOpenTelemetryConfigSectionIsMissing_ForLogging()
    {
        // Arrange: Empty configuration with no SimpleOpenTelemetry section
        // Note: HostApplicationBuilder automatically provides LoggerProvider via AddLogging
        var config = new ConfigurationBuilder()
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);

        var exception = Assert.Throws<Exception>(() => builder.AddSimpleOpenTelemetry());
        Assert.Contains("No configuration section 'SimpleOpenTelemetry'", exception.Message);
    }

    [Fact]
    public void Configure_SetsUpTracing_WhenTraceExportersAreConfigured()
    {
        // Arrange: Configuration with trace exporters (using OTLP which is built-in)
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

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        // Assert: TracerProvider should be registered when Trace exporters are configured
        var tracerProvider = host.Services.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Theory]
    [InlineData(@"""Settings"": { ""IncludeFormattedMessage"": true, ""IncludeScopes"": true, ""ParseStateValues"": true }", true, true, true)]
    [InlineData(@"""Settings"": { ""IncludeFormattedMessage"": true }", true, false, false)]
    [InlineData(@"""Exporters"": []", false, false, false)]
    public void Configure_SetsUpLoggingOptions_WhenAreConfigured(string jsonSeg, bool formatMsg, bool inclScope, bool parseState)
    {
      var jsonConfig = $@"{{ ""SimpleOpenTelemetry"": {{ ""Log"": {{ {jsonSeg} }} }} }}";

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(config);
        builder.AddSimpleOpenTelemetry();

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;
        Assert.Equal(formatMsg, options.IncludeFormattedMessage);
        Assert.Equal(inclScope, options.IncludeScopes);
        Assert.Equal(parseState, options.ParseStateValues);
    }

    [Fact]
    public void Configure_WithDistroSet_LoadsDistroAndReturnsEarly()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockOtelBuilder = new Mock<IOpenTelemetryBuilder>();
        mockOtelBuilder.Setup(b => b.Services).Returns(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["SimpleOpenTelemetry:Distro"] = "AzureMonitorAspNetCore"
            })
            .Build();

        var builder = new SimpleOpenTelemetryBuilder(mockOtelBuilder.Object, config);

        // Use reflection to mock private fields for testing
        var distroLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_distroLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockDistroLoader = new Mock<IDistroLoader>();
        mockDistroLoader.Setup(d => d.LoadDistro(It.IsAny<IOpenTelemetryBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>())).Returns(true);
        distroLoaderField?.SetValue(builder, mockDistroLoader.Object);

        var exporterLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_exporterLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockExporterLoader = new Mock<IExporterLoader>();
        exporterLoaderField?.SetValue(builder, mockExporterLoader.Object);

        var instrumentationLoaderField = typeof(SimpleOpenTelemetryBuilder).GetField("_openTelemetryInstrumentationLoader", BindingFlags.NonPublic | BindingFlags.Instance);
        var mockInstrumentationLoader = new Mock<IInstrumentationLoader>();
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

        // Act
        builder.Configure();

        // Assert
        mockDistroLoader.Verify(d => d.LoadDistro(mockOtelBuilder.Object, It.IsAny<SimpleOpenTelemetryOptions>()), Times.Once);

        // Verify resourcebuilder configure callbacks are setup        
        Assert.False(services.Any(r => r.ServiceType.Name == "IConfigureMeterProviderBuilder"));
        Assert.False(services.Any(r => r.ServiceType.Name == "IConfigureTracerProviderBuilder"));
        Assert.False(services.Any(r => r.ServiceType.Name == "IConfigureLoggerProviderBuilder"));
        
        // Verify that other loaders' methods are not called (indicating early return)
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<MeterProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<TracerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExporterLoader.Verify(e => e.ConfigureExporters(It.IsAny<LoggerProviderBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockInstrumentationLoader.Verify(i => i.AddMetricsInstrumentation(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricInstrumentationEnum>()), Times.Never);
        mockInstrumentationLoader.Verify(i => i.AddTracingInstrumentation(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceInstrumentationEnum>()), Times.Never);
        mockResourceDetectorLoader.Verify(r => r.AddResourceDetectors(It.IsAny<ResourceBuilder>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockSamplerLoader.Verify(s => s.AddSampler(It.IsAny<TracerProviderBuilder>(), It.IsAny<Resource>(), It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockPropagatorLoader.Verify(p => p.AddPropagators(It.IsAny<SimpleOpenTelemetryOptions>()), Times.Never);
        mockExtensionsLoader.Verify(e => e.AddMetricsExtension(It.IsAny<MeterProviderBuilder>(), It.IsAny<MetricExtensionsEnum>()), Times.Never);
        mockExtensionsLoader.Verify(e => e.AddTraceExtension(It.IsAny<TracerProviderBuilder>(), It.IsAny<TraceExtensionsEnum>()), Times.Never);
    }
}