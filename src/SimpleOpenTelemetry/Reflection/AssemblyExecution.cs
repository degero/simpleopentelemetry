using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using System.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

namespace SimpleOpenTelemetry.Reflection;

/// <summary>
/// Provides utilities for dynamically loading assemblies and invoking extension methods via reflection.
/// </summary>
internal class AssemblyExecution : IAssemblyExecution
{
    private Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

    /// <summary>
    /// Gets a cached or newly loaded assembly by name.
    /// </summary>
    /// <remarks>
    /// Caches loaded assemblies to avoid redundant loading. First checks cache, then attempts to load from disk.
    /// </remarks>
    /// <param name="assemblyName">The name of the assembly to load (without .dll extension).</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="Exception">Thrown when assembly cannot be loaded.</exception>
    public virtual Assembly GetAssembly(string assemblyName)
    {
        if (_loadedAssemblies.Keys.Contains(assemblyName))
        {
            return _loadedAssemblies[assemblyName];
        }
        else
        {
            var assembly = TryLoadAssembly(assemblyName);
            if (assembly == null)
                throw new Exception($"Cannot load assembly '{assemblyName}'. " +
                    $"Ensure you have added the required nuget package to your project.");

            return assembly;
        }
    }

    /// <summary>
    /// Attempts to load an assembly by name from the application's base directory.
    /// </summary>
    /// <remarks>
    /// Checks already-loaded assemblies first, then attempts to load from disk.
    /// Returns null if the assembly is not found or loading fails.
    /// </remarks>
    /// <param name="assemblyName">The name of the assembly to load (without .dll extension).</param>
    /// <returns>The loaded assembly, or null if not found or loading failed.</returns>
    public virtual Assembly? TryLoadAssembly(string assemblyName)
    {
        // Check if already loaded first
        var existing = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (existing != null)
            return existing;

        // Try to load from base directory (i.e. user has the package installed)
        var path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var loaded = Assembly.LoadFrom(path);
            return loaded;
        }
        catch (Exception ex)
        {
            EventSource.Log.ErrorEvent($"Failed to load assembly '{assemblyName}'.", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Finds a public static extension method that accepts a builder parameter and has no or defaulted parameters.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="builderType">The type of the builder parameter to match.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <returns>The MethodInfo if found, otherwise null.</returns>
    public virtual MethodInfo? FindParameterlessMethod(
        Type type,
        Type builderType,
        string methodName)
    {
        // Try exact match first
        var exact = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [builderType],
            modifiers: null);

        if (exact != null)
            return exact;

        // Fall back to methods where all params beyond the builder have default values
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == methodName &&
                m.GetParameters() is { Length: >= 1 } p &&
                p[0].ParameterType == builderType &&
                p.Skip(1).All(param => param.HasDefaultValue));
    }

    /// <summary>
    /// Invokes a parameterless public static method on the specified type with a builder argument.
    /// </summary>
    /// <param name="methodInfo">MethodInfo object found from FindParamtereless.</param>
    /// <param name="builder">The builder instance to pass as argument.</param>
    /// <returns>The method's return value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method is not found.</exception>
    public virtual object InvokeParameterless(
     MethodInfo methodInfo,
     object builder)
    {
        var paramsToInvoke = new List<object?> { builder };
        methodInfo.GetParameters()
            .Skip(1)
            .ToList()
            .ForEach(_ => paramsToInvoke.Add(null));

        return methodInfo.Invoke(null, paramsToInvoke.ToArray())!;
    }

    /// <summary>
    /// Invokes a method with an Action&lt;TOptions&gt; parameter, binding configuration to options.
    /// </summary>
    /// <remarks>
    /// Constructs an Action&lt;TOptions&gt; delegate from the configuration section and passes it
    /// along with the builder to the target method.
    /// </remarks>
    /// <param name="actionMethod">The MethodInfo of the method to invoke.</param>
    /// <param name="builder">The builder instance to pass as the first argument.</param>
    /// <param name="section">The configuration section to bind to options.</param>
    /// <returns>The method's return value.</returns>
    public virtual object InvokeWithAction(
        MethodInfo actionMethod,
        object builder,
        IConfiguration section)
    {
        var parameters = actionMethod.GetParameters();
        var optionsType = parameters[1].ParameterType.GetGenericArguments()[0];
        var configureAction = BuildConfigureAction(optionsType, section);

        var args = new object[parameters.Length]; // set so  remaining are set as null
        args[0] = builder;
        args[1] = configureAction;

        return actionMethod.Invoke(null, args)!;
    }

    /// <summary>
    /// Finds a public static method that accepts a builder and Action&lt;TOptions&gt; parameter.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="builderType">The type of the builder parameter to match.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <returns>The MethodInfo if found, otherwise null.</returns>
    public virtual MethodInfo? FindActionOverload(
        Type type,
        Type builderType,
        string methodName)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
        return methods.FirstOrDefault(m =>
                m.Name == methodName &&
                m.GetParameters() is { Length: >= 2 } p &&
                p[0].ParameterType == builderType &&
                p[1].ParameterType.IsGenericType &&
                p[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>));
    }

    /// <summary>
    /// Builds an Action&lt;TOptions&gt; delegate that configures options from a configuration section.
    /// </summary>
    /// <remarks>
    /// Uses expression trees to create a compiled lambda that efficiently copies configuration values
    /// to the options instance properties.
    /// </remarks>
    /// <param name="optionsType">The type of options to configure (must have parameterless constructor).</param>
    /// <param name="section">The configuration section containing values to bind.</param>
    /// <returns>A compiled Action&lt;TOptions&gt; delegate.</returns>
    public virtual object BuildConfigureAction(
        Type optionsType,
        IConfiguration section)
    {
        var options = Activator.CreateInstance(optionsType)!;
        section.Bind(options);

        CreateDefaultInstanceOfComplexObjectProperty(options, section);

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

    public void CreateDefaultInstanceOfComplexObjectProperty(object config, IConfiguration section)
    {
        Type type = config.GetType();

        foreach (IConfigurationSection child in section.GetChildren())
        {
            PropertyInfo? prop = type.GetProperty(child.Key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop == null || !prop.CanWrite) continue;

            Type propType = prop.PropertyType;

            if (IsComplexType(propType) && !string.IsNullOrWhiteSpace(child.Value))
            {
                try
                {
                    Type? instanceType = Type.GetType(child.Value, throwOnError: false, ignoreCase: true);
                    if (instanceType is null)
                    {
                        string fullTypeName = child.Value!.Trim();
                        int lastDot = fullTypeName.LastIndexOf('.');
                        string assemblyName = fullTypeName.Substring(0, lastDot);
                        string typeName = fullTypeName; // GetType needs the full name including namespace

                        var assembly = GetAssembly(assemblyName);
                        instanceType = assembly!.GetType(typeName);
                    }
                    object? nestedInstance = Activator.CreateInstance(instanceType!,
                        BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
                        null,
                        Array.Empty<object>(),
                        null);
                    if (nestedInstance is null)
                        throw new Exception("nestedInstance is null");
                    prop.SetValue(config, nestedInstance);
                }
                catch (Exception ex)
                {
                    EventSource.Log.ErrorEvent($"Failed to create default instance of Configuration Action Property {child.Value} for Configuration '{type.Name}'", ex.Message);
                }
            }
        }
    }

    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive
            && !type.IsEnum
            && type != typeof(string)
            && type != typeof(decimal)
            && type != typeof(DateTime)
            && type != typeof(Guid)
            && !type.IsValueType;
    }
}
