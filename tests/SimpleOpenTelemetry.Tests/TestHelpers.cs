using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry.OtelComponents.Common;
using SimpleOpenTelemetry.Reflection;

namespace SimpleOpenTelemetryTests;


internal static class TestHelpers
{
   
    internal static IEnumerable<TextMapPropagator> GetCompositePropagators(CompositeTextMapPropagator composite)
    {
        var type = composite.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (typeof(IEnumerable<TextMapPropagator>).IsAssignableFrom(field.FieldType))
            {
                var value = field.GetValue(composite);
                if (value is IEnumerable<TextMapPropagator> items)
                {
                    return items;
                }
            }
        }

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var property in properties)
        {
            if (typeof(IEnumerable<TextMapPropagator>).IsAssignableFrom(property.PropertyType))
            {
                var value = property.GetValue(composite);
                if (value is IEnumerable<TextMapPropagator> items)
                {
                    return items;
                }
            }
        }

        throw new InvalidOperationException("Unable to inspect CompositeTextMapPropagator internal propagators.");
    }

    
    internal static IConfigurationSection? GetComponentConfigurationSection(
        IAssemblyExecution assemblyExec, 
        AssemblyDescriptor descriptor,
        string? optionsSectionName = null)
    {
        // Just generate a section based on the options class structure, dont set an values
        IConfigurationSection? optionsConfigSection = null;

        var className = descriptor.OptionsClassName;
        var assembly = assemblyExec.GetAssembly(descriptor.AssemblyName);
        var classDef = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == className)!;

        var ctor = classDef.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var instance = ctor!.Invoke(null);

        var sectionName = optionsSectionName ?? classDef.Name;
        var inner = JsonSerializer.Serialize(instance, classDef);
        var wrapped = $"{{\"{sectionName}\": {inner}}}";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(wrapped));
        IConfiguration classOptionsBuilder = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        optionsConfigSection = classOptionsBuilder.GetSection(sectionName);
        
        return optionsConfigSection;
    }
}