namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Extensions;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.OtelComponents.Propagator;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.OtelComponents.Sampler;
using SimpleOpenTelemetry.Utils;


/// <summary>
/// Configure OpenTelemetry settings via IConfiguration and return
/// OpenTelemetryBuilder for an other custom fluent operations
/// </summary>
internal sealed class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    private SimpleOpenTelemetryOptions _options = new SimpleOpenTelemetryOptions();

    private readonly IOpenTelemetryBuilder _otelBuilder;

    private readonly IConfiguration _configuration;

    private readonly IInstrumentationLoader _openTelemetryInstrumentationLoader;

    private readonly IExporterLoader _exporterLoader;

    private readonly IResourceDetectorLoader _resourceDetectorLoader;

    private readonly ISamplerLoader _samplerLoader;

    private readonly IPropagatorLoader _propagatorLoader;

    private readonly IExtensionLoader _extensionLoader;

    private readonly IDistroLoader _distroLoader;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder and load in configuration
    /// </summary>
    internal SimpleOpenTelemetryBuilder(IOpenTelemetryBuilder otelBuilder,
        IConfiguration config)
    {
        _configuration = config;
        _otelBuilder = otelBuilder;
        _openTelemetryInstrumentationLoader = new InstrumentationLoader(config);
        _resourceDetectorLoader = new ResourceDetectorLoader(config);
        _exporterLoader = new ExporterLoader(config);
        _samplerLoader = new SamplerLoader(config);
        _propagatorLoader = new PropagatorLoader();
        _extensionLoader = new ExtensionLoader(config);
        _distroLoader = new DistroLoader(config);

        // Load in configuration from file
        var section = _configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        var simpleOpenTelemetryConfig = new SimpleOpenTelemetryOptions();

        section.Bind(simpleOpenTelemetryConfig);

        if (simpleOpenTelemetryConfig == null)
            throw new ArgumentNullException(nameof(simpleOpenTelemetryConfig));

        _options = simpleOpenTelemetryConfig;

    }

    /// <summary>
    /// Configures the appropriate settings for trace, log and metrics based on 
    /// SimpleOpenTelemetryOptions values.  
    /// Also sets up:
    ///  - Propagators, extensions, samplers, resource detectors
    ///  - OpenTelmeetry.Resources.Resource based on configured detectors, internal AssemblyVersionResourceDetector Env var detector
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder</param>
    /// <param name="configuration">builder configuration</param>
    public void Configure()
    {
        // Check and load distro, this will skip any other configuration
        if (_distroLoader.LoadDistro(_otelBuilder, _options))
            return;

        var resourceBuilder = ConfigureResourceAttributes();

        ConfigureMetrics();

        ConfigureTracing(resourceBuilder);

        ConfigureLogging();

        _propagatorLoader.AddPropagators(_options);

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
            _resourceDetectorLoader.AddResourceDetectors(builder, _options);

            // 3. override with any ENV Vars / json config section definitions
            builder.AddEnvironmentVariableDetector();

            _builder = builder;

         });

        return _builder;
    }

    private void ConfigureMetrics()
    {
        if (!SettingsHelper.HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Metric)))
            return;

        _otelBuilder.WithMetrics(metrics =>
        {
            // Apply settings
            if (_options.Metric.Settings?.MetricLimit != null)
                metrics.SetMaxMetricStreams(_options.Metric.Settings.MetricLimit.Value);

            // add in tracing instrumentation options from config
            _options.Metric.Instrumentations?.ToList().ForEach(r =>
            {
                _openTelemetryInstrumentationLoader.AddMetricsInstrumentation(metrics, r);
            });

            // add in meters
            _options.Metric.CustomMeters?.ToList().ForEach(r => metrics.AddMeter(r));

            if (_options.Metric.Exporters is not null)
                _exporterLoader.ConfigureExporters(metrics, _options);

            _options.Metric.Extensions?.ToList()?.ForEach(r => _extensionLoader.AddMetricsExtension(metrics, r));

        });
    }

    private void ConfigureTracing(ResourceBuilder? resourceBuilder)
    {
        var shouldConfigureTracing = SettingsHelper.HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Trace));

        if (!shouldConfigureTracing)
            return;

        _otelBuilder.WithTracing(tracing =>
        {
            // set any options            
            if (_options.Trace.Settings?.SetErrorStatusOnException.HasValue == true)
                tracing.SetErrorStatusOnException(_options.Trace.Settings.SetErrorStatusOnException.Value);

            // add in tracing instrumenation options from config
            _options.Trace.Instrumentations?.ToList().ForEach(r => {
                _openTelemetryInstrumentationLoader.AddTracingInstrumentation(tracing, r);
            });

            // add trace sources from config
            _options.Trace.Sources?.ToList().ForEach(r => tracing.AddSource(r));

            // add in sampler if set in config
            _samplerLoader.AddSampler(tracing, resourceBuilder?.Build(), _options);

            if (_options.Trace.Exporters is not null)
                _exporterLoader.ConfigureExporters(tracing, _options);

            // Iterate over exporters for this montioring type
            _options.Trace.Extensions?.ToList()?.ForEach(r => _extensionLoader.AddTraceExtension(tracing, r));
        });
    }

    private void ConfigureLogging()
    {
        if (!SettingsHelper.HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Log)))
            return;

        _otelBuilder.WithLogging(
            logging =>
            {
                // Iterate over exporters for this montioring type and add them
                _exporterLoader.ConfigureExporters(logging, _options);
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
