namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.OtelComponents.Propagator;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.OtelComponents.Sampler;
using SimpleOpenTelemetry.Reflection;
using SimpleOpenTelemetry.Utils;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

/// <summary>
/// Configure OpenTelemetry settings via IConfiguration and return
/// OpenTelemetryBuilder for an other custom fluent operations
/// </summary>
internal sealed class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    private static readonly string eventCategory = nameof(SimpleOpenTelemetryBuilder);

    private SimpleOpenTelemetryOptions _options = new SimpleOpenTelemetryOptions();

    private readonly IOpenTelemetryBuilder _otelBuilder;

    private readonly IConfiguration _configuration;

    private readonly IInstrumentationLoader _instrumentationLoader;

    private readonly IExporterLoader _exporterLoader;

    private readonly IResourceDetectorLoader _resourceDetectorLoader;

    private readonly ISamplerLoader _samplerLoader;

    private readonly IPropagatorLoader _propagatorLoader;

    private readonly IExtensionLoader _extensionLoader;

    private readonly IDistroLoader _distroLoader;

    private readonly IAssemblyExecution _assemblyExecution;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder and load in configuration
    /// </summary>
    internal SimpleOpenTelemetryBuilder(
        IOpenTelemetryBuilder otelBuilder,
        IConfiguration config) // TODO refac out use of Iconfiguration and inject services
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(otelBuilder);

        _configuration = config;
        _otelBuilder = otelBuilder;
        _assemblyExecution = new AssemblyExecution();
        // TODO Chad remove config dependency
        _instrumentationLoader = new InstrumentationLoader(config, _assemblyExecution);
        _resourceDetectorLoader = new ResourceDetectorLoader(config, _assemblyExecution);
        _exporterLoader = new ExporterLoader(config, _assemblyExecution);
        _samplerLoader = new SamplerLoader(_assemblyExecution);
        _propagatorLoader = new PropagatorLoader(_assemblyExecution);
        _extensionLoader = new ExtensionLoader(config, _assemblyExecution);
        _distroLoader = new DistroLoader(config, _assemblyExecution);

    }

    /// <summary>
    /// Configures the appropriate settings for trace, log and metrics based on 
    /// SimpleOpenTelemetryOptions values.  
    /// Also sets up:
    ///  - Propagators, extensions, samplers, resource detectors
    ///  - OpenTelmeetry.Resources.Resource based on configured detectors, internal AssemblyVersionResourceDetector Env var detector
    /// </summary>
    public void Configure()
    {
        if (!ValidateAndLoadOptions())
            return;

        // Check and load distro, this will skip any other configuration
        if (_distroLoader.LoadDistro(_otelBuilder, _options))
            return;

        ConfigureResourceAttributes();
        
        ConfigureMetrics();

        ConfigureTracing();

        ConfigureLogging();

    }

    private bool ValidateAndLoadOptions()
    {
        // Load in configuration
        var section = _configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);

        // validate that this config has simpleopentelemetry section exists() as 
        // simpleOpenTelemetryConfig will never be null if any type of config opject was bound
        if (!section.Exists())
        {
            EventSource.Log.Error(eventCategory, $"No configuration section '{SimpleOpenTelemetryOptions.SectionName}'. This is required for SimpleOpenTelemetry.");
            return false;
        }

        // bypass check if distro is used
        bool specifiedDistro = !string.IsNullOrWhiteSpace(section.GetValue<string?>("Distro"));

        if (!specifiedDistro) // bypass signal settings validation if distro is set
        {
            bool atLeastOneExists = section.GetSection("Log").Exists()
                || section.GetSection("Metric").Exists()
                || section.GetSection("Trace").Exists();
                
            if (!atLeastOneExists)
            {
                EventSource.Log.Error(eventCategory, $"Missing signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.");
                return false;
            }
        }
        var simpleOpenTelemetryConfig = new SimpleOpenTelemetryOptions();
        section.Bind(simpleOpenTelemetryConfig);
        _options = simpleOpenTelemetryConfig;
        return true;
    }

    private void ConfigureResourceAttributes()
    {   
        // Normally users will want to set in "Detectors" config at minium "EnvVar" for opentelemetry to load in
        // it's OTEL env var settings OTEL_RESOURCE_ATTRIBUTES, OTEL_SERVICE_NAME
        _otelBuilder.ConfigureResource(r =>
        {
            _resourceDetectorLoader.AddResourceDetectors(r, _options);
        });
    }

    private void ConfigureMetrics()
    {
        if (!HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Metric)))
            return;

        _otelBuilder.WithMetrics(metrics =>
        {
            // Apply settings
            if (_options.Metric.Settings?.MetricLimit != null)
                metrics.SetMaxMetricStreams(_options.Metric.Settings.MetricLimit.Value);

            // add in tracing instrumentation options from config
            // TODO refac to just one call
            _options.Metric.Instrumentations?.ToList().ForEach(r => 
                _instrumentationLoader.AddMetricsInstrumentation(metrics, r));

            // add in meters
            if (_options.Metric.CustomMeters is not null)
                metrics.AddMeter(_options.Metric.CustomMeters.ToArray());

            // add exporters
            _exporterLoader.ConfigureExporters(metrics, _options);

            // TODO refac to just one call
            _options.Metric.Extensions?.ToList()?.ForEach(r => _extensionLoader.AddMetricsExtension(metrics, r));

        });
    }

    private void ConfigureTracing()
    {
        var shouldConfigureTracing = HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Trace));

        if (!shouldConfigureTracing)
            return;

        _otelBuilder.WithTracing(tracing =>
        {
            // set any settings            
            if (_options.Trace.Settings?.SetErrorStatusOnException.HasValue == true &&
                    _options.Trace.Settings?.SetErrorStatusOnException.Value == true)
                tracing.SetErrorStatusOnException(_options.Trace.Settings.SetErrorStatusOnException.Value);

            // add in tracing instrumentation options from config
            // TODO refac to just one call
            _options.Trace.Instrumentations?.ToList().ForEach(r => 
                _instrumentationLoader.AddTracingInstrumentation(tracing, r));

            // add trace sources from config
            if (_options.Trace.Sources is not null)
                tracing.AddSource(_options.Trace.Sources.ToArray());

            // add in sampler if set in config
            // TODO rename to SetSampler as there is only one snf only pass resourcebuilder
            // add integration tests to verify this doesnt break resourcebuilder config
            _samplerLoader.AddSampler(tracing, _options);

            // add exporters
            _exporterLoader.ConfigureExporters(tracing, _options);

            // Iterate over exporters for this montioring type
            // TODO refac to just one call
            _options.Trace.Extensions?.ToList()?.ForEach(r => _extensionLoader.AddTraceExtension(tracing, r));
        });
        
        // Add propagators
        _propagatorLoader.AddPropagators(_options);
    }

    private void ConfigureLogging()
    {
        if (!HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Log)))
            return;

        _otelBuilder.WithLogging(
            logging =>
            {
                // Iterate over exporters for this montioring type and add them
                _exporterLoader.ConfigureExporters(logging, _options);
                
                 // Iterate over exporters for this montioring type
                _options.Log.Extensions?.ToList()?.ForEach(r => _extensionLoader.AddLogExtension(logging, r));
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

    private bool HasSimpleOpenTelemetrySection(IConfiguration configuration, string sectionName)
    {
        var simpleOtelSection = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        return simpleOtelSection.GetSection(sectionName).Exists();
    }

}
