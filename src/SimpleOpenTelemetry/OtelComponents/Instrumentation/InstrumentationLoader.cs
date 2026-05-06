using Microsoft.Extensions.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using SimpleOpenTelemetry.Utils;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

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
    private readonly IAssemblyExecution _assemblyExec;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryInstrumentationLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public InstrumentationLoader(IConfiguration configuration,
        IAssemblyExecution assemblyExecution)
    {
        _configuration = configuration;
        _assemblyExec = assemblyExecution;
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
            
            var section = configurationSection is not null ? _configuration.GetSection(configurationSection) : null;
            ReflectiveLoaderExecutor.InvokeBuilderExtension(
                _assemblyExec,
                builder,
                assemblyName,
                typeName,
                methodName,
                section,
                configurationSection,
                "instrumentation");

            EventSource.Log.Verbose(eventCategory, $"Registered {signal} instrumentation '{instrumentation}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} instrumentation '{instrumentation}' via {typeName}.{methodName}.", ex.Message);
        }
    }
}
