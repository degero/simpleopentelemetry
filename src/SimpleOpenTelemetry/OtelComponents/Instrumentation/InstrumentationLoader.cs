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
internal class InstrumentationLoader : LoaderBase, IInstrumentationLoader
{
    protected override string ComponentKind => "Instrumentation";

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryInstrumentationLoader class.
    /// </summary>
    /// <param name="assemblyExecution">To invoke insturemtation library registration</param>
    public InstrumentationLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution)
    {
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
        TryInvokeComponents(options.Trace.Instrumentations, builder, 
            InstrumentationAssemblies.KnownTraceInstrumentations, 
            options, 
            (descriptor, sotelOptions, componentName) =>
                {
                    return descriptor.OptionsClassName is not null ? sotelOptions.Trace?.InstrumentationConfig?.GetSection(componentName!) : null;
                }
        );
    }

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
         TryInvokeComponents(options.Metric.Instrumentations, builder, 
            InstrumentationAssemblies.KnownMetricsInstrumentations, 
            options, 
            (descriptor, sotelOptions, componentName) =>
                {
                    return descriptor.OptionsClassName is not null ? sotelOptions.Metric?.InstrumentationConfig?.GetSection(componentName!) : null;
                }
        );
    }
}
