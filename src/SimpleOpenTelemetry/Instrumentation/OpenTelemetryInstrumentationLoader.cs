using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Instrumentation;
using SimpleOpenTelemetry.Instrumenttaion;

namespace SimpleOpenTelemetry.Utils;

/// <summary>
/// Load assembly and invoke tracing/metrics instrumentation method based on the available types
/// linked to TracingInstrumentationEnum, MetricsInstrumentationEnum
/// eg. MetricsInstrumentationEnum.AspNetCore =
///         OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions.AddAspNetCoreInstrumentation()
///     in the seperate nupkg OpenTelemetry.Instrumentation.AspNetCore
/// </summary>
public class OpenTelemetryInstrumentationLoader
{
    private readonly IConfiguration _configuration;
    private readonly AssemblyExecution _assemblyExec;

    public OpenTelemetryInstrumentationLoader(IConfiguration configuration)
    {
        _configuration = configuration;
        _assemblyExec = new AssemblyExecution();
    }

    public void AddTracingInstrumentation(
    TracerProviderBuilder builder,
    TracingInstrumentationEnum instrumentation,
    ILogger? logger = null)
    => AddInstrumentation(builder, instrumentation, InstrumentationAssemblies.KnownTraceInstrumentations, logger);

    public void AddMetricsInstrumentation(
        MeterProviderBuilder builder,
        MetricsInstrumentationEnum instrumentation,
        ILogger? logger = null)
        => AddInstrumentation(builder, instrumentation, InstrumentationAssemblies.KnownMetricsInstrumentations, logger);

    private void AddInstrumentation<TBuilder, TEnum>(
    TBuilder builder,
    TEnum instrumentation,
    Dictionary<TEnum, InstrumentationExtensionDescriptor> descriptors,
    ILogger? logger = null)
    where TEnum : notnull
    {
        if (!descriptors.TryGetValue(instrumentation, out var descriptor))
            throw new InvalidOperationException(
                $"Critical: {typeof(TEnum).Name} type not found: {instrumentation} to initialise instrumentation");

        var assembly = _assemblyExec.GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor, logger);
    }


    private void TryInvokeExtension<TBuilder>(
        TBuilder builder,
        Assembly assembly,
        InstrumentationExtensionDescriptor descriptor,
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
                throw new InvalidOperationException(
                    $"Failed registration {builderTypeName} instrumentation: '{methodName}'. " +
                    $"A configuration section '{configurationSection}' is required but not found in config file.");
            }

            var section = descriptor.ConfigurationSection is not null ? _configuration.GetSection(descriptor.ConfigurationSection) : null;

            if (section is not null && section.Exists())
                _assemblyExec.InvokeWithAction(actionMethod, builder, section);
            else
                _assemblyExec.InvokeParameterless(type, builderType, methodName, builder);

            logger?.LogInformation("Successfully registered {TBuilder} instrumentation: {Method}", builderTypeName, methodName);

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register otel instrumentation via {typeName}.{methodName}", ex);
        }
    }

}
