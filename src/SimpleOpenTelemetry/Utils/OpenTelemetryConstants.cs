namespace SimpleOpenTelemetry.Utils;

/// <summary>
/// Because OpenTelemetry lib doesn't expose string literals used for 
/// configuration / env vars. This are created here.
/// </summary>
public static class OpenTelemetryConstants
{
    /// <summary>
    /// OpenTelemetry config Environment vars / root IConfiguration values
    /// </summary>
    public static class EnvironmentVariables
    {
        public const string OTEL_SERVICE_NAME = "OTEL_SERVICE_NAME";
        public const string OTEL_RESOURCE_ATTRIBUTES = "OTEL_RESOURCE_ATTRIBUTES";
    }

    public static class ResourceAttributes
    {
        public const string AttributeServiceName = "service.name";
        public const string AttributeServiceNamespace = "service.namespace";
        public const string AttributeServiceInstance = "service.instance.id";
        public const string AttributeServiceVersion = "service.version";
    }
}
