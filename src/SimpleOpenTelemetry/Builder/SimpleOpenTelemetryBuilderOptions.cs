namespace SimpleOpenTelemetry.Builder;

/// <summary>
/// Configuration options for SimpleOpenTelemetry Builder
/// </summary>
public class SimpleOpenTelemetryBuilderOptions
{
    // TODO chad remove as these can be set through standard OTEL env vars or functions on the builder.
    ///// <summary>
    ///// Gets or sets the service name
    ///// </summary>
    //public string? ServiceName { get; set; }

    ///// <summary>
    ///// Gets or sets the service version
    ///// </summary>
    //public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the exporter type (OTLP, AzureMonitor, etc)
    /// </summary>
    public string Exporter { get; set; } = "OTLP";

    /// <summary>
    /// Gets or sets whether tracing is enabled
    /// </summary>
    public bool EnableTracing { get; set; } = false;

    /// <summary>
    /// Gets or sets whether metrics are enabled
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether logging is enabled
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// 
    /// </summary>
    public bool AspNetCoreInstrumentation { get; set; } = false;

    /// <summary>
    /// 
    /// </summary>
    public bool HttpClientInstrumentation  { get; set; } = false;
    
    /// <summary>
    /// 
    /// </summary>
    public bool SqlClientInstrumentation  { get; set; } = false;

    //TODO Chad migrate more in from old demo below

    // Metrics provides by ASP.NET Core in .NET 8
    //metrics.AddMeter("Microsoft.AspNetCore.Hosting");
    //metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
    //metrics.AddHttpClientInstrumentation()
    // TODO check
    //.AddRuntimeInstrumentation()
    //.AddAspNetCoreInstrumentation();

    //    .AddSource("Azure.*");

}