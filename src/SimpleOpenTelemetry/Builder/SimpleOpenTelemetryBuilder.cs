namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Configuration;
using SimpleOpenTelemetry.Exporter;
using SimpleOpenTelemetry.Utils;
using System.Reflection;
using System.Text;
using System.Text.Json;

public interface IProviderBuilder
{
    void AddOtlpExporter(string name, Action<OtlpExporterOptions>? configure);
}

/// <summary>
/// Builder for configuring OpenTelemetry in a simpler fashion
/// </summary>
public class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    private SimpleOpenTelemetryBuilderOptions _options = new SimpleOpenTelemetryBuilderOptions();
    private IList<OtlpExporterOptions> _exporters = new List<OtlpExporterOptions>();

    private readonly TracerProviderBuilder _tracerProviderBuilder;
    private readonly IOpenTelemetryBuilder _otelBuilder;
    private readonly IConfiguration _configuration;

    private ILogger _logger;

    // TODO Chad extract interface for testing / Change name
    private readonly OpenTelemetryInstrumentationLoader _openTelemetryInstrumentationLoader;
    private readonly ExporterLoader _exporterLoader;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder
    /// </summary>
    public SimpleOpenTelemetryBuilder(IOpenTelemetryBuilder otelBuilder,
        IConfiguration config)
    {
        _configuration = config;
        _otelBuilder = otelBuilder;
        _openTelemetryInstrumentationLoader = new(config);
        _exporterLoader = new(config);

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
        // Load in configuration from file
        var section = _configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        var config = new SimpleOpenTelemetryConfiguration();

        section.Bind(config);

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        _options = config;

        var extraAttributes = CreateExtraAttributes();

        // Run OpenTelemetry Auto detection / configuration (eg from OTEL_* configs)
        _otelBuilder.ConfigureResource(config => config
            .AddEnvironmentVariableDetector()
            .AddAttributes(extraAttributes)
        );

        // TODO Chad remove
        //var (valid, validationErrors) = ValidateConfiguration();
        //if (!valid)
        //    throw new Exception($"Aborting startup. Critical OpenTelemetry Configuration errors, {validationErrors}");

        SetupMetrics();

        SetupTracing();

        SetupLogging();

        // TODO Chad add in other exporter registrations if possible

        return this;
    }

    /// <summary>
    /// Create an key attributes if not defined
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, object> CreateExtraAttributes()
    {
        var attribs = new Dictionary<string, object>();

        // Only set verion from assemly if not set in service.version attribute
        if (!SettingsHelper.OtelResourceAttributes(_configuration).ToLower()
            .Contains(OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion))
        {
            // TODO Chad test on a dotnet build -p:Version=X
            var version = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion?.Split('+')[0];

            // TODO chad remove
            //.GetName().Version?.ToString();

            if (!string.IsNullOrWhiteSpace(version))
                attribs.Add(OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion, version);
        }

        return attribs;
    }


    // TODO Chad just use extension method to validate after all builders are done
    //private (bool, string?) ValidateConfiguration()
    //{
    //    var errors = "";
    //    var sb = new StringBuilder();

    //    // TODO Chad find out how to get these directly from OpenTelemetryBuilder
    //    //var tracerProvider = app.Services.GetRequiredService<TracerProvider>();
    //    //var resource = tracerProvider.GetResource();

    //    //var attrs = resource.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

    //    //// Check the ones you care about
    //    //var requiredKeys = new[] { "service.name", "service.version", "service.instance.id", "deployment.environment" };
    //    //var missing = requiredKeys.Where(k => !attrs.ContainsKey(k) || string.IsNullOrEmpty(attrs[k]?.ToString())).ToList();

    //    //if (missing.Any())
    //    //{
    //    //    throw new InvalidOperationException(
    //    //        $"Missing required OpenTelemetry resource attributes: {string.Join(", ", missing)}. " +
    //    //        "Set OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES env vars.");
    //    //}

    //    //// Optionally log what was detected
    //    //foreach (var key in requiredKeys)
    //    //    app.Logger.LogInformation("OTel resource: {Key}={Value}", key, attrs.GetValueOrDefault(key));


    //    var serviceName = SettingsHelper.OtelServiceName(_configuration);

    //    if (serviceName is null)
    //        sb.AppendLine($"Configuration value: {Utils.OpenTelemetryConstants.EnvironmentVariables.OTELse} missing");

    //    // TODO check instance id and attributes to validate they ar all set

    //    // DO we want to default this to assembly?
    //    //var serviceName = _options.ServiceName ?? "unknown-service";
    //    // var serviceVersion = SettingsHelper.OtelServiceVersion(_configuration) ?? "1.0.0";

    //    errors = sb.ToString();
    //    sb.Clear();

    //    if (!string.IsNullOrWhiteSpace(errors))
    //        return (false, errors);
    //    else
    //        return (true, null);
    //}

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
                _exporterLoader.ConfigureExporters(metrics, _options, _logger);

        });
    }


    private void SetupTracing()
    {

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

            // TODO chad add in samplers

            // TODO Chad remove
            //tracing.AddSource(serviceName)
            //    .SetResourceBuilder(
            //        ResourceBuilder.CreateDefault()
            //            .AddService(serviceName: serviceName,
            //                serviceVersion: serviceVersion
            //            ));

            //    // TODO Chad check and any other tracing settings
            //    // tracing.RecordException = true;

            // Iterate over exporters for this montioring type
            if (_options.Exporters is not null)
                _exporterLoader.ConfigureExporters(tracing, _options, _logger);

        });
    }

    private void SetupLogging()
    {
        _otelBuilder.WithLogging(logging =>
        {
            // Iterate over exporters for this montioring type and add them
            _exporterLoader.ConfigureExporters(logging, _options, _logger);

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
