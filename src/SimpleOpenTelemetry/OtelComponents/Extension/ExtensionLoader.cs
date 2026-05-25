using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Extension;
using SimpleOpenTelemetry.Reflection;

namespace SimpleOpenTelemetry.OtelComponents.Extensions;

/// <summary>
/// Load assembly and invoke tracing/metrics extension method based on the available types
/// linked to TraceExtensionsEnum, MetricExtensionsEnum
/// eg. MetricExtensionsEnum.AspNetCore =
///         OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions.AddAspNetCoreInstrumentation()
///     in the separate nupkg OpenTelemetry.Instrumentation.AspNetCore
/// </summary>
internal class ExtensionLoader : LoaderBase, IExtensionLoader
{
    protected override string ComponentKind => "Extension";

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryExtensionLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public ExtensionLoader(IAssemblyExecution assemblyExecution) : base(assemblyExecution)
    {
    }

    /// <summary>
    /// Adds a log extension to the provided LoggerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="options">Log provider options</param>
    public void AddLogExtensions(LoggerProviderBuilder builder, SimpleOpenTelemetryLogOptions options) => 
        TryInvokeComponents(options.Extensions, builder, ExtensionAssemblies.KnownLogExtensions);

    
    /// <summary>
    /// Adds a trace extension to the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="options">Trace provider options</param>
    /// void AddMetricsExtensions(MeterProviderBuilder builder, SimpleOpenTelemetryMetricOptions options);
    public void AddTraceExtensions(TracerProviderBuilder builder, SimpleOpenTelemetryTraceOptions options) => 
        TryInvokeComponents(options.Extensions, builder, ExtensionAssemblies.KnownTraceExtensions);


    /// <summary>
    /// Adds a metric extension to the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="options">Metric provider options</param>
    public void AddMetricExtensions(MeterProviderBuilder builder, SimpleOpenTelemetryMetricOptions options) => 
        TryInvokeComponents(options.Extensions, builder, ExtensionAssemblies.KnownMetricExtensions);
    
    /// <summary>
    /// Adds a OpenTelemetryBuilder extension.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="options">Metric provider options</param>
    public void AddBuilderExtensions(IOpenTelemetryBuilder builder, SimpleOpenTelemetryOptions options) => 
        options.BuilderExtensions?.ToList().ForEach(x => TryInvokeComponent(x.Type, 
            builder, ExtensionAssemblies.KnownBuilderExtensions, options, (a,b,c) => x.Options)
        );
    
}
