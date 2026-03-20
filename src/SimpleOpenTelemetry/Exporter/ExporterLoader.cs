using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Exporter;

namespace SimpleOpenTelemetry.Utils;

/// <summary>
/// Load vendor exporter assembly and invoke expoter method based on the available types
/// linked to [Log/Trace/Metric]ExporterEnum
/// </summary>
public class ExporterLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

   // Available 3rd parter exporters
    internal readonly Array _traceExporters = Enum.GetValues<TraceExporterEnum>();
    internal readonly Array _metricExporters = Enum.GetValues<MetricExporterEnum>();
    internal readonly Array _logExporters = Enum.GetValues<LogExporterEnum>();

    public ExporterLoader(IConfiguration configuration)
    {
        _configuration = configuration;
        _assemblyExec = new AssemblyExecution();
    }

    public void ConfigureExporters(MeterProviderBuilder builder, IList<SimpleOpenTelemetryExporterConfig> exporters, ILogger logger)
        => ConfigureExporters(builder, MetricExporterEnum.Otlp, exporters, 
            (name, cfg) => builder.AddOtlpExporter(name: name, configure: cfg), 
            _metricExporters, ExporterAssemblies.KnownMetricsExporters, logger);

    public void ConfigureExporters(TracerProviderBuilder builder, IList<SimpleOpenTelemetryExporterConfig> exporters, ILogger logger)
        => ConfigureExporters(builder, TraceExporterEnum.Otlp, exporters, 
            (name, cfg) => builder.AddOtlpExporter(name: name, configure: cfg), 
            _traceExporters, ExporterAssemblies.KnownTraceExporters, logger);

    public void ConfigureExporters(LoggerProviderBuilder builder, IList<SimpleOpenTelemetryExporterConfig> exporters, ILogger logger)
        => ConfigureExporters(builder, LogExporterEnum.Otlp, exporters,
            (name, cfg) => builder.AddOtlpExporter(name: name, configureExporter: cfg), 
            _logExporters, ExporterAssemblies.KnownLogExporters, logger);

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
                // Dont use reflection as we have this lib
                AddOTLPExporter(addOtlp, item, $"OTLPExporter-{i}");
            }
            else if (validExporters.Cast<object>().Any(e => string.Equals(e.ToString(), item.Type.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                var matchedExporter = Enum.Parse(typeof(TEnum), item.Type.ToString(), ignoreCase: true);

                if (!descriptors.TryGetValue((TEnum)matchedExporter , out var descriptor))
                    throw new InvalidOperationException(
                        $"Critical: {typeof(TEnum).Name} type not found: {matchedExporter} to initialise exporter");

                // TODO: fix issue where metrics exporter needs arguments for azure. refac and clean all this crap codegen up
                AddExporter(builder, descriptor, logger);

            
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
    ILogger? logger = null)
    {
       
        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor, logger);
    }


    private void TryInvokeExtension<TBuilder>(
        TBuilder builder,
        Assembly assembly,
        ExporterExtensionDescriptor descriptor,
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
                actionMethod is not null &&
                parameterlessMethod is null)
            {
                throw new InvalidOperationException(
                    $"Failed registration {builderTypeName} instrumentation: '{methodName}'. " +
                    $"A 'options' section '{optionsClassName}' is required but not found in config file.");
            }

            var section = descriptor.OptionsClassName is not null ? _configuration.GetSection(descriptor.OptionsClassName) : null;

            if (section is not null && section.Exists())
                _assemblyExec.InvokeWithAction(actionMethod, builder, section);
            else
                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

            logger?.LogInformation("Successfully registered {TBuilder} exporter: {Method}", builderTypeName, methodName);

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel expoter via {typeName}.{methodName}", ex);
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
