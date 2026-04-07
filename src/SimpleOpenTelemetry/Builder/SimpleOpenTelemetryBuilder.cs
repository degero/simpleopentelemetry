namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Configuration;
using SimpleOpenTelemetry.Distro;
using SimpleOpenTelemetry.Exporter;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.Propagator;
using SimpleOpenTelemetry.Resource;
using SimpleOpenTelemetry.Sampler;
using SimpleOpenTelemetry.Utils;


/// <summary>
/// Configure OpenTelemetry settings via IConfiguration and return
/// OpenTelemetryBuilder for an other custom fluent operations
/// </summary>
internal sealed class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    private SimpleOpenTelemetryBuilderOptions _options = new SimpleOpenTelemetryBuilderOptions();

    private IList<OtlpExporterOptions> _exporters = new List<OtlpExporterOptions>();

    private readonly TracerProviderBuilder _tracerProviderBuilder;

    private readonly IOpenTelemetryBuilder _otelBuilder;

    private readonly IConfiguration _configuration;

    private ILogger _logger;

    // TODO Chad extract interface for testing / Change name
    private readonly InstrumentationLoader _openTelemetryInstrumentationLoader;

    private readonly ExporterLoader _exporterLoader;

    private readonly ResourceDetectorLoader _resourceDetectorLoader;

    private readonly SamplerLoader _samplerLoader;

    private readonly PropagatorLoader _propagatorLoader;

    private readonly ExtensionLoader _extensionsLoader;

    private readonly DistroLoader _distroLoader;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder and load in configuration
    /// </summary>
    internal SimpleOpenTelemetryBuilder(IOpenTelemetryBuilder otelBuilder,
        IConfiguration config)
    {
        _configuration = config;
        _otelBuilder = otelBuilder;
        _openTelemetryInstrumentationLoader = new(config);
        _resourceDetectorLoader = new(config);
        _exporterLoader = new(config);
        _samplerLoader = new(config);
        _propagatorLoader = new(config);
        _extensionsLoader = new(config);
        _distroLoader = new(config);

        // Load in configuration from file
        var section = _configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
        var simpleOpenTelemetryConfig = new SimpleOpenTelemetryConfiguration();

        section.Bind(simpleOpenTelemetryConfig);

        if (simpleOpenTelemetryConfig == null)
            throw new ArgumentNullException(nameof(simpleOpenTelemetryConfig));

        _options = simpleOpenTelemetryConfig;

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
    /// Configures the appropriate settings for trace, log and metrics based on 
    /// SimpleOpenTelemetryConfiguration values.  
    /// Also sets up:
    ///  - Propagators, extensions, samplers, resource detectors
    ///  - OpenTelmeetry.Resources.Resource based on configured detectors, internal AssemblyVersionResourceDetector Env var detector
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <param name="configuration">builder configuration</param>
    /// <returns>The builder for chaining</returns>
    public IOpenTelemetryBuilder Configure()
    {
        var resourceBuilder = ConfigureResourceAttributes();

        // Check and load distro, this will skip any other configuration
        if (_distroLoader.LoadDistro(_otelBuilder, _options, _logger))
            return _otelBuilder;

        ConfigureMetrics();

        ConfigureTracing(resourceBuilder);

        ConfigureLogging();

        _propagatorLoader.AddPropagators(_options, _logger);

        return _otelBuilder;
    }

    private bool LoadDistro()
    {
        throw new NotImplementedException();
    }

    private ResourceBuilder? ConfigureResourceAttributes()
    {   
        ResourceBuilder? _builder = null;

        // Run OpenTelemetry Auto detection / configuration (eg from OTEL_* configs)
        _otelBuilder.ConfigureResource(builder => 
        {   

            // TODO Chad do all this from config entries

            // 1. detect the (very important) service.version attribute (it can be overridden by the next steps)
            builder.AddAssemblyVersionResourceDetector();

            // 2. Run detectors first
            _resourceDetectorLoader.AddResourceDetectors(builder, _options, _logger);

            // 3. override with any ENV Vars / json config section definitions
            builder.AddEnvironmentVariableDetector();

            _builder = builder;

         });

        return _builder;
    }

    private void ConfigureMetrics()
    {
        if (!SettingsHelper.HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryBuilderOptions.Metric)))
            return;

        _otelBuilder.WithMetrics(metrics =>
        {
            // Apply settings
            if (_options.Metric.Settings?.MetricLimit != null)
                metrics.SetMaxMetricStreams(_options.Metric.Settings.MetricLimit.Value);

            // add in tracing instrumentation options from config
            _options.Metric.Instrumentations?.ToList().ForEach(r =>
            {
                _openTelemetryInstrumentationLoader.AddMetricsInstrumentation(metrics, r, _logger);
            });

            // add in meters
            _options.CustomMeters?.ToList().ForEach(r => metrics.AddMeter(r));

            if (_options.Metric.Exporters is not null)
                _exporterLoader.ConfigureExporters(metrics, _options, _logger);

            _options.Metric.Extensions?.ToList()?.ForEach(r => _extensionsLoader.AddMetricsExtension(metrics, r, _logger));

        });
    }

    private void ConfigureTracing(ResourceBuilder? resourceBuilder)
    {
        var shouldConfigureTracing = SettingsHelper.HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryBuilderOptions.Trace));

        if (!shouldConfigureTracing)
            return;

        _otelBuilder.WithTracing(tracing =>
        {
            // set any options            
            if (_options.Trace.Settings?.SetErrorStatusOnException.HasValue == true)
                tracing.SetErrorStatusOnException(_options.Trace.Settings.SetErrorStatusOnException.Value);

            // add in tracing instrumenation options from config
            _options.Trace.Instrumentations?.ToList().ForEach(r => {
                _openTelemetryInstrumentationLoader.AddTracingInstrumentation(tracing, r, _logger);
            });

            // add trace sources from config
            _options.TraceSources?.ToList().ForEach(r => tracing.AddSource(r));

            // add in sampler if set in config
            _samplerLoader.AddSampler(tracing, resourceBuilder?.Build(), _options, _logger);

            if (_options.Trace.Exporters is not null)
                _exporterLoader.ConfigureExporters(tracing, _options, _logger);

            // Iterate over exporters for this montioring type
            _options.Trace.Extensions?.ToList()?.ForEach(r => _extensionsLoader.AddTraceExtension(tracing, r, _logger));
        });
    }

    private void ConfigureLogging()
    {
        if (!SettingsHelper.HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryBuilderOptions.Log)))
            return;

        _otelBuilder.WithLogging(
            logging =>
            {
                // Iterate over exporters for this montioring type and add them
                _exporterLoader.ConfigureExporters(logging, _options, _logger);
            }, 
            options =>
            {
                
                if (_options.Log.Settings?.IncludeFormattedMessage is not null)
                    options.IncludeFormattedMessage = _options.Log.Settings.IncludeFormattedMessage.Value;
                if (_options.Log.Settings?.IncludeScopes is not null)
                    options.IncludeScopes = _options.Log.Settings.IncludeScopes.Value;
                if (_options.Log.Settings?.ParseStateValues is not null)
                    options.ParseStateValues = _options.Log.Settings.ParseStateValues.Value;
            }
        );
    }
}
