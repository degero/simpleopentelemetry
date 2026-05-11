namespace SimpleOpenTelemetry.Utils;

/// <summary>
/// Because OpenTelemetry lib doesn't expose string literals used for 
/// configuration / env vars. This are created here for reference for any end-user
/// code work.
/// </summary>
public static class OpenTelemetryConstants
{
    /// <summary>
    /// OpenTelemetry config Environment vars / root IConfiguration values
    /// </summary>
    public static class EnvironmentVariables
    {
        /// <summary>
        /// OpenTelemetry service name environment variable.
        /// </summary>
        public const string OTEL_SERVICE_NAME = "OTEL_SERVICE_NAME";
        /// <summary>
        /// OpenTelemetry resource attributes environment variable.
        /// </summary>
        public const string OTEL_RESOURCE_ATTRIBUTES = "OTEL_RESOURCE_ATTRIBUTES";
    }

    /// <summary>
    /// Resource attribute keys.
    /// </summary>
    public static class ResourceAttributes
    {
        /// <summary>
        /// Service name resource attribute.
        /// </summary>
        public const string AttributeServiceName = "service.name";
        /// <summary>
        /// Service namespace resource attribute.
        /// </summary>
        public const string AttributeServiceNamespace = "service.namespace";
        /// <summary>
        /// Service instance ID resource attribute.
        /// </summary>
        public const string AttributeServiceInstance = "service.instance.id";
        /// <summary>
        /// Service version resource attribute.
        /// </summary>
        public const string AttributeServiceVersion = "service.version";
    }
}
