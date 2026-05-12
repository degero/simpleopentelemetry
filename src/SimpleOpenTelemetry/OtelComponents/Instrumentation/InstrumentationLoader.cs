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
    private readonly IAssemblyExecution _assemblyExec;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryInstrumentationLoader class.
    /// </summary>
    /// <param name="assemblyExecution">To invoke insturemtation library registration</param>
    public InstrumentationLoader(IAssemblyExecution assemblyExecution)
    {
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
    /// <param name="options">SimpleOpenTelemetryOptions to look up an instrumentationconfig</param>
    public void AddTracingInstrumentations(
        TracerProviderBuilder builder,
        SimpleOpenTelemetryOptions options
    )
    {
        options.Trace.Instrumentations?.ToList().ForEach(r => 
            AddTracingInstrumentation(builder, options, r));
    }

    private void AddTracingInstrumentation(
        TracerProviderBuilder builder,
        SimpleOpenTelemetryOptions options,
        TraceInstrumentationEnum instrumentation)
    => AddInstrumentation(builder, options, instrumentation, InstrumentationAssemblies.KnownTraceInstrumentations);

    /// <summary>
    /// Adds a metrics instrumentation to the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the instrumentation assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="options">SimpleOpenTelemetryOptions to look up an instrumentationconfig</param>
    public void AddMetricsInstrumentations(
        MeterProviderBuilder builder,
        SimpleOpenTelemetryOptions options
    )
    {
         options.Metric.Instrumentations?.ToList().ForEach(r => 
            AddMetricsInstrumentation(builder, options, r));
    }

    private void AddMetricsInstrumentation(
        MeterProviderBuilder builder,
        SimpleOpenTelemetryOptions options,
        MetricInstrumentationEnum instrumentation)
        => AddInstrumentation(builder, options, instrumentation, InstrumentationAssemblies.KnownMetricsInstrumentations);

    private void AddInstrumentation<TBuilder, TEnum>(
    TBuilder builder,
    SimpleOpenTelemetryOptions options,
    TEnum instrumentation,
    Dictionary<TEnum, AssemblyDescriptor> descriptors)
    where TEnum : notnull
    {
        var signal = Util.GetSignalName<TBuilder>();

        if (!descriptors.TryGetValue(instrumentation, out var descriptor))
        {
            EventSource.Log.Error(eventCategory,
                $"{typeof(TEnum).Name} type '{instrumentation}' not found to initialise {signal} instrumentation.");
            return;
        }
            
        var (assemblyName, typeName, methodName, optionsClassName, _) = descriptor!;
        var instrumentationName = instrumentation.ToString();
        
        try
        {
            var section = optionsClassName is not null ? options.Trace?.InstrumentationConfig?.GetSection(instrumentationName!) : null;
            ReflectiveLoaderExecutor.InvokeBuilderExtension(
                _assemblyExec,
                builder,
                assemblyName,
                typeName,
                methodName!,
                section,
                optionsClassName,
                "instrumentation");

            EventSource.Log.Verbose(eventCategory, $"Registered {signal} instrumentation '{instrumentation}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} instrumentation '{instrumentation}' via {typeName}.{methodName}.", ex.Message);
        }
    }
}
