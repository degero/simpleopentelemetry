
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace SimpleOpenTelemetry.Reflection;

internal interface IAssemblyExecution
{
    object BuildConfigureAction(Type optionsType, IConfiguration section);
    MethodInfo? FindActionOverload(Type type, Type builderType, string methodName);
    MethodInfo? FindParameterlessMethod(Type type, Type builderType, string methodName);
    Assembly GetAssembly(string assemblyName);
    object InvokeParameterless(MethodInfo methodInfo, object builder);
    object InvokeWithAction(MethodInfo actionMethod, object builder, IConfiguration section);
    Assembly? TryLoadAssembly(string assemblyName);
}
