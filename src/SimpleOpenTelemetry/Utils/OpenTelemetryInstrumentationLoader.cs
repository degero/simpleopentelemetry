using System.Reflection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;
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

        var assembly = GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor.TypeName, descriptor.MethodName, logger);
    }

    private Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();
    private Assembly GetAssembly(string assemblyName, ILogger logger)
    {
        if (_loadedAssemblies.Keys.Contains(assemblyName))
        {
            return _loadedAssemblies[assemblyName];
        }
        else
        {
            var assembly = TryLoadAssembly(assemblyName, logger);
            if (assembly == null)
                throw new Exception($"Critical SimpleOpenTelemetry error: Cannot load instrumentation assembly {assemblyName}");
            return assembly;
        }
    }
    private Assembly? TryLoadAssembly(string assemblyName, ILogger? logger)
    {
        // Check if already loaded first
        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (existing != null) return existing;

        // TODO chad test this in win / linux deployments etc
        // Try to load from base directory (i.e. user has the package installed)
        var path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        if (!File.Exists(path))
        {
            logger?.LogDebug("Instrumentation assembly not found, skipping: {Assembly}", assemblyName);
            return null;
        }

        try
        {
            var loaded = Assembly.LoadFrom(path);
            logger?.LogInformation("Loaded instrumentation assembly: {Assembly}", assemblyName);
            return loaded;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to load instrumentation assembly: {Assembly}", assemblyName);
            return null;
        }
    }
    private void TryInvokeExtension<TBuilder>(
        TBuilder builder,
        Assembly assembly,
        string typeName,
        string methodName,
        ILogger? logger)
    {
        try
        {
            var type = assembly.GetType(typeName)
                ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

            var method = type.GetMethod(methodName, new[] { typeof(TBuilder) })
                ?? throw new InvalidOperationException($"Method '{methodName}' not found on {typeName}");

            method.Invoke(null, new object[] { builder });

            logger?.LogInformation("Registered instrumentation: {Method}", methodName);
        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register instrumentation via {typeName}.{methodName}", ex);
        }
    }
}
