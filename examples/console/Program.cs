using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SimpleOpenTelemetry.Examples.Console;
using SimpleOpenTelemetry.Extensions;

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║     SimpleOpenTelemetry Console Application Sample        ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

// Setup .net Generic host
Console.WriteLine($"[Configuration] Initialising .Net Generic Host and loading configurations");
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Get exporter type from configuration
var serviceName = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");

Console.WriteLine($"[Configuration] OpenTelemetry Service Name: {serviceName}");

// Register opentelemetry and add SimpleOpenTelemetry
Console.WriteLine("\n[OpenTelemetry] Initialization of AddOpenTelemetry()");

var otelBuilder = builder.Services.AddOpenTelemetry();

Console.WriteLine("\n[OpenTelemetry] Initialization complete");
Console.WriteLine("\n" + new string('─', 60));
Console.WriteLine($"[OpenTelemetry] Configuring OpenTelemetry with SimpleOpenTelemetry");

builder.Services.SimpleOpenTelemetry(otelBuilder, builder.Configuration);

Console.WriteLine($"[OpenTelemetry] SimpleOpenTelemetry configuration complete");
Console.WriteLine("\n" + new string('─', 60));

// Add hosted service to do trigger some telemetry to be sent
builder.Services.AddHostedService<App>();
// Add console output for the 
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});
var host = builder.Build();

Console.WriteLine("\n[Demo] Starting hosted service to run operations");
await host.RunAsync();


Console.WriteLine("\n" + new string('─', 60));
Console.WriteLine("\n[Demo] All operations completed with tracing enabled!");
Console.WriteLine("[Demo] Check your monitoring system for traces.");



/// <summary>
/// Extension method to configure exporters
/// </summary>
static class ExporterExtensions
{
    // TODO fix up these

    //public static ISimpleOpenTelemetryBuilder ConfigureExporter(
    //    this ISimpleOpenTelemetryBuilder builder,
    //    string exporterType)
    //{
    //    return exporterType?.ToUpper() switch
    //    {
    //        "AZUREMONITOR" => ConfigureAzureMonitor(builder),
    //        "NEWRELIC" => ConfigureNewRelic(builder),
    //        _ => ConfigureOtlp(builder)
    //    };
    //}

    //private static ISimpleOpenTelemetryBuilder ConfigureOtlp(
    //    this ISimpleOpenTelemetryBuilder builder)
    //{
    //    var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    //        ?? "http://localhost:4317";

    //    Console.WriteLine($"[OpenTelemetry] Using OTLP exporter: {endpoint}");
    //    return builder.WithOtlpExporter(endpoint);
    //}

    //private static ISimpleOpenTelemetryBuilder ConfigureAzureMonitor(
    //    this ISimpleOpenTelemetryBuilder builder)
    //{
    //    var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

    //    if (string.IsNullOrEmpty(connectionString))
    //    {
    //        Console.WriteLine("[OpenTelemetry] ⚠ Azure Monitor: APPLICATIONINSIGHTS_CONNECTION_STRING not found");
    //        return ConfigureOtlp(builder);
    //    }

    //    Console.WriteLine("[OpenTelemetry] Using Azure Monitor exporter");
    //    return builder.WithAzureMonitorExporter(connectionString);
    //}

}
