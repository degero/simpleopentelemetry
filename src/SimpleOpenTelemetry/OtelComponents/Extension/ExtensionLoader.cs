using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;
    private readonly IAssemblyExecution _assemblyExec;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryExtensionLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public ExtensionLoader(IConfiguration configuration,
        IAssemblyExecution assemblyExecution)
    {
        _configuration = configuration;
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
    /// <param name="extension">The trace extension type to add.</param>
    public void AddLogExtension(
        LoggerProviderBuilder builder,
        LogExtensionsEnum extension)
        => AddExtension(builder, extension, ExtensionAssemblies.KnownLogExtensions);

    /// <summary>
    /// Adds a trace extension to the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="extension">The trace extension type to add.</param>
    public void AddTraceExtension(
        TracerProviderBuilder builder,
        TraceExtensionsEnum extension)
        => AddExtension(builder, extension, ExtensionAssemblies.KnownTraceExtensions);

    /// <summary>
    /// Adds a metrics extension to the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="extension">The metrics extension type to add.</param>
    public void AddMetricsExtension(
        MeterProviderBuilder builder,
        MetricExtensionsEnum extension)
        => AddExtension(builder, extension, ExtensionAssemblies.KnownMetricExtensions);

    private void AddExtension<TBuilder, TEnum>(
        TBuilder builder,
        TEnum extension,
        Dictionary<TEnum, ExtensionDescriptor> descriptors)
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

        var (assemblyName, typeName, methodName, optionsClassName, optionsRequired ) = descriptor!;
      
        try
        {
           
            var section = optionsClassName is not null ? _configuration.GetSection(optionsClassName) : null;
            ReflectiveLoaderExecutor.InvokeBuilderExtension(
                _assemblyExec,
                builder,
                assemblyName,
                typeName,
                methodName,
                section,
                optionsRequired ? optionsClassName : null,
                "extension");

            EventSource.Log.Verbose(eventCategory, $"Registered {signal} extension '{extension}'.");

        }
        catch (Exception ex)
        {
            EventSource.Log.Error(eventCategory, $"Failed to register {signal} extension '{extension}' via '{typeName}.{methodName}'.", ex.Message);
        }
    }
}
