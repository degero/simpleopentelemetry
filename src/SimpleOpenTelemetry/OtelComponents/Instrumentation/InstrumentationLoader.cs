using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Instrumentation;
using SimpleOpenTelemetry.Utils;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Exporter;

internal interface IInstrumentationLoader
{
    void AddMetricsInstrumentation(MeterProviderBuilder builder, MetricInstrumentationEnum instrumentation);
    void AddTracingInstrumentation(TracerProviderBuilder builder, TraceInstrumentationEnum instrumentation);
}

/// <summary>
/// Load assembly and invoke tracing/metrics instrumentation method based on the available types
/// linked to TraceInstrumentationEnum, MetricsInstrumentationEnum
/// eg. MetricsInstrumentationEnum.AspNetCore =
///         OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions.AddAspNetCoreInstrumentation()
///     in the seperate nupkg OpenTelemetry.Instrumentation.AspNetCore
/// </summary>
internal class InstrumentationLoader : IInstrumentationLoader
{
    private readonly string eventCategory = nameof(InstrumentationLoader);
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryInstrumentationLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public InstrumentationLoader(IConfiguration configuration)
    {
        _configuration = configuration;
        _assemblyExec = new AssemblyExecution();
    }

    /// <summary>
    /// Adds a tracing instrumentation to the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the instrumentation assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="instrumentation">The instrumentation type to add.</param>
    public void AddTracingInstrumentation(
    TracerProviderBuilder builder,
    TraceInstrumentationEnum instrumentation)
    => AddInstrumentation(builder, instrumentation, InstrumentationAssemblies.KnownTraceInstrumentations);

    /// <summary>
    /// Adds a metrics instrumentation to the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the instrumentation assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="instrumentation">The instrumentation type to add.</param>
    public void AddMetricsInstrumentation(
        MeterProviderBuilder builder,
        MetricInstrumentationEnum instrumentation)
        => AddInstrumentation(builder, instrumentation, InstrumentationAssemblies.KnownMetricsInstrumentations);

    private void AddInstrumentation<TBuilder, TEnum>(
    TBuilder builder,
    TEnum instrumentation,
    Dictionary<TEnum, InstrumentationExtensionDescriptor> descriptors)
    where TEnum : notnull
    {
        var signal = Util.GetSignalName<TBuilder>();

        if (!descriptors.TryGetValue(instrumentation, out var descriptor))
        {
            EventSource.Log.Error(eventCategory,
                $"{typeof(TEnum).Name} type '{instrumentation}' not found to initialise {signal} instrumentation.");
            return;
        }
            
        var (assemblyName, typeName, methodName, configurationSection) = descriptor!;

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
            if (descriptor.ConfigurationSection is not null &&
                actionMethod is not null &&
                parameterlessMethod is null)
            {
                throw new InvalidOperationException( // TODO chad add tests around these scenarios
                    $"Failed registration {builderTypeName} instrumentation: '{methodName}'. " +
                    $"A configuration section '{configurationSection}' is required but not found in config file.");
            }

            var section = descriptor.ConfigurationSection is not null ? _configuration.GetSection(descriptor.ConfigurationSection) : null;

            if (section is not null && section.Exists())
                _assemblyExec.InvokeWithAction(actionMethod, builder, section);
            else
                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

            EventSource.Log.Verbose(eventCategory, $"registered {signal} instrumentation '{instrumentation}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} instrumentation '{instrumentation}' via {typeName}.{methodName}.", ex.Message);
        }
    }

}
