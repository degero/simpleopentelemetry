using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using SimpleOpenTelemetry.Utils;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Exporter;

/// <summary>
/// Load otel-contrib and vendor exporter assembly and invoke exporter method based on the available types
/// linked to [Log/Trace/Metric]ExporterEnum
/// </summary>
internal class ExporterLoader : IExporterLoader
{
    private readonly string eventCategory = nameof(ExporterLoader);

    private readonly IConfiguration _configuration;
    private readonly IAssemblyExecution _assemblyExec;

    private readonly string _exportersTopLevelConfigSectionName = "ExporterOptions";

    /// <summary>
    /// Initializes a new instance of the ExporterLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration containing exporter settings.</param>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public ExporterLoader(IConfiguration configuration,
        IAssemblyExecution assemblyExecution)
    {
        _configuration = configuration.GetSection(SimpleOpenTelemetryOptions.SectionName);
        _assemblyExec = assemblyExecution;
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
            ExporterAssemblies.KnownMetricExporters);

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
            ExporterAssemblies.KnownTraceExporters);

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
            ExporterAssemblies.KnownLogExporters);

    private IConfiguration? GetCustomExporterConfig<TEnum>(
        SimpleOpenTelemetryExporterConfig<TEnum> config
    )
    {
        // try get the top level exporter settings of the exporter name
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
        IList<SimpleOpenTelemetryExporterConfig<TEnum>> exporters,
        Action<string, Action<OtlpExporterOptions>> addOtlp,
        Dictionary<TEnum, ExporterExtensionDescriptor> descriptors)
        where TEnum : struct, Enum
    {
        var signal = Util.GetSignalName<TBuilder>();

        for (var i = 0; i < exporters.Count; i++)
        {
            var item = exporters[i];
            var rawType = item.Type.ToString();

            if (LoaderEnumHelper.TryParseKnown<TEnum>(rawType, out var matchedExporter))
            {
                if (string.Equals(nameof(TraceExporterEnum.Otlp), matchedExporter.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // Dont use reflection as we have this built in the OpenTelemetry lib
                    AddOTLPExporter(addOtlp, item, $"OTLPExporter-{signal}-{i}", signal);
                }
                else if (!descriptors.TryGetValue(matchedExporter, out var descriptor))
                {
                    EventSource.Log.Error(eventCategory, 
                        $"{typeof(TEnum).Name} type '{matchedExporter}' not found to initialise {signal} exporter.");
                }
                else 
                {
                    var config = descriptor.OptionsClassName is not null ? GetCustomExporterConfig(item) : null;
                    AddExporter(matchedExporter, builder, descriptor, config, signal);
                }
            }
            else
            {
                // Throw an exception on an unknown exporter type
                EventSource.Log.Error(eventCategory, $"Unsupported OpenTelemetry {signal} exporter type '{item.Type}'. Please check your SimpleOpenTelemetry configuration.");
            }
        }
    }


    private void AddOTLPExporter<TEnum>(Action<string, Action<OtlpExporterOptions>> addExporter, SimpleOpenTelemetryExporterConfig<TEnum> item, string exporterName, string signal)
    {
        try 
        {
            addExporter(exporterName, BuildOtlpConfig(item.Options));
            EventSource.Log.Verbose(eventCategory, $"Registered {signal} exporter '{TraceExporterEnum.Otlp}' '{exporterName}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} OTLP exporter via '{exporterName}'.", ex.Message);
        }
    }

    private void AddExporter<TBuilder,TEnum>(
        TEnum exporterEnum,
        TBuilder builder,
        ExporterExtensionDescriptor descriptor,
        IConfiguration? section,
        string signal)
    {
        var (assemblyName, typeName, methodName, optionsClassName, optionsRequired) = descriptor;

        try
        {
            ReflectiveLoaderExecutor.InvokeBuilderExtension(
                _assemblyExec,
                builder,
                assemblyName,
                typeName,
                methodName,
                section,
                optionsClassName,
                "exporter");

            EventSource.Log.Verbose(eventCategory, $"Registered {signal} exporter '{exporterEnum}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} exporter '{exporterEnum}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }

    private Action<OtlpExporterOptions> BuildOtlpConfig(IConfigurationSection? options)
    {
        // If options are passed, bind to OtlpExporterOptions structure
        if (options is not null && options.GetChildren().Count() > 0)
        {
            return config =>
            {
                var section = options;
                section.Bind(config);
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
