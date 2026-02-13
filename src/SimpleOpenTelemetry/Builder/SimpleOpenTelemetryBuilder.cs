namespace SimpleOpenTelemetry.Builder;

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Configuration;

/// <summary>
/// Fluent builder for configuring SimpleOpenTelemetry
/// </summary>
public class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    private readonly TracerProviderBuilder _tracerProviderBuilder;
    private readonly SimpleOpenTelemetryOptions _options;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder
    /// </summary>
    public SimpleOpenTelemetryBuilder()
    {
        _options = new SimpleOpenTelemetryOptions();
        _tracerProviderBuilder = Sdk.CreateTracerProviderBuilder();
    }

    /// <inheritdoc />
    public TracerProviderBuilder TracerProviderBuilder => _tracerProviderBuilder;

    /// <inheritdoc />
    public ISimpleOpenTelemetryBuilder WithServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        _options.ServiceName = serviceName;
        UpdateResourceBuilder();
        return this;
    }

    /// <inheritdoc />
    public ISimpleOpenTelemetryBuilder WithServiceVersion(string serviceVersion)
    {
        if (string.IsNullOrWhiteSpace(serviceVersion))
            throw new ArgumentException("Service version cannot be null or empty", nameof(serviceVersion));

        _options.ServiceVersion = serviceVersion;
        UpdateResourceBuilder();
        return this;
    }

    /// <inheritdoc />
    public ISimpleOpenTelemetryBuilder ConfigureTracing(Action<TracerProviderBuilder> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        configure(_tracerProviderBuilder);
        return this;
    }

    /// <inheritdoc />
    public TracerProvider Build()
    {
        return _tracerProviderBuilder.Build();
    }

    private void UpdateResourceBuilder()
    {
        var serviceName = _options.ServiceName ?? "unknown-service";
        var serviceVersion = _options.ServiceVersion ?? "1.0.0";
        
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion);

        _tracerProviderBuilder.SetResourceBuilder(resourceBuilder);
    }
}
