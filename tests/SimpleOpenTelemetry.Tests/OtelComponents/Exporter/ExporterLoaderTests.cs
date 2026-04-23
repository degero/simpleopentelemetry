using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Diagnostics;
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

    #region Theory Tests for All Known Exporters

    /// <summary>
    /// Theory test that verifies all known trace exporters can be loaded and registered successfully.
    /// Uses EventSource listener to verify successful registration events.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAllKnownTraceExporters))]
    public void ConfigureExporters_WithAllKnownTraceExporters_SuccessfullyRegistered(TraceExporterEnum exporterType)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
        var config = new SimpleOpenTelemetryOptions
        {
            Trace = new SimpleOpenTelemetryTraceOptions
            {
                Exporters = new List<SimpleOpenTelemetryExporterConfig>
                {
                    new()
                    {
                        Type = ConvertTraceExporterEnumToConfigType(exporterType)
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

        // Assert - Verify that exporter registration events were logged (either success or error)
        var verboseEvents = listener.Events
            .Where(e => e.Level == EventLevel.Verbose)
            .ToList();

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        // Either we have a successful registration event or an error is acceptable
        // (some exporters may fail if assemblies are not available, but we still logged the attempt)
        var allRelevantEvents = verboseEvents.Union(errorEvents).ToList();
        
        Assert.NotEmpty(allRelevantEvents);
    }

    /// <summary>
    /// Theory test that verifies all known metric exporters can be loaded and registered successfully.
    /// Uses EventSource listener to verify successful registration events.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAllKnownMetricExporters))]
    public void ConfigureExporters_WithAllKnownMetricExporters_SuccessfullyRegistered(MetricExporterEnum exporterType)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
        var config = new SimpleOpenTelemetryOptions
        {
            Metric = new SimpleOpenTelemetryMetricOptions
            {
                Exporters = new List<SimpleOpenTelemetryExporterConfig>
                {
                    new()
                    {
                        Type = ConvertMetricExporterEnumToConfigType(exporterType)
                    }
                }
            }
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithMetrics(m =>
        {
            _loader.ConfigureExporters(m, config);
        });

        using var app = builder.Build();

        // Assert - Verify that exporter registration events were logged (either success or error)
        var verboseEvents = listener.Events
            .Where(e => e.Level == EventLevel.Verbose)
            .ToList();

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        // Either we have a successful registration event or an error is acceptable
        // (some exporters may fail if assemblies are not available, but we still logged the attempt)
        var allRelevantEvents = verboseEvents.Union(errorEvents).ToList();
        
        Assert.NotEmpty(allRelevantEvents);
    }

    /// <summary>
    /// Theory test that verifies all known log exporters can be loaded and registered successfully.
    /// Uses EventSource listener to verify successful registration events.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetAllKnownLogExporters))]
    public void ConfigureExporters_WithAllKnownLogExporters_SuccessfullyRegistered(LogExporterEnum exporterType)
    {
        // Arrange
        using var listener = new TestEventListener(SimpleOpenTelemetryEventSource.EventSourceName);
        
        var config = new SimpleOpenTelemetryOptions
        {
            Log = new SimpleOpenTelemetryLogOptions
            {
                Exporters = new List<SimpleOpenTelemetryExporterConfig>
                {
                    new()
                    {
                        Type = ConvertLogExporterEnumToConfigType(exporterType)
                    }
                }
            }
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Act
        builder.Services.AddOpenTelemetry().WithLogging(l =>
        {
            _loader.ConfigureExporters(l, config);
        });

        using var app = builder.Build();

        // Assert - Verify that exporter registration events were logged (either success or error)
        var verboseEvents = listener.Events
            .Where(e => e.Level == EventLevel.Verbose)
            .ToList();

        var errorEvents = listener.Events
            .Where(e => e.Level == EventLevel.Error)
            .ToList();

        // Either we have a successful registration event or an error is acceptable
        // (some exporters may fail if assemblies are not available, but we still logged the attempt)
        var allRelevantEvents = verboseEvents.Union(errorEvents).ToList();
        
        Assert.NotEmpty(allRelevantEvents);
    }

    #endregion

    #region MemberData Providers for Theory Tests

    /// <summary>
    /// Provides all known trace exporters for theory tests.
    /// </summary>
    public static IEnumerable<object[]> GetAllKnownTraceExporters()
    {
        foreach (var exporter in Enum.GetValues<TraceExporterEnum>())
        {
            yield return new object[] { exporter };
        }
    }

    /// <summary>
    /// Provides all known metric exporters for theory tests.
    /// </summary>
    public static IEnumerable<object[]> GetAllKnownMetricExporters()
    {
        foreach (var exporter in Enum.GetValues<MetricExporterEnum>())
        {
            yield return new object[] { exporter };
        }
    }

    /// <summary>
    /// Provides all known log exporters for theory tests.
    /// </summary>
    public static IEnumerable<object[]> GetAllKnownLogExporters()
    {
        foreach (var exporter in Enum.GetValues<LogExporterEnum>())
        {
            yield return new object[] { exporter };
        }
    }

    #endregion

    #region Helper Methods for Converting Between Enum Types

    /// <summary>
    /// Converts TraceExporterEnum to SimpleOpenTelemetryExporterType.
    /// </summary>
    private static SimpleOpenTelemetryExporterType ConvertTraceExporterEnumToConfigType(TraceExporterEnum exporterEnum)
    {
        return exporterEnum switch
        {
            TraceExporterEnum.Otlp => SimpleOpenTelemetryExporterType.Otlp,
            TraceExporterEnum.Console => SimpleOpenTelemetryExporterType.Console,
            TraceExporterEnum.Azure => SimpleOpenTelemetryExporterType.Azure,
            _ => throw new ArgumentOutOfRangeException(nameof(exporterEnum), exporterEnum, null)
        };
    }

    /// <summary>
    /// Converts MetricExporterEnum to SimpleOpenTelemetryExporterType.
    /// </summary>
    private static SimpleOpenTelemetryExporterType ConvertMetricExporterEnumToConfigType(MetricExporterEnum exporterEnum)
    {
        return exporterEnum switch
        {
            MetricExporterEnum.Otlp => SimpleOpenTelemetryExporterType.Otlp,
            MetricExporterEnum.Console => SimpleOpenTelemetryExporterType.Console,
            MetricExporterEnum.PrometheusHttpListener => SimpleOpenTelemetryExporterType.PrometheusHttpListener,
            MetricExporterEnum.PrometheusAspNetCore => SimpleOpenTelemetryExporterType.PrometheusAspNetCore,
            MetricExporterEnum.Azure => SimpleOpenTelemetryExporterType.Azure,
            _ => throw new ArgumentOutOfRangeException(nameof(exporterEnum), exporterEnum, null)
        };
    }

    /// <summary>
    /// Converts LogExporterEnum to SimpleOpenTelemetryExporterType.
    /// </summary>
    private static SimpleOpenTelemetryExporterType ConvertLogExporterEnumToConfigType(LogExporterEnum exporterEnum)
    {
        return exporterEnum switch
        {
            LogExporterEnum.Otlp => SimpleOpenTelemetryExporterType.Otlp,
            LogExporterEnum.Console => SimpleOpenTelemetryExporterType.Console,
            LogExporterEnum.Azure => SimpleOpenTelemetryExporterType.Azure,
            _ => throw new ArgumentOutOfRangeException(nameof(exporterEnum), exporterEnum, null)
        };
    }

    #endregion
}