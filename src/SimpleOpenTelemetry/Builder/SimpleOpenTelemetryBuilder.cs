namespace SimpleOpenTelemetry.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Distro;
using SimpleOpenTelemetry.OtelComponents.Exporter;
using SimpleOpenTelemetry.OtelComponents.Extensions;
using SimpleOpenTelemetry.OtelComponents.Instrumentation;
using SimpleOpenTelemetry.OtelComponents.Propagator;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.OtelComponents.Sampler;
using SimpleOpenTelemetry.Reflection;
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
        IConfiguration config,
        IAssemblyExecution assemblyExecution,
        IInstrumentationLoader instrumentationLoader,
        IResourceDetectorLoader resourceDetectorLoader,
        IExporterLoader exporterLoader,
        ISamplerLoader samplerLoader,
        IPropagatorLoader propagatorLoader,
        IExtensionLoader extensionLoader,
        IDistroLoader distroLoader
        )
    {
        _configuration = config;
        _otelBuilder = otelBuilder;
        _assemblyExecution = assemblyExecution;
        _resourceDetectorLoader = resourceDetectorLoader;
        _exporterLoader = exporterLoader;
        _instrumentationLoader = instrumentationLoader;
        _samplerLoader = samplerLoader;
        _instrumentationLoader = instrumentationLoader;
        _propagatorLoader = propagatorLoader;
        _extensionLoader = extensionLoader;
        _distroLoader = distroLoader;
    }

    internal static SimpleOpenTelemetryBuilder Create(
        IOpenTelemetryBuilder otelBuilder,
        IConfiguration config)
    {
        var assemblyExecution = new AssemblyExecution();
        return new SimpleOpenTelemetryBuilder(
            otelBuilder,
            config,
            assemblyExecution,
            new InstrumentationLoader(assemblyExecution),
            new ResourceDetectorLoader(assemblyExecution),
            new ExporterLoader(assemblyExecution),
            new SamplerLoader(assemblyExecution),
            new PropagatorLoader(assemblyExecution),
            new ExtensionLoader(assemblyExecution),
            new DistroLoader(assemblyExecution));
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
        if (!ValidateConfigurationFormat(_configuration))
            return;

        BindConfigurationToSimpleOpenTelemetryOptions();

        // Check and load distro first - other settings can be appended
        _distroLoader.LoadDistro(_otelBuilder, _options);

        ConfigureResourceAttributes();

        ConfigureMetrics();

        ConfigureTracing();

        ConfigureLogging();

        _extensionLoader.AddBuilderExtensions(_otelBuilder, _options);

    }

    /// <summary>
    /// Validate SimpleOpenTelemetry configuration exists with base requirements in the root configuration
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static bool ValidateConfigurationFormat(IConfiguration configuration)
    {
        // Load in configuration
        var section = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);

        // validate that this config has simpleopentelemetry section exists() as 
        // simpleOpenTelemetryConfig will never be null if any type of config opject was bound
        if (!section.Exists())
        {
            EventSource.Log.CriticalEvent(eventCategory, $"No configuration section '{SimpleOpenTelemetryOptions.SectionName}' found. This is required for SimpleOpenTelemetry.");
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
                EventSource.Log.CriticalEvent(eventCategory, $"Missing signal configuration subsections in '{SimpleOpenTelemetryOptions.SectionName}'. Ensure defining at least one of Trace, Log or Metric subsection.");
                return false;
            }
        }
        return true;

    }

    private void BindConfigurationToSimpleOpenTelemetryOptions()
    {
        var section = _configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        var simpleOpenTelemetryConfig = new SimpleOpenTelemetryOptions();
        section.Bind(simpleOpenTelemetryConfig);
        _options = simpleOpenTelemetryConfig;
    }

    private void ConfigureResourceAttributes()
    {
        // 'EnvVar' detector only be needed if OpenTelemetry SDK changes to not include this by default
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
            _instrumentationLoader.AddMetricsInstrumentations(metrics, _options);

            // add in meters
            if (_options.Metric.CustomMeters is not null)
                metrics.AddMeter(_options.Metric.CustomMeters.ToArray());

            // add extensions
            _extensionLoader.AddMetricExtensions(metrics, _options.Metric);

            // add exporters
            _exporterLoader.ConfigureExporters(metrics, _options);
        });
    }

    private void ConfigureTracing()
    {
        if (!HasSimpleOpenTelemetrySection(_configuration, nameof(SimpleOpenTelemetryOptions.Trace)))
            return;

        _otelBuilder.WithTracing(tracing =>
        {
            // set any settings            
            if (_options.Trace.Settings?.SetErrorStatusOnException.HasValue == true &&
                    _options.Trace.Settings?.SetErrorStatusOnException.Value == true)
                tracing.SetErrorStatusOnException(_options.Trace.Settings.SetErrorStatusOnException.Value);

            // add in tracing instrumentation options from config
            _instrumentationLoader.AddTracingInstrumentations(tracing, _options);

            // add trace sources from config
            if (_options.Trace.Sources is not null)
                tracing.AddSource(_options.Trace.Sources.ToArray());

            // add in sampler if set in config
            // add integration tests to verify this doesnt break resourcebuilder config
            _samplerLoader.SetSampler(tracing, _options);

            // add extensions
            _extensionLoader.AddTraceExtensions(tracing, _options.Trace);

            // add exporters
            _exporterLoader.ConfigureExporters(tracing, _options);
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
                // add extensions
                _extensionLoader.AddLogExtensions(logging, _options.Log);

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

    private bool HasSimpleOpenTelemetrySection(IConfiguration configuration, string sectionName)
    {
        var simpleOtelSection = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        return simpleOtelSection.GetSection(sectionName).Exists();
    }

}
