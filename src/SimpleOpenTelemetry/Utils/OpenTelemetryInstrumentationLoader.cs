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

    public OpenTelemetryInstrumentationLoader(IConfiguration configuration)
    {
        _configuration = configuration;
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

        var assembly = GetAssembly(descriptor.AssemblyName, logger);

        TryInvokeExtension<TBuilder>(builder, assembly, descriptor, logger);
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
                throw new Exception($"Critical SimpleOpenTelemetry error: Cannot load instrumentation assembly {assemblyName}. " +
                    $"Ensure you have added the required nuget package to your project.");
            return assembly;
        }
    }

    private Assembly? TryLoadAssembly(string assemblyName, ILogger? logger)
    {
        // Check if already loaded first
        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (existing != null) 
            return existing;

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

            var parameterlessMethod = FindParameterlessMethod(type, builderType, descriptor.MethodName);
            var actionMethod = FindActionOverload(type, builderType, descriptor.MethodName);

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
                InvokeWithAction(actionMethod, builder, section);
            else
                InvokeParameterless(type, builderType, methodName, builder);

            logger?.LogInformation("Successfully registered {TBuilder} instrumentation: {Method}", builderTypeName, methodName);

        }
        catch (Exception ex)
        {
            throw new Exception($"SimpleOpenTelemetry Failed to register instrumentation via {typeName}.{methodName}", ex);
        }
    }

    // TODO Chad remove
    //private TracerProviderBuilder AddInstrumentationViaReflection(
    //TracerProviderBuilder builder,
    //InstrumentationExtensionDescriptor descriptor,
    //IConfiguration configuration)
    //{
    //    var assembly = Assembly.Load(descriptor.AssemblyName);
    //    var type = assembly.GetType(descriptor.TypeName)!;

    //    if (descriptor.ConfigurationSection is null)
    //    {
    //        // Truly parameterless
    //        var method = type.GetMethod(descriptor.MethodName,
    //            BindingFlags.Public | BindingFlags.Static,
    //            binder: null,
    //            types: new[] { typeof(TracerProviderBuilder) },
    //            modifiers: null)!;

    //        return (TracerProviderBuilder)method.Invoke(null, new object[] { builder })!;
    //    }

    //    // Find Action<TOptions> overload and discover TOptions from its signature
    //    var actionMethod = type
    //        .GetMethods(BindingFlags.Public | BindingFlags.Static)
    //        .FirstOrDefault(m =>
    //            m.Name == descriptor.MethodName &&
    //            m.GetParameters() is { Length: 2 } p &&
    //            p[0].ParameterType == typeof(TracerProviderBuilder) &&
    //            p[1].ParameterType.IsGenericType &&
    //            p[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>));

    //    if (actionMethod is null)
    //    {
    //        // No Action<T> overload exists — fall back to parameterless
    //        var method = type.GetMethod(descriptor.MethodName,
    //            BindingFlags.Public | BindingFlags.Static,
    //            binder: null,
    //            types: new[] { typeof(TracerProviderBuilder) },
    //            modifiers: null)!;

    //        return (TracerProviderBuilder)method.Invoke(null, new object[] { builder })!;
    //    }

    //    var optionsType = actionMethod.GetParameters()[1].ParameterType.GetGenericArguments()[0];
    //    var section = configuration.GetSection(descriptor.ConfigurationSection);

    //    // Bind config section → options instance
    //    var options = Activator.CreateInstance(optionsType)!;
    //    section.Bind(options);

    //    // Build Action<TOptions> that applies the bound instance
    //    var param = Expression.Parameter(optionsType, "opts");
    //    var source = Expression.Constant(options, optionsType);
    //    var assignments = optionsType
    //        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //        .Where(p => p.CanRead && p.CanWrite)
    //        .Select(p => (Expression)Expression.Assign(
    //            Expression.Property(param, p),
    //            Expression.Property(source, p)));

    //    var actionType = typeof(Action<>).MakeGenericType(optionsType);
    //    var configureAction = Expression
    //        .Lambda(actionType, Expression.Block(assignments), param)
    //        .Compile();

    //    return (TracerProviderBuilder)actionMethod.Invoke(
    //        null, new object[] { builder, configureAction })!;
    //}

    private MethodInfo? FindParameterlessMethod(
    Type type,
    Type builderType,
    string methodName)
    => type.GetMethod(
        methodName,
        BindingFlags.Public | BindingFlags.Static,
        binder: null,
        types: new Type[] { builderType },
        modifiers: null);

    private object InvokeParameterless(
    Type type,
    Type builderType,
    string methodName,
    object builder)
    {
        var method = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new Type[] { builderType },
            modifiers: null)
            ?? throw new InvalidOperationException(
                   $"No parameterless '{methodName}' method accepting {builderType.Name} found on {type.FullName}.");

        return method.Invoke(null, new object[] { builder })!;
    }

    private static object InvokeWithAction(
    MethodInfo actionMethod,
    object builder,
    IConfigurationSection section)
    {
        var optionsType = actionMethod.GetParameters()[1].ParameterType.GetGenericArguments()[0];
        var configureAction = BuildConfigureAction(optionsType, section);

        return actionMethod.Invoke(null, new object[] { builder, configureAction })!;
    }

    private MethodInfo? FindActionOverload(
        Type type,
        Type builderType,
        string methodName)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
        return methods.FirstOrDefault(m =>
                m.Name == methodName &&
                m.GetParameters() is { Length: 2 } p &&
                p[0].ParameterType == builderType &&
                p[1].ParameterType.IsGenericType &&
                p[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>));
    }

    private static object BuildConfigureAction(
    Type optionsType,
    IConfigurationSection section)
    {
        var options = Activator.CreateInstance(optionsType)!;
        section.Bind(options);

        var param = Expression.Parameter(optionsType, "opts");
        var source = Expression.Constant(options, optionsType);
        var assignments = optionsType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => (Expression)Expression.Assign(
                Expression.Property(param, p),
                Expression.Property(source, p)));

        return Expression
            .Lambda(typeof(Action<>).MakeGenericType(optionsType),
                    Expression.Block(assignments),
                    param)
            .Compile();
    }
}
