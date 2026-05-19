using Microsoft.Extensions.Configuration;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;
using EventSource = SimpleOpenTelemetry.Diagnostics.SimpleOpenTelemetryEventSource;

internal abstract class LoaderBase
{
    protected abstract string ComponentKind { get; }

    protected readonly IAssemblyExecution _assemblyExec;

    protected LoaderBase(IAssemblyExecution assemblyExec)
    {
        _assemblyExec = assemblyExec;
    }
    
    protected bool TryInvokeComponents<TEnum, TBuilder>(
        string?[] componentNames,
        TBuilder builder,
        Dictionary<TEnum, AssemblyDescriptor> descriptors,
        SimpleOpenTelemetryOptions? options = null,
        Func<AssemblyDescriptor, SimpleOpenTelemetryOptions, string, IConfiguration?>? getConfiguration = null) 
        where TEnum : struct, Enum
    {
        var result = true;
        if (componentNames is not null)
        {
            foreach(var componentName in componentNames)
            {
                if(!TryInvokeComponent(componentName, builder, descriptors, options, getConfiguration))
                    result = false;
            }
        }
        return result;
    }

    protected bool TryInvokeComponent<TEnum, TBuilder>(
        string? componentName,
        TBuilder builder,
        Dictionary<TEnum, AssemblyDescriptor> descriptors,
        SimpleOpenTelemetryOptions? options = null,
        Func<AssemblyDescriptor, SimpleOpenTelemetryOptions, string, IConfiguration?>? getConfiguration = null) 
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(componentName))
        {
            if (TryGetDescriptor<TEnum, TBuilder>(componentName, descriptors, out var descriptor, out var matchedEnum))
            {
                IConfiguration? config = getConfiguration is not null ? getConfiguration(descriptor!, options!, matchedEnum.ToString()) : null;
                return TryInvokeDescriptor(componentName, builder, descriptor!, config);
            }
        }
        return false;
    }

    protected bool TryGetDescriptor<TEnum, TBuilder>(string componentName,
        Dictionary<TEnum, AssemblyDescriptor> descriptors,
        out AssemblyDescriptor? descriptor,
        out TEnum? matchedEnum)
        where TEnum : struct, Enum
    {
        var builderName = typeof(TBuilder).Name;
        if (TryParseKnown<TEnum>(componentName!, out var matchedComponent))
        {
            matchedEnum = matchedComponent;
            if (!descriptors.TryGetValue(matchedComponent, out descriptor))
            {
                EventSource.Log.Error(ComponentKind,
                    $"OpenTelemetry {ComponentKind} {typeof(TEnum).Name} type '{matchedComponent}' for builder '{builderName}' not found to initialise. Please check your SimpleOpenTelemetry configuration.");
                return false;                
            }
            return true;
        }
        else
        {
            matchedEnum = null;
            EventSource.Log.Error(ComponentKind, $"Unsupported OpenTelemetry {ComponentKind} '{componentName}' for builder '{builderName}'. Please check your SimpleOpenTelemetry configuration.");
            descriptor = null;
            return false;                
        }
    }

    protected bool TryInvokeDescriptor<TBuilder>(
        string componentName,
        TBuilder builder,
        AssemblyDescriptor descriptor,
        IConfiguration? optionsSection)
    {
        var builderName = typeof(TBuilder).Name;
        
        try
        {
            InvokeBuilderExtension(
                _assemblyExec,
                builder,
                descriptor,
                optionsSection,
                ComponentKind);

            EventSource.Log.Verbose(ComponentKind,
                $"Registered OpenTelemetry {ComponentKind} '{componentName}' for builder '{builderName}'.");
            return true;
        }
        catch (Exception ex)
        {
            EventSource.Log.Error(ComponentKind,
                $"Failed to register OpenTelemetry {ComponentKind} '{componentName}' for builder '{builderName}'.",
                ex.Message);
            return false;
        }
    }

    private void InvokeBuilderExtension<TBuilder>(
        IAssemblyExecution assemblyExecution,
        TBuilder builder,
        AssemblyDescriptor descriptor,
        IConfiguration? optionsSection,
        string componentKind)
    {
        var (assemblyName, typeName, methodNames, optionsClassName, optionsRequired ) = descriptor;

        var assembly = assemblyExecution.GetAssembly(assemblyName);
        var builderType = typeof(TBuilder);
        var builderTypeName = builder?.GetType().Name ?? builderType.Name;

        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in {assembly.GetName().Name}");

        var hasOptions = optionsSection is IConfigurationSection sectionCheck
            ? sectionCheck.Exists()
            : optionsSection is not null;

        methodNames?.ToList().ForEach(methodName =>
        {
            // If options are required an action method must be used.
            var parameterlessMethod = optionsRequired ? null : assemblyExecution.FindParameterlessMethod(type, builderType, methodName);
            var actionMethod = assemblyExecution.FindActionOverload(type, builderType, methodName);

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

            assemblyExecution.InvokeParameterless(parameterlessMethod!, builder!);
        });
    }

    protected bool TryParseKnown<TEnum>(string? raw, out TEnum value)
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