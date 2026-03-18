using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace SimpleOpenTelemetry.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// If you wish to critically fail the app from starting up 
    /// when key ResourceAttributes are not set for the app / hosted env
    /// </summary>
    /// <param name="servicves"></param>
    /// <exception cref="InvalidOperationException"></exception>
	public static void SimpleOpenTelemetryValidate(this IServiceProvider services)
	{
        var tracerProvider = services.GetRequiredService<TracerProvider>();
        var resource = tracerProvider.GetResource();

        var attrs = resource.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value);

        // TODO Chad change to best practice perhaps on prod build? - service.name, service.namespace, service.version, service.instance.id, host.name, host.type, os.name, and os.version
        var requiredKeys = new[] { "service.name", "service.version", "deployment.environment.name" }; //"service.instance.id","host.name", "host.type", "os.type", "os.version"
        var missing = requiredKeys.Where(k => !attrs.ContainsKey(k) || string.IsNullOrEmpty(attrs[k]?.ToString())).ToList();

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Missing required OpenTelemetry resource attributes: {string.Join(", ", missing)}. " +
                "Set OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES env vars.");
        }

    }
}
