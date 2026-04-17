using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Utils;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Exporter;

internal interface IExporterLoader
{
    void ConfigureExporters(MeterProviderBuilder builder, SimpleOpenTelemetryOptions config);
    void ConfigureExporters(TracerProviderBuilder builder, SimpleOpenTelemetryOptions config);
    void ConfigureExporters(LoggerProviderBuilder builder, SimpleOpenTelemetryOptions config);
}

/// <summary>
/// Load vendor exporter assembly and invoke expoter method based on the available types
/// linked to [Log/Trace/Metric]ExporterEnum
/// </summary>
internal class ExporterLoader : IExporterLoader
{
    private readonly string eventCategory = nameof(ExporterLoader);

    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    private readonly string _exportersTopLevelConfigSectionName = "ExporterOptions";

    // Available 3rd parter exporters
    internal readonly Array _traceExporters = Enum.GetValues<TraceExporterEnum>();
    internal readonly Array _metricExporters = Enum.GetValues<MetricExporterEnum>();
    internal readonly Array _logExporters = Enum.GetValues<LogExporterEnum>();

    /// <summary>
    /// Initializes a new instance of the ExporterLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing exporter settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public ExporterLoader(IConfiguration configuration)
    {
        // TODO seems wrong Configuration is loaded in as the section for this lib
        _configuration = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        _assemblyExec = new AssemblyExecution();
    }

    /// <summary>
    /// Configures metric exporters on the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures exporters based on configuration. Exporters are loaded
    /// using reflection from their respective assemblies and registered with the provided builder.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="config">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    public void ConfigureExporters(MeterProviderBuilder builder, SimpleOpenTelemetryOptions config)
        => ConfigureExporters(builder, config.Metric.Exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configure: cfg),
            _metricExporters, ExporterAssemblies.KnownMetricsExporters);

    /// <summary>
    /// Configures trace exporters on the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures trace exporters based on configuration. Exporters are loaded
    /// using reflection from their respective assemblies and registered with the provided builder.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="config">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    public void ConfigureExporters(TracerProviderBuilder builder, SimpleOpenTelemetryOptions config)
        => ConfigureExporters(builder, config.Trace.Exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configure: cfg),
            _traceExporters, ExporterAssemblies.KnownTraceExporters);

    /// <summary>
    /// Configures log exporters on the provided LoggerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures log exporters based on configuration. Exporters are loaded
    /// using reflection from their respective assemblies and registered with the provided builder.
    /// </remarks>
    /// <param name="builder">The LoggerProviderBuilder to configure.</param>
    /// <param name="config">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    public void ConfigureExporters(LoggerProviderBuilder builder, SimpleOpenTelemetryOptions config)
        => ConfigureExporters(builder, config.Log.Exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configureExporter: cfg),
            _logExporters, ExporterAssemblies.KnownLogExporters);

    private IConfiguration? GetCustomExporterConfig(
        SimpleOpenTelemetryExporterConfig config
    )
    {
        // try get the top level exporter settings 
        var topConfigSection = _configuration.GetSection(_exportersTopLevelConfigSectionName).GetSection(config.Type.ToString());
        if (topConfigSection is not null && topConfigSection!.Exists())
        {
            //  override with the output type options if they exist
            if (config.Options is not null && config.Options.Exists())
            {

                var merged = new ConfigurationBuilder()
                    .AddConfiguration(topConfigSection)    // base values
                    .AddConfiguration(config.Options)  // overrides/adds on top
                    .Build();

                return merged;
            }
            else
                return topConfigSection;
        }
        else if (config.Options is not null && config.Options.Exists())
            return config.Options;

        return null;
    }

    private void ConfigureExporters<TBuilder, TEnum>(TBuilder builder,
        IList<SimpleOpenTelemetryExporterConfig> exporters,
        Action<string, Action<OtlpExporterOptions>> addOtlp,
        Array validExporterTypes,
        Dictionary<TEnum, ExporterExtensionDescriptor> descriptors)
    {
        // Determine the valid exporters for the given builder type
        var validExporters = validExporterTypes.Cast<object>()
            .Select(e => e.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var signal = Util.GetSignalName<TBuilder>();

        for (var i = 0; i < exporters.Count; i++)
        {
            var item = exporters[i];

            if (item.Type == SimpleOpenTelemetryExporterType.Otlp)
            {
                // Dont use reflection as we have this builtin to OpenTelemetry lib
                AddOTLPExporter(addOtlp, item, $"OTLPExporter-{i}");
            }
            else if (validExporters.Cast<object>().Any(e => string.Equals(e.ToString(), item.Type.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                var matchedExporter = (TEnum)Enum.Parse(typeof(TEnum), item.Type.ToString(), ignoreCase: true);

                if (!descriptors.TryGetValue(matchedExporter, out var descriptor))
                {
                    EventSource.Log.Error(eventCategory, 
                        $"{typeof(TEnum).Name} type '{matchedExporter}' not found to initialise {signal} exporter.");
                }
                else 
                {
                    // TODO: fix to use a flag if this vendor lib has options, refac and clean all this crap codegen up
                    var config = descriptor.OptionsClassName is not null ? GetCustomExporterConfig(item) : null;
                    AddExporter(matchedExporter, builder, descriptor, config, signal);
                }
            }
            else
            {
                // Throw an exception on an unknown exporter type
                EventSource.Log.Error(eventCategory, $"Unsupported otel {signal} exporter type '{item.Type}'. Please check your SimpleOpenTelemetry configuration.");
            }
        }
    }


    private void AddOTLPExporter(Action<string, Action<OtlpExporterOptions>> addExporter, SimpleOpenTelemetryExporterConfig item, string exporterName)
        => addExporter(exporterName, BuildOtlpConfig(item));

    private void AddExporter<TBuilder,TEnum>(
        TEnum exporterEnum,
        TBuilder builder,
        ExporterExtensionDescriptor descriptor,
        IConfiguration? section,
        string signal)
    {
        var (assemblyName, typeName, methodName, optionsClassName) = descriptor;

        try
        {
            var assembly = _assemblyExec.GetAssembly(assemblyName);
            var builderType = typeof(TBuilder);
            var builderTypeName = builder.GetType().Name;

            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

            var parameterlessMethod = _assemblyExec.FindParameterlessMethod(type, builderType, descriptor.MethodName);
            var actionMethod = _assemblyExec.FindActionOverload(type, builderType, descriptor.MethodName);


            // attempt Action<TOptions> path only when section exists in config
            if (descriptor.OptionsClassName is not null &&
                parameterlessMethod is null &&
                section is null)
            {
                throw new InvalidOperationException(
                    $"Failed registration {builderTypeName} exporter: '{methodName}'. " +
                    $"A 'options' section '{optionsClassName}' is required but not found in config file.");
            }

            if (section is not null)
                _assemblyExec.InvokeWithAction(actionMethod, builder, section);
            else
                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

            EventSource.Log.Verbose(eventCategory, $"registered {signal} exporter '{exporterEnum}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} exporter '{exporterEnum}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }

    private Action<OtlpExporterOptions> BuildOtlpConfig(SimpleOpenTelemetryExporterConfig item)
    {
        // If options are passed, bind to OtlpExporterOptions structure
        if (item.Options is not null)
        {
            return config =>
            {
                OtlpExporterOptions options = new();
                var section = item.Options;
                section.Bind(options);
            };
        }
        else
        {
            // If not set in this configsection, set through either the OpenTelemetry Env vars
            // or Configuration json that OpenTelemetry lib loads under a root "OpenTelemetryOTLPExporter" config section
            return null;
        }
    }
}
