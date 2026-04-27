using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
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
    public Assembly GetAssembly(string assemblyName)
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
    public Assembly? TryLoadAssembly(string assemblyName)
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
            return null;
        }

        try
        {
            var loaded = Assembly.LoadFrom(path);
            return loaded;
        }
        catch (Exception ex)
        {
            EventSource.Log.Error($"Failed to load assembly '{assemblyName}'.", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Finds a public static method that accepts a builder parameter.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="builderType">The type of the builder parameter to match.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <returns>The MethodInfo if found, otherwise null.</returns>
    public MethodInfo? FindParameterlessMethod(
    Type type,
    Type builderType,
    string methodName)
    => type.GetMethod(
        methodName,
        BindingFlags.Public | BindingFlags.Static,
        binder: null,
        types: new Type[] { builderType },
        modifiers: null);

    /// <summary>
    /// Finds a public static method that accepts just builder parameter.
    /// or with all defaulting successive overloads
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="builderType">The type of the builder parameter to match.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <returns>The MethodInfo if found, otherwise null.</returns>
    public MethodInfo FindParameterlessMethodWithAllDefaultValues(
        Type type,
        Type builderType,
        string methodName)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
        var matchingMethod = methods.FirstOrDefault(m =>
            m.Name == methodName &&
            m.GetParameters() is { Length: >= 1 } p &&
            p[0].ParameterType == builderType &&
            p.Skip(1).All(param => param.HasDefaultValue))
            ?? throw new InvalidOperationException(
                   $"No parameterless '{methodName}' method accepting {builderType.Name} found on {type.FullName}.");

        return matchingMethod;
    }

    /// <summary>
    /// Invokes an extension method on the specified target object and sets all defaulted parameters.
    /// as null.
    /// </summary>
    /// <param name="method">Method to invoke</param>
    /// <param name="targetType">The type of instance to pass as argument.</param>
    /// <param name="target">The instance to pass as argument.</param>
    public object InvokeParameterlessOrDefaultedParameters(MethodInfo method, Type targetType, object target)
    {
        var paramsToInvoke = new List<object>() { target };
        var remainingParams = method.GetParameters().Skip(1).ToList();

        // If this method cant accept all defaulted remaining parameters throw error
        if (remainingParams.Any(r => !r.HasDefaultValue))
            throw new InvalidOperationException(
                   $"No parameterless '{method.Name}' method accepting {targetType.Name} found on {method.GetType().Name}.");

        remainingParams.ForEach(p => paramsToInvoke.Add(null));

        return method.Invoke(null, paramsToInvoke.ToArray())!;
    }

    /// <summary>
    /// Invokes a parameterless public static method on the specified type with a builder argument.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="builderType">The type of the builder parameter.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="builder">The builder instance to pass as argument.</param>
    /// <returns>The method's return value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method is not found.</exception>
    public object InvokeParameterless(
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
    public object InvokeWithAction(
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
    public MethodInfo? FindActionOverload(
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
    public object BuildConfigureAction(
    Type optionsType,
    IConfiguration section)
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
