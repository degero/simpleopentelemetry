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
    internal readonly IConfiguration _configuration;

    internal ILogger _logger;

    // TODO Chad extract interface for testing
    internal readonly OpenTelemetryInstrumentationLoader _openTelemetryInstrumentationLoader;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder
    /// </summary>
    public SimpleOpenTelemetryBuilder(OpenTelemetryBuilder otelBuilder,
        IConfiguration config)
    {
        _configuration = config;
        _otelBuilder = otelBuilder;
        _openTelemetryInstrumentationLoader = new OpenTelemetryInstrumentationLoader(config);

        // TODO Chad fix this up to be injected
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
    /// <param name="configuration">builder configuration</param>
    /// <returns>The builder for chaining</returns>
    public ISimpleOpenTelemetryBuilder Configure()
    {
        var section = _configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        var config = new SimpleOpenTelemetryConfiguration();
       
        section.Bind(config);

        if (config == null) 
            throw new ArgumentNullException(nameof(config));

        // TODO chad this may not be necessary anymore as there are no overrides
        _options = JsonSerializer.Deserialize<SimpleOpenTelemetryBuilderOptions>(
            JsonSerializer.Serialize(config));


        SetupMetrics();

        SetupTracing();
        
        SetupLogging();

        // TODO Chad add in other exporter registrations if possible

        return this;
    }

    private void SetupMetrics()
    {
        _otelBuilder.WithMetrics(metrics =>
        {
            // add in tracing instrumenation options from config
            _options.MetricsInstrumentations?.ToList().ForEach(r =>
            {
                _openTelemetryInstrumentationLoader.AddMetricsInstrumentation(metrics, r, _logger);
            });

            if (_options.Exporters is not null)
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
            // If not set in this configsection, set through either the OpenTelemetry Env vars
            // or Configuration json that OpenTelemetry lib loads under a root "OpenTelemetryOTLPExporter" config section
            // TODO Chad test this scenario
            return null;
        }
    }

    private void SetupTracing()
    {
        var serviceName = _configuration.GetValue<string>("OTEL_SERVICE_NAME"); // TODO Chad SettingsHelper.OtelServiceName() ?? "";

        _otelBuilder.WithTracing(tracing =>
        {
            // add in tracing instrumenation options from config
            _options.TracingInstrumentations?.ToList().ForEach(r =>
            {
                _openTelemetryInstrumentationLoader.AddTracingInstrumentation(tracing, r, _logger);
            });

            // add trace sources from config
            _options.TraceSources?.ToList().ForEach(r =>
            {
                tracing.AddSource(r);
            });
            
            // TODO Chad add configuration for this
            // Setup a tracing source
            tracing.AddSource(serviceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName));

            //    // TODO Chad check and any other tracing settings
            //    // tracing.RecordException = true;

            // Iterate over exporters for this montioring type
            if (_options.Exporters is not null)
                ConfigureExporters(tracing, _options.Exporters.Tracing, AddOTLPExporter);

        });
    }

    private void SetupLogging()
    {
        _otelBuilder.WithLogging(logging =>
        {
            // Iterate over exporters for this montioring type
            ConfigureExporters(logging, _options.Exporters.Logging, AddOTLPExporter);
        // TODO chad add in other logging related settings and possible move the below here from program.cs
        // WebApllicationBuilder.Logging.AddOpenTelemetry(logging =>
        //{
        //    logging.IncludeFormattedMessage = true;
        //    logging.IncludeScopes = true;
        //});
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
