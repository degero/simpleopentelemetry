using System.Reflection.Emit;
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
internal class ExporterLoader : LoaderBase, IExporterLoader
{
    protected override string ComponentKind => "Exporter";

    private readonly string eventCategory = nameof(ExporterLoader);


    /// <summary>
    /// Initializes a new instance of the ExporterLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public ExporterLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution)
    { }

    /// <summary>
    /// Configures metric exporters on the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads and configures exporters based on configuration. Exporters are loaded
    /// using reflection from their respective assemblies and registered with the provided builder.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="options">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    public void ConfigureExporters(MeterProviderBuilder builder, SimpleOpenTelemetryOptions options)
        => ConfigureExporters(builder, options,
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
    /// <param name="options">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    public void ConfigureExporters(TracerProviderBuilder builder, SimpleOpenTelemetryOptions options)
        => ConfigureExporters(builder, options,
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
    /// <param name="options">The SimpleOpenTelemetry configuration containing exporter settings.</param>
    public void ConfigureExporters(LoggerProviderBuilder builder, SimpleOpenTelemetryOptions options)
        => ConfigureExporters(builder, options,
            (name, cfg) => builder.AddOtlpExporter(name: name, configureExporter: cfg),
            ExporterAssemblies.KnownLogExporters);

    private IConfiguration? GetCustomExporterConfig<TEnum>(SimpleOpenTelemetryOptions config,
        SimpleOpenTelemetryExporterConfig<TEnum> exporterConfig
    )
    {
        // try get the top level exporter settings of the exporter name
        var exporterType = exporterConfig.Type?.ToString();
        var topConfigSection = config.ExporterOptions?.GetSection(exporterType!);
        if (topConfigSection is not null && topConfigSection!.Exists())
        {
            //  override with the output type options if they exist
            if (exporterConfig.Options is not null && exporterConfig.Options.Exists())
            {

                var merged = new ConfigurationBuilder()
                    .AddConfiguration(topConfigSection)    // base values
                    .AddConfiguration(exporterConfig.Options)  // overrides/adds on top
                    .Build();

                return merged;
            }
            else
                return topConfigSection;
        }
        else if (exporterConfig.Options is not null && exporterConfig.Options.Exists())
            return exporterConfig.Options;

        return null;
    }

    private void ConfigureExporters<TBuilder, TEnum>(TBuilder builder,
        SimpleOpenTelemetryOptions options,
        Action<string, Action<OtlpExporterOptions>> addOtlp,
        Dictionary<TEnum, AssemblyDescriptor> descriptors)
        where TEnum : struct, Enum
    {
        var signal = Util.GetSignalName<TBuilder>();
        List<SimpleOpenTelemetryExporterConfig<TEnum>> exporters = GetExportersForBuilder<TEnum>(options);
        var otlpExporterIdx = 0;
        var otlpExporterEnumName = nameof(TraceExporterEnum.Otlp);
        var otlpExporters = exporters.Where(r => r.Type?.Equals(otlpExporterEnumName, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        var otherExporters = exporters.Except(otlpExporters).ToList();

        foreach (var item in otlpExporters)
        {
            var rawType = item.Type.ToString();

            // If not set in this configsection, set through either the OpenTelemetry Env vars
            // or Configuration json that OpenTelemetry lib loads under a root "OpenTelemetryOTLPExporter" config section
            var config = GetCustomExporterConfig(options, item);
            // Dont use reflection as we have this built in the OpenTelemetry lib
            AddOTLPExporter<TEnum, TBuilder>(addOtlp, config, $"OTLPExporter-{signal}-{otlpExporterIdx++}");
        }

        foreach (var item in otherExporters)
        {
            if (TryGetDescriptor<TEnum, TBuilder>(item.Type, descriptors, out var descriptor, out var matchedEnum))
            {
                var config = descriptor!.OptionsClassName is not null ? GetCustomExporterConfig(options, item) : null;
                TryInvokeDescriptor<TBuilder>(item.Type, builder, descriptor, config);
            }
        }
    }

    private List<SimpleOpenTelemetryExporterConfig<TEnum>> GetExportersForBuilder<TEnum>(
        SimpleOpenTelemetryOptions options)
    {
        object? exporters = typeof(TEnum) switch
        {
            var t when t == typeof(MetricExporterEnum) => options.Metric.Exporters,
            var t when t == typeof(TraceExporterEnum)  => options.Trace.Exporters,
            _                                          => options.Log.Exporters
        };

        return (List<SimpleOpenTelemetryExporterConfig<TEnum>>?)exporters ?? new();
    }

    private void AddOTLPExporter<TEnum,TBuilder>(Action<string, Action<OtlpExporterOptions>> addExporter, 
        IConfiguration? options,
        string exporterName)
    {
        var builderName = typeof(TBuilder).Name;
        try 
        {
            // If not set in this configsection, set through either the OpenTelemetry Env vars
            // or Configuration json that OpenTelemetry lib loads under a root "OpenTelemetryOTLPExporter" config section
            addExporter(exporterName, BuildOtlpConfigAction(options));
            EventSource.Log.Verbose(eventCategory, $"Registered OpenTelemetry {ComponentKind} '{TraceExporterEnum.Otlp}' for builder '{builderName}' with name '{exporterName}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register OpenTelemetry {ComponentKind} '{TraceExporterEnum.Otlp}' for builder '{builderName} with name '{exporterName}'.", ex.Message);
        }
    }

    private Action<OtlpExporterOptions> BuildOtlpConfigAction(IConfiguration? options)
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
            return opts => {};
        }
    }
}
