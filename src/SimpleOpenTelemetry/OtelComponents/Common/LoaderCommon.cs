using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry.Reflection;

namespace SimpleOpenTelemetry.OtelComponents.Common;

internal static class LoaderEnumHelper
{
    public static bool TryParseKnown<TEnum>(string? raw, out TEnum value)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw, ignoreCase: true, out value))
        {
            return Enum.IsDefined(value);
        }

        value = default;
        return false;
    }
}

internal static class ReflectiveLoaderExecutor
{
    public static void InvokeBuilderExtension<TBuilder>(
        IAssemblyExecution assemblyExecution,
        TBuilder builder,
        string assemblyName,
        string typeName,
        string methodName,
        IConfiguration? optionsSection,
        string? optionsClassName,
        string componentKind)
    {
        var assembly = assemblyExecution.GetAssembly(assemblyName);
        var builderType = typeof(TBuilder);
        var builderTypeName = builder?.GetType().Name ?? builderType.Name;

        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

        var parameterlessMethod = assemblyExecution.FindParameterlessMethod(type, builderType, methodName);
        var actionMethod = assemblyExecution.FindActionOverload(type, builderType, methodName);

        var hasOptions = optionsSection is IConfigurationSection sectionCheck
            ? sectionCheck.Exists()
            : optionsSection is not null;

        if (hasOptions)
        {
            if (actionMethod is null)
            {
                throw new InvalidOperationException(
                    $"No Action<TOptions> overload found for '{methodName}' on '{typeName}'.");
            }

            assemblyExecution.InvokeWithAction(actionMethod, builder!, optionsSection!);
            return;
        }

        if (!string.IsNullOrWhiteSpace(optionsClassName) &&
            actionMethod is not null &&
            parameterlessMethod is null)
        {
            throw new InvalidOperationException(
                $"Failed registration {builderTypeName} {componentKind}: '{methodName}'. " +
                $"A configuration section '{optionsClassName}' is required but not found in config file.");
        }

        assemblyExecution.InvokeParameterless(type, builderType, methodName, builder!);
    }
}
