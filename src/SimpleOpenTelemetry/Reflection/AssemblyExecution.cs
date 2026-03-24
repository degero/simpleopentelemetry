using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class AssemblyExecution
{
    private Dictionary<string, Assembly> _loadedAssemblies = new Dictionary<string, Assembly>();

    public Assembly GetAssembly(string assemblyName, ILogger logger)
    {
        if (_loadedAssemblies.Keys.Contains(assemblyName))
        {
            return _loadedAssemblies[assemblyName];
        }
        else
        {
            var assembly = TryLoadAssembly(assemblyName, logger);
            if (assembly == null)
                throw new Exception($"Critical SimpleOpenTelemetry error: Cannot load otel instrumentation assembly {assemblyName}. " +
                    $"Ensure you have added the required nuget package to your project.");
            return assembly;
        }
    }

    public Assembly? TryLoadAssembly(string assemblyName, ILogger? logger)
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
