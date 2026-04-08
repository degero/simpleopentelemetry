using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Extensions;

internal interface IExtensionLoader
{
    void AddMetricsExtension(MeterProviderBuilder builder, MetricExtensionsEnum extension, ILogger? logger = null);
    void AddTraceExtension(LoggerProviderBuilder builder, LogExtensionsEnum extension, ILogger? logger = null);
    void AddTraceExtension(TracerProviderBuilder builder, TraceExtensionsEnum extension, ILogger? logger = null);
}

/// <summary>
/// Load assembly and invoke tracing/metrics extension method based on the available types
/// linked to TraceExtensionsEnum, MetricExtensionsEnum
/// eg. MetricExtensionsEnum.AspNetCore =
///         OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions.AddAspNetCoreInstrumentation()
///     in the separate nupkg OpenTelemetry.Instrumentation.AspNetCore
/// </summary>
internal class ExtensionLoader : IExtensionLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryExtensionLoader class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    public ExtensionLoader(IConfiguration configuration)
    {
        _configuration = configuration;
        _assemblyExec = new AssemblyExecution();
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
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when extension type is not found or registration fails.</exception>
    public void AddTraceExtension(
    LoggerProviderBuilder builder,
    LogExtensionsEnum extension,
    ILogger? logger = null)
    => AddExtension(builder, extension, ExtensionAssemblies.KnownLogExtensions, logger);

    /// <summary>
    /// Adds a trace extension to the provided TracerProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The TracerProviderBuilder to configure.</param>
    /// <param name="extension">The trace extension type to add.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when extension type is not found or registration fails.</exception>
    public void AddTraceExtension(
    TracerProviderBuilder builder,
    TraceExtensionsEnum extension,
    ILogger? logger = null)
    => AddExtension(builder, extension, ExtensionAssemblies.KnownTraceExtensions, logger);

    /// <summary>
    /// Adds a metrics extension to the provided MeterProviderBuilder.
    /// </summary>
    /// <remarks>
    /// Dynamically loads the extension assembly and invokes the appropriate extension method.
    /// Configuration can be provided via appsettings.json or environment variables.
    /// </remarks>
    /// <param name="builder">The MeterProviderBuilder to configure.</param>
    /// <param name="extension">The metrics extension type to add.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <exception cref="InvalidOperationException">Thrown when extension type is not found or registration fails.</exception>
    public void AddMetricsExtension(
        MeterProviderBuilder builder,
        MetricExtensionsEnum extension,
        ILogger? logger = null)
        => AddExtension(builder, extension, ExtensionAssemblies.KnownMetricExtensions, logger);

    private void AddExtension<TBuilder, TEnum>(
    TBuilder builder,
    TEnum extension,
    Dictionary<TEnum, ExtensionDescriptor> descriptors,
    ILogger? logger = null)
    where TEnum : notnull
    {
        if (!descriptors.TryGetValue(extension, out var descriptor))
            throw new InvalidOperationException(
                $"Critical: {typeof(TEnum).Name} type not found: {extension} to initialise extension");

        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor, logger);
    }


    private void TryInvokeExtension<TBuilder>(
        TBuilder builder,
        Assembly assembly,
        ExtensionDescriptor descriptor,
        ILogger? logger)
    {
        var (assemblyName, typeName, methodName, configurationSection) = descriptor;

        try
        {
            var builderType = typeof(TBuilder);
            var builderTypeName = builder.GetType().Name;

            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Critical error: Type '{typeName}' not found in {assembly.GetName().Name}");

            var parameterlessMethod = _assemblyExec.FindParameterlessMethod(type, builderType, descriptor.MethodName);
            var actionMethod = _assemblyExec.FindActionOverload(type, builderType, descriptor.MethodName);

            // attempt Action<TOptions> path only when section exists in config
            if (descriptor.ConfigurationSection is not null &&
                actionMethod is not null &&
                parameterlessMethod is null)
            {
                throw new InvalidOperationException( // TODO chad add tests around these scenarios
                    $"Failed registration {builderTypeName} extension: '{methodName}'. " +
                    $"A configuration section '{configurationSection}' is required but not found in config file.");
            }

            var section = descriptor.ConfigurationSection is not null ? _configuration.GetSection(descriptor.ConfigurationSection) : null;

            if (section is not null && section.Exists())
                _assemblyExec.InvokeWithAction(actionMethod, builder, section);
            else
                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

            logger?.LogInformation("Successfully registered {TBuilder} extension: {Method}", builderTypeName, methodName);

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel extension via {typeName}.{methodName}", ex);
        }
    }

}
