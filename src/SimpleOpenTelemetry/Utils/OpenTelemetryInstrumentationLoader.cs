using System.Reflection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Builder;

namespace SimpleOpenTelemetry.Utils;

/// <summary>
///
/// </summary>
public class OpenTelemetryInstrumentationLoader
{

    public void AddTracingInstrumentation(
    TracerProviderBuilder builder,
    TracingInstrumentationEnum feature,
    ILogger? logger = null)
    => AddInstrumentation(builder, feature, KnownTraceInstrumentations, logger);

    public void AddMetricsInstrumentation(
        MeterProviderBuilder builder,
        MetricsInstrumentationEnum feature,
        ILogger? logger = null)
        => AddInstrumentation(builder, feature, KnownMetricsInstrumentations, logger);

    private void AddInstrumentation<TBuilder, TEnum>(
    TBuilder builder,
    TEnum feature,
    Dictionary<TEnum, InstrumentationDescriptor> descriptors,
    ILogger? logger = null)
    where TEnum : notnull
    {
        if (!descriptors.TryGetValue(feature, out var descriptor))
            throw new InvalidOperationException(
                $"Critical: {typeof(TEnum).Name} type not found: {feature} to initialise instrumentation");

        var assembly = GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor.TypeName, descriptor.MethodName, logger);

        //if (!featureEnumMethods.ContainsKey(feature))
        //    throw new Exception($"Critical: {typeof(TEnum).Name} type not found: {feature}");

        //var methodName = featureEnumMethods[feature];

        //var knownInstrumentation = KnownTraceInstrumentations
        //    .Single(r => r.Value.MethodName == methodName);

        //var assembly = GetAssembly(knownInstrumentation.Key, logger);

        //TryInvokeExtension<TBuilder>(builder, assembly, knownInstrumentation.Value.TypeName, knownInstrumentation.Value.MethodName, logger);
    }


    private record InstrumentationDescriptor(
        string AssemblyName,
        string TypeName,
        string MethodName
    );

    private static readonly Dictionary<TracingInstrumentationEnum, InstrumentationDescriptor>
        KnownTraceInstrumentations = new()
        {
            [TracingInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Trace.AspNetCoreInstrumentationTracerProviderBuilderExtensions",
                "AddAspNetCoreInstrumentation"),
            [TracingInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Trace.HttpClientInstrumentationTracerProviderBuilderExtensions",
                "AddHttpClientInstrumentation"),
            [TracingInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Trace.SqlClientInstrumentationTracerProviderBuilderExtensions",
                "AddSqlClientInstrumentation"),
            [TracingInstrumentationEnum.EFCore] = new(
                "OpenTelemetry.Instrumentation.EntityFrameworkCore",
                "OpenTelemetry.Trace.EntityFrameworkInstrumentationTracerProviderBuilderExtensions",
                "AddEntityFrameworkCoreInstrumentation"),
        };

    private static readonly Dictionary<MetricsInstrumentationEnum, InstrumentationDescriptor>
        KnownMetricsInstrumentations = new()
        {
            [MetricsInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions",
                "AddAspNetCoreInstrumentation"),
            [MetricsInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Metrics.HttpClientInstrumentationMeterProviderBuilderExtensions",
                "AddHttpClientInstrumentation"),
            [MetricsInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Metrics.SqlClientInstrumentationMeterProviderBuilderExtensions",
                "AddSqlClientInstrumentation"),
            [MetricsInstrumentationEnum.Runtime] = new(
                "OpenTelemetry.Instrumentation.Runtime",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddRuntimeInstrumentation"),
        };

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


    //private readonly Dictionary<TracingInstrumentationEnum, string> _tracingFeatureEnumMethods = new()
    //{
    //    { TracingInstrumentationEnum.HttpClient, "AddHttpClientInstrumentation" },
    //    { TracingInstrumentationEnum.SqlClient, "AddSqlClientInstrumentation" },
    //    { TracingInstrumentationEnum.AspNetCore, "AddAspNetCoreInstrumentation" },
    //    { TracingInstrumentationEnum.EFCore, "AddEntityFrameworkCoreInstrumentation" },
    //};

    //private readonly Dictionary<MetricsInstrumentationEnum, string> _metricsFeatureEnumMethods = new()
    //{
    //    { MetricsInstrumentationEnum.Runtime, "AddRuntimeInstrumentation" },
    //    { MetricsInstrumentationEnum.SqlClient, "AddSqlClientInstrumentation" },
    //    { MetricsInstrumentationEnum.AspNetCore, "AddAspNetCoreInstrumentation" },
    //    { MetricsInstrumentationEnum.HttpClient, "AddHttpClientInstrumentation" },
    //};


    //public void AddTracingInstrumentation(
    //  TracerProviderBuilder builder,
    //  TracingInstrumentationEnum tracingFeature, 
    //  ILogger? logger = null)
    //{

    //    if (!_tracingFeatureEnumMethods.ContainsKey(tracingFeature))
    //        throw new Exception("Critical: TracingInstrumentationEnum type not found: " + tracingFeature);

    //    var tracingFeatureType = _tracingFeatureEnumMethods.Where(r => r.Key == tracingFeature).Single();

    //    var knownInstrumentation = KnownTraceInstrumentations.Where(r => r.Value.MethodName == tracingFeatureType.Value).Single();

    //    var assembly = GetAssembly(knownInstrumentation.Key, logger);

    //    TryInvokeExtension<TracerProviderBuilder>(builder, assembly, knownInstrumentation.Value.TypeName, knownInstrumentation.Value.MethodName, logger);
    //}


    //public void AddMetricsInstrumentation(
    //  MeterProviderBuilder builder,
    //  MetricsInstrumentationEnum metricsInstrumentation,
    //  ILogger? logger = null)
    //{

    //    if (!_metricsFeatureEnumMethods.ContainsKey(metricsInstrumentation))
    //        throw new Exception("Critical: MetricsInstrumentationEnum type not found: " + metricsInstrumentation);

    //    var metricsInstrumatationType = _metricsFeatureEnumMethods.Where(r => r.Key == metricsInstrumentation).Single();

    //    var knownInstrumentation = KnownTraceInstrumentations.Where(r => r.Value.MethodName == metricsInstrumatationType.Value).Single();

    //    var assembly = GetAssembly(knownInstrumentation.Key, logger);

    //    TryInvokeExtension<MeterProviderBuilder>(builder, assembly, knownInstrumentation.Value.TypeName, knownInstrumentation.Value.MethodName, logger);
    //}

    ///// <summary>
    ///// TODO Chad remove
    ///// </summary>
    ///// <param name="builder"></param>
    ///// <param name="logger"></param>
    ///// <returns></returns>
    //public void AddAvailableTracingInstrumentations(
    //    TracerProviderBuilder builder,
    //    ILogger? logger = null)
    //{
    //    foreach (var (assemblyName, (typeName, methodName)) in KnownTraceInstrumentations)
    //    {
    //        var assembly = GetAssembly(assemblyName, logger);

    //        TryInvokeExtension(builder, assembly, typeName, methodName, logger);
    //    }
    //}

}
