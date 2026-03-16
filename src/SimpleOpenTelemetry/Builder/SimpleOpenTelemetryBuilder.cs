namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Configuration;
using SimpleOpenTelemetry.Utils;
using System.Diagnostics;
using System.Text.Json;

public interface IProviderBuilder
{
    void AddOtlpExporter(string name, Action<OtlpExporterOptions>? configure);
}

/// <summary>
/// Fluent builder for configuring SimpleOpenTelemetry
/// </summary>
public class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    internal SimpleOpenTelemetryBuilderOptions _options = new SimpleOpenTelemetryBuilderOptions();
    internal IList<OtlpExporterOptions> _exporters = new List<OtlpExporterOptions>();

    internal readonly TracerProviderBuilder _tracerProviderBuilder;
    internal readonly OpenTelemetryBuilder _otelBuilder;
    internal ILogger _logger;

    // TODO Chad extract interface for testing
    internal readonly OpenTelemetryInstrumentationLoader _openTelemetryInstrumentationLoader;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder
    /// </summary>
    public SimpleOpenTelemetryBuilder(OpenTelemetryBuilder otelBuilder)
    {
        _otelBuilder = otelBuilder;
        _openTelemetryInstrumentationLoader = new OpenTelemetryInstrumentationLoader();
        _logger = LoggerFactory.Create(builder =>
        {
            builder.AddFilter("Microsoft", LogLevel.Warning)
               .AddFilter("System", LogLevel.Warning)
               .AddFilter("SampleApp.Program", LogLevel.Debug)
               .AddConsole();
        }).CreateLogger<SimpleOpenTelemetryBuilder>();
    }

    /// <summary>
    /// Configures the appropriate exporter (AzureMonitor, NewRelic, or OTLP) based on SimpleOpenTelemetryOptions
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <param name="configurationManager">builder configurationManager</param>
    /// <returns>The builder for chaining</returns>
    public ISimpleOpenTelemetryBuilder ConfigureExporterFromOptions(
         IConfiguration configuration)
    {
        var section = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        var config = new SimpleOpenTelemetryConfiguration();
        section.Bind(config);

        if (config == null) throw new ArgumentNullException(nameof(config));

        _options = JsonSerializer.Deserialize<SimpleOpenTelemetryBuilderOptions>(
            JsonSerializer.Serialize(config));

        var serviceName = configuration.GetValue<string>("OTEL_SERVICE_NAME"); //SettingsHelper.OtelServiceName() ?? "";

        // Now options are in place enable tracking, logging and metrics based on option
        SetupMetrics();

        SetupTracing(serviceName);
        
        SetupLogging();

        // TODO Chad figure out how to load in other exporters reffed in config
        //return options.Exporters?.ToUpper() switch
        //{
        //    "AZUREMONITOR" => ConfigureAzureMonitor(builder),
        //    "NEWRELIC" => ConfigureNewRelic(builder),
        //    _ => ConfigureOtlp(builder)
        //};
        return this;
    }

    private void SetupMetrics()
    {
        _otelBuilder.WithMetrics(metrics =>
        {
            // add in tracing instrumenation options from config
            _options.MetricsInstrumentations.ToList().ForEach(r =>
            {
                _openTelemetryInstrumentationLoader.AddMetricsInstrumentation(metrics, r, _logger);
            });

            ConfigureExporters(metrics, _options.Exporters.Metrics, AddOTLPExporter);

        });
    }

    private void ConfigureExporters<TBuilder>(
    TBuilder builder,
    IList<SimpleOpenTelemetryExporterConfig> exporters,
    Action<TBuilder, SimpleOpenTelemetryExporterConfig, string> addExporter)
    {
        for (var i = 0; i < exporters.Count; i++)
        {
            var item = exporters[i];
            switch (item.Type)
            {
                case SimpleOpenTelemetryExporterType.Otlp:
                    addExporter(builder, item, $"OTLPExporter-{i}");
                    break;
                default:
                    break;
            }
        }
    }

    private void AddOTLPExporter(MeterProviderBuilder builder, SimpleOpenTelemetryExporterConfig item, string exporterName)
    => builder.AddOtlpExporter(name: exporterName, configure: BuildOtlpConfig(item));

    private void AddOTLPExporter(TracerProviderBuilder builder, SimpleOpenTelemetryExporterConfig item, string exporterName)
        => builder.AddOtlpExporter(name: exporterName, configure: BuildOtlpConfig(item));

    /// TOOO Chad look at making this generic

    private void AddOTLPExporter(LoggerProviderBuilder builder, SimpleOpenTelemetryExporterConfig item, string exporterName)
        => builder.AddOtlpExporter(name: exporterName, configureExporter: BuildOtlpConfig(item));

    private Action<OtlpExporterOptions> BuildOtlpConfig(SimpleOpenTelemetryExporterConfig item)
    {
        if (item.Endpoint != null && item.Protocol != null)
        {
            return (Action<OtlpExporterOptions>)(config =>
            {
                // TODO Chad add in other config
                config.Endpoint = item.Endpoint;
                config.Protocol = item.Protocol == SimpleOpenTelemetryExporterProtocol.Grpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;
            });
        }
        else
        {
            // Set config through either the Env vars or Configuration json that OpenTelemetry lib loads
            return null;
        }
    }

    private void SetupTracing(string serviceName)
    {
        _otelBuilder.WithTracing(tracing =>
        {
            // add in tracing instrumenation options from config
            _options.TracingInstrumentations.ToList().ForEach(r =>
            {
                _openTelemetryInstrumentationLoader.AddTracingInstrumentation(tracing, r, _logger);
            });

            // add trace sources configured
            _options.TraceSources.ToList().ForEach(r =>
            {
                tracing.AddSource(r);
            });
            
            // TODO Chad add configuration for this
            // Setup a tracing source
            tracing.AddSource(serviceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName));

            //    // TODO Chad check
            //    // tracing.RecordException = true;

            // Iterate over exporters for this montioring type
            ConfigureExporters(tracing, _options.Exporters.Tracing, AddOTLPExporter);

        });
    }

    private void SetupLogging()
    {
        _otelBuilder.WithLogging(logging =>
        {
            // Iterate over exporters for this montioring type
            ConfigureExporters(logging, _options.Exporters.Logging, AddOTLPExporter);

        });
    }

    // // TODO Chad check if needed
    // private void UpdateResourceBuilder()
    // {
    //     var serviceName = _options.ServiceName ?? "unknown-service";
    //     var serviceVersion = _options.ServiceVersion ?? "1.0.0";

    //     // TOOD chad check if needed
    //     var resourceBuilder = ResourceBuilder.CreateDefault()
    //         .AddService(
    //             serviceName: serviceName,
    //             serviceVersion: serviceVersion);

    //     _tracerProviderBuilder.SetResourceBuilder(resourceBuilder);
    // }
}
