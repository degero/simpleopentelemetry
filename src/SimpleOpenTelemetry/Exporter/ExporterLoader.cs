using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Configuration;
using SimpleOpenTelemetry.Exporter;

namespace SimpleOpenTelemetry.Exporter;

/// <summary>
/// Load vendor exporter assembly and invoke expoter method based on the available types
/// linked to [Log/Trace/Metric]ExporterEnum
/// </summary>
internal class ExporterLoader
{
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
        _configuration = configuration.GetSection(SimpleOpenTelemetryConfiguration.SectionName);
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
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when builder or config is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when exporter registration fails.</exception>
    public void ConfigureExporters(MeterProviderBuilder builder, SimpleOpenTelemetryBuilderOptions config, ILogger logger)
        => ConfigureExporters(builder, MetricExporterEnum.Otlp, config.Metric.Exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configure: cfg),
            _metricExporters, ExporterAssemblies.KnownMetricsExporters, logger);

    /// <summary>
    /// Configures trace exporters on the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures trace exporters based on configuration. Exporters are loaded
    /// using reflection from their respective assemblies and registered with the provided builder.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="config">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when builder or config is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when exporter registration fails.</exception>
    public void ConfigureExporters(TracerProviderBuilder builder, SimpleOpenTelemetryBuilderOptions config, ILogger logger)
        => ConfigureExporters(builder, TraceExporterEnum.Otlp, config.Trace.Exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configure: cfg),
            _traceExporters, ExporterAssemblies.KnownTraceExporters, logger);

    /// <summary>
    /// Configures log exporters on the provided LoggerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures log exporters based on configuration. Exporters are loaded
    /// using reflection from their respective assemblies and registered with the provided builder.
    /// </remarks>
    /// <param name="builder">The LoggerProviderBuilder to configure.</param>
    /// <param name="config">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when builder or config is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when exporter registration fails.</exception>
    public void ConfigureExporters(LoggerProviderBuilder builder, SimpleOpenTelemetryBuilderOptions config, ILogger logger)
        => ConfigureExporters(builder, LogExporterEnum.Otlp, config.Log.Exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configureExporter: cfg), 
            _logExporters, ExporterAssemblies.KnownLogExporters, logger);

    private IConfiguration? GetCustomExporterConfig(
        ExporterExtensionDescriptor descriptor,
        SimpleOpenTelemetryExporterConfig config
    )
    {
        // try get the top level exporter settings 
        var topConfigSection = _configuration.GetSection(_exportersTopLevelConfigSectionName).GetSection(config.Type.ToString());
        if (topConfigSection is not null && topConfigSection!.Exists())
        {
            //  override with the output type options if they exist
            if (config.Options is not null &&
                config.Options.Exists())
            {

                var merged = new ConfigurationBuilder()
                    .AddConfiguration(topConfigSection)    // base values
                    .AddConfiguration(config.Options)  // overrides/adds on top
                    .Build();

                return merged;
                // return MergeConfigurations(topConfigSection, config.Options);
            }
            else
                return topConfigSection;
        } 
        else if (config.Options is not null &&
                config.Options.Exists())
        {
            return config.Options;
        }

        return null;
    }

    private void ConfigureExporters<TBuilder,TEnum>(TBuilder builder, TEnum enumExporter,
        IList<SimpleOpenTelemetryExporterConfig> exporters,
        Action<string, Action<OtlpExporterOptions>> addOtlp,
        Array validExporterTypes,
        Dictionary<TEnum, ExporterExtensionDescriptor> descriptors,
        ILogger logger)
    {
        // Determine the valid exporters for the given builder type
        var validExporters = validExporterTypes.Cast<object>()
            .Select(e => e.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
                var matchedExporter = Enum.Parse(typeof(TEnum), item.Type.ToString(), ignoreCase: true);

                if (!descriptors.TryGetValue((TEnum)matchedExporter , out var descriptor))
                    throw new InvalidOperationException(
                        $"Critical: {typeof(TEnum).Name} type not found: {matchedExporter} to initialise exporter");

                // TODO: fix to use a flag if this vendor lib has options, refac and clean all this crap codegen up
                var config = descriptor.OptionsClassName is not null ? GetCustomExporterConfig(descriptor, item) : null;

                AddExporter(builder, descriptor, config, logger);
            }
            else 
            {
                // Throw an exception on an unknown exporter type
                throw new InvalidOperationException($"Unsupported exporter type: {item.Type}. Please check your SimpleOpenTelemetry Configuration.");
            }
        }
    }
    

    private void AddOTLPExporter(Action<string, Action<OtlpExporterOptions>> addExporter, SimpleOpenTelemetryExporterConfig item, string exporterName)
        => addExporter(exporterName, BuildOtlpConfig(item));


    //private void AddTraceExporter(
    //TracerProviderBuilder builder,
    //TraceExporterEnum instrumentation,
    //ILogger? logger = null)
    //=> AddExporter(builder, instrumentation, ExporterAssemblies.KnownTraceExporters, logger);

    //private void AddMetricExporter(
    //    MeterProviderBuilder builder,
    //    MetricExporterEnum instrumentation,
    //    ILogger? logger = null)
    //    => AddExporter(builder, instrumentation, ExporterAssemblies.KnownMetricsExporters, logger);

    private void AddExporter<TBuilder>(
    TBuilder builder,
    ExporterExtensionDescriptor descriptor,
    IConfiguration? section,
    ILogger? logger = null)
    {
       
        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor, section, logger);
    }


    private void TryInvokeExtension<TBuilder>(
        TBuilder builder,
        Assembly assembly,
        ExporterExtensionDescriptor descriptor,
        IConfiguration? section,
        ILogger? logger)
    {
        var (assemblyName, typeName, methodName, optionsClassName) = descriptor;

        try
        {
            var builderType = typeof(TBuilder);
            var builderTypeName = builder.GetType().Name;

            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

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

            logger?.LogInformation("Successfully registered {TBuilder} exporter: {Method}", builderTypeName, methodName);

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel exporter via {typeName}.{methodName}", ex);
        }
    }

    private Action<OtlpExporterOptions> BuildOtlpConfig(SimpleOpenTelemetryExporterConfig item)
    {
        // If options are passed, bind to OtlpExporterOptions structure
        if (item.Options is not null)
        {
            return (Action<OtlpExporterOptions>)(config =>
            {
                OtlpExporterOptions options = new();
                var section = item.Options;
                section.Bind(options);
            });
        }
        else
        {
            // If not set in this configsection, set through either the OpenTelemetry Env vars
            // or Configuration json that OpenTelemetry lib loads under a root "OpenTelemetryOTLPExporter" config section
            return null;
        }
    }
}
