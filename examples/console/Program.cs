using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using SimpleOpenTelemetry.Examples.Console;
using SimpleOpenTelemetry.Examples.Shared;
using SimpleOpenTelemetry.Extensions;


var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

if (config.GetValue<string>("UseGenericHost").ToLower() == "true")
{

    Console.WriteLine("╔═════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SimpleOpenTelemetry Console Application Generic Host Sample     ║");
    Console.WriteLine("╚═════════════════════════════════════════════════════════════════════╝");

    // Add Event listeners outputing to console for demo/debug purposes
    using var otelListener = new OtelEventListener();
    using var simpleOtelListener = new SimpleOtelEventListener();

    // Setup .net Generic host
    Console.WriteLine($"[Configuration] Initialising .Net Generic Host and loading configurations");
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    // OPTIONAL: clear loggers so the OpenTelemetry logger is attached
    builder.Logging.ClearProviders();

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

    var app = builder.Build();

    app.Services.SimpleOpenTelemetryValidate();

    var monitor = app.Services.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>();
    var primaryOptions = monitor.Get("OTLPExporter-trace-1");

    Console.WriteLine("\n[Demo] Starting hosted service to run operations");

    await app.RunAsync();


    Console.WriteLine("\n" + new string('─', 60));
    Console.WriteLine("\n[Demo] All operations completed with tracing enabled!");
    Console.WriteLine("[Demo] Check your monitoring system for traces.");

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}
else
{
    Console.WriteLine("╔═════════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SimpleOpenTelemetry Console Application Non-Generic Host Sample     ║");
    Console.WriteLine("╚═════════════════════════════════════════════════════════════════════════╝");

    
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}
// TODO Chad check this demo
// https://github.com/dfederm/GenericHostConsoleApp/blob/main/Program.cs
// https://github.com/dotnet/docs/blob/main/docs/core/extensions/snippets/configuration/app-lifetime/ExampleHostedService.cs

