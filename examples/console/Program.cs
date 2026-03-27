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

// OPTIONAL: clear loggers for the OpenTelemetry loggers to be the only ones
builder.Logging.ClearProviders();

// Get exporter type from configuration
var serviceName = SimpleOpenTelemetry.Utils.SettingsHelper.OtelServiceName(builder.Configuration);

Console.WriteLine($"[Configuration] OpenTelemetry Service Name: {serviceName}");

Console.WriteLine($"[OpenTelemetry] Initialising / Configuring OpenTelemetry with SimpleOpenTelemetry");

builder.Services.AddSimpleOpenTelemetry(builder.Configuration);

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

host.Services.SimpleOpenTelemetryValidate();

Console.WriteLine("\n[Demo] Starting hosted service to run operations");
await host.RunAsync();


Console.WriteLine("\n" + new string('─', 60));
Console.WriteLine("\n[Demo] All operations completed with tracing enabled!");
Console.WriteLine("[Demo] Check your monitoring system for traces.");


// TODO Chad check this demo
// https://github.com/dfederm/GenericHostConsoleApp/blob/main/Program.cs
// https://github.com/dotnet/docs/blob/main/docs/core/extensions/snippets/configuration/app-lifetime/ExampleHostedService.cs

/// <summary>
/// Extension method to configure exporters
/// </summary>
static class ExporterExtensions
{
    // TODO Chad fix up these

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
