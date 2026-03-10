namespace SimpleOpenTelemetry.Builder;

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Configuration;

/// <summary>
/// Fluent builder for configuring SimpleOpenTelemetry
/// </summary>
public class SimpleOpenTelemetryBuilder : ISimpleOpenTelemetryBuilder
{
    internal readonly TracerProviderBuilder _tracerProviderBuilder;
    internal readonly SimpleOpenTelemetryBuilderOptions _options;
    internal readonly OpenTelemetryBuilder _otelBuilder;

    // TODO Chad check if we can have multiple and how to ad
    private IList<OtlpExporterOptions> _exporters;

    /// <summary>
    /// Initializes a new instance of the SimpleOpenTelemetryBuilder
    /// </summary>
    public SimpleOpenTelemetryBuilder(OpenTelemetryBuilder otelBuilder)
    {
        _otelBuilder = otelBuilder;
        _options = new SimpleOpenTelemetryBuilderOptions();
        _exporters = new List<OtlpExporterOptions>();
    }

    /// <summary>
    /// 
    /// </summary>
    public OpenTelemetryBuilder OtelBuilder => _otelBuilder;

    /// <summary>
    /// Builds the OpenTelemetry configuration based on the provided settings
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IOpenTelemetryBuilder AddOpenTelemetry()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds OpenTelemetry Metrics to the configuration
    /// </summary>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithMetrics()
        => this.WithMetrics(b => { });

    /// <summary>
    /// Adds OpenTelemetry Metrics to the configuration with additional configuration options
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithMetrics(Action<MeterProviderBuilder> configure)
    {
        _otelBuilder.WithMetrics(configure);
        return this;
    }

    /// <summary>
    /// Adds OpenTelemetry Tracing to the configuration
    /// </summary>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithTracing()
        => this.WithTracing(b => { });

    /// <summary>
    /// Adds OpenTelemetry Tracing to the configuration with additional configuration options
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithTracing(Action<TracerProviderBuilder> configure)
    {
        _otelBuilder.WithTracing(configure);
        return this;
    }

    /// <summary>
    /// Adds OpenTelemetry Logging to the configuration
     /// </summary>
     /// <returns></returns>
    /// </summary>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithLogging() 
    {
         _otelBuilder.WithLogging(configureBuilder: null, configureOptions: null);
        return this;
    }
    /// <summary>
    /// Adds OpenTelemetry Logging to the configuration with additional configuration options
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithLogging(Action<LoggerProviderBuilder> configure)
    {
        _otelBuilder.WithLogging(configureBuilder: configure, configureOptions: null);
        return this;
    }

    /// <summary>
    /// Adds OpenTelemetry Logging to the configuration with additional configuration options
    /// </summary>
    /// <param name="configureBuilder"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public ISimpleOpenTelemetryBuilder WithLogging(
        Action<LoggerProviderBuilder>? configureBuilder,
        Action<OpenTelemetryLoggerOptions>? configureOptions)
    {
        _otelBuilder.WithLogging(configureBuilder, configureOptions);

        return this;
    }


    // /// <inheritdoc />
    // public ISimpleOpenTelemetryBuilder WithServiceName(string serviceName)
    // {
    //     if (string.IsNullOrWhiteSpace(serviceName))
    //         throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

    //     _options.ServiceName = serviceName;
    //   //  UpdateResourceBuilder();
    //     return this;
    // }

    // /// <inheritdoc />
    // public ISimpleOpenTelemetryBuilder WithServiceVersion(string serviceVersion)
    // {
    //     if (string.IsNullOrWhiteSpace(serviceVersion))
    //         throw new ArgumentException("Service version cannot be null or empty", nameof(serviceVersion));

    //     _options.ServiceVersion = serviceVersion;
    //     //UpdateResourceBuilder();
    //     return this;
    // }


    // // TODO Chad check if needed
    // private void UpdateResourceBuilder()
    // {
    //     var serviceName = _options.ServiceName ?? "unknown-service";
    //     var serviceVersion = _options.ServiceVersion ?? "1.0.0";
        
    //     // TOOD chad check if needed
    //     var resourceBuilder = ResourceBuilder.CreateDefault()
    //         .AddService(
    //             serviceName: serviceName,
    //             serviceVersion: serviceVersion);

    //     _tracerProviderBuilder.SetResourceBuilder(resourceBuilder);
    // }
}
