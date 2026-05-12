using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.OtelComponents.Extension;
using SimpleOpenTelemetry.Reflection;
using SimpleOpenTelemetry.Utils;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.OtelComponents.Extensions;

/// <summary>
/// Load assembly and invoke tracing/metrics extension method based on the available types
/// linked to TraceExtensionsEnum, MetricExtensionsEnum
/// eg. MetricExtensionsEnum.AspNetCore =
///         OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions.AddAspNetCoreInstrumentation()
///     in the separate nupkg OpenTelemetry.Instrumentation.AspNetCore
/// </summary>
internal class ExtensionLoader : IExtensionLoader
{
    private readonly string eventCategory = nameof(ExtensionLoader);
    private readonly IAssemblyExecution _assemblyExec;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryExtensionLoader class.
    /// </summary>
    /// <param name="assemblyExecution">Handles loading and executing extensions.</param>
    public ExtensionLoader(IAssemblyExecution assemblyExecution)
    {
        _assemblyExec = assemblyExecution;
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
        options.Extensions?.ToList()?.ForEach(r => AddExtension(builder, r, ExtensionAssemblies.KnownLogExtensions));
    
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
        options.Extensions?.ToList()?.ForEach(r => AddExtension(builder, r, ExtensionAssemblies.KnownTraceExtensions));


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
        options.Extensions?.ToList()?.ForEach(r => AddExtension(builder, r, ExtensionAssemblies.KnownMetricExtensions));
    
    private void AddExtension<TBuilder, TEnum>(
        TBuilder builder,
        TEnum extension,
        Dictionary<TEnum, AssemblyDescriptor> descriptors)
    where TEnum : notnull
    {
        var signal = Util.GetSignalName<TBuilder>();
        if (!descriptors.TryGetValue(extension, out var descriptor))
        {
            EventSource.Log.Error(eventCategory, 
                $"{typeof(TEnum).Name} type '{extension}' not found to initialise {signal} extension." 
            );
            return;
        }

        var (assemblyName, typeName, methodName, _, _ ) = descriptor!;
      
        try
        {
           
            ReflectiveLoaderExecutor.InvokeBuilderExtension(
                _assemblyExec,
                builder,
                assemblyName,
                typeName,
                methodName!,
                null,
                null,
                "extension");

            EventSource.Log.Verbose(eventCategory, $"Registered {signal} extension '{extension}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} extension '{extension}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }
}
