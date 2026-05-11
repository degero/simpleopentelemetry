using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry;
using SimpleOpenTelemetry;
using SimpleOpenTelemetry.Examples.Console;
using SimpleOpenTelemetry.Examples.Shared;
using SimpleOpenTelemetry.Extensions;


var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// Add Event listeners outputing to console for demo/debug purposes
using var otelListener = new OtelEventListener();
using var simpleOtelListener = new SimpleOtelEventListener();

if ((config.GetValue<string>("UseGenericHost") ?? "").ToLower() == "true")
{

    Console.WriteLine("╔═════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SimpleOpenTelemetry Console Application Generic Host Sample     ║");
    Console.WriteLine("╚═════════════════════════════════════════════════════════════════════╝");

    

    // Setup .net Generic host
    Console.WriteLine($"[Configuration] Initialising .Net Generic Host and loading appsettings json configurations");
    HostApplicationBuilder builder = Host.CreateApplicationBuilder();

    // OPTIONAL: clear loggers so the OpenTelemetry logger is attached
    builder.Logging.ClearProviders();

    Console.WriteLine($"[OpenTelemetry] Initialising / Configuring OpenTelemetry with SimpleOpenTelemetry");

    var sw = Stopwatch.StartNew();

    // The entry point for SimpleOpenTelemetry to setup your OpenTelemetry
    var otelBuilder = builder.AddSimpleOpenTelemetry();

    sw.Stop();
    Console.WriteLine($"AddSimpleOpenTelemetry() took: {sw.ElapsedMilliseconds}ms");

    Console.WriteLine($"[OpenTelemetry] SimpleOpenTelemetry configuration complete");
    Console.WriteLine("\n" + new string('─', 60));

    // Add hosted service to do trigger some telemetry to be sent
    builder.Services.AddSingleton<ITestHttpCalls, TestHttpCalls>();
    builder.Services.AddHostedService<App>();

    // Additional console output (SimpleOpenTelemetry adds a logger)
    builder.Logging.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.ColorBehavior = LoggerColorBehavior.Enabled;
    });

    var app = builder.Build();

    // OPTIONAL: Validate OpenTelemetry using SimpleOpentelemetry extension method
    app.Services.SimpleOpenTelemetryValidate();

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
    Console.WriteLine("║     SimpleOpenTelemetry Console Application Standalone app Sample       ║");
    Console.WriteLine("╚═════════════════════════════════════════════════════════════════════════╝");

    // Build logger factory with settings from appsettins.Development.json
    // using var loggerFactory = LoggerFactory.Create(builder => builder.AddConfiguration(config.GetSection("Logging")));

    // Create a logger factory with OpenTelemetry as it is not auto added as
    // when using Generic Host Opentelemetry registration extensions 
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConfiguration(config.GetSection("Logging"));
        builder.AddOpenTelemetry();
    });

    var sw = Stopwatch.StartNew();

    var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);
    
    Console.WriteLine($"AddSimpleOpenTelemetry() took: {sw.ElapsedMilliseconds}ms");

    // Create the typed logger from the loggerfactory created by OpenTelemetry
    var sdkLoggerFactory = sdk!.GetLoggerFactory();
    ILogger<TestHttpCalls> testCallsLogger = sdkLoggerFactory?.CreateLogger<TestHttpCalls>() ?? throw new InvalidOperationException("Logger factory is null.");
   
    // 1. DEMO calls to view in Grafana Loki and Tempo queries and Jaeger
    testCallsLogger.LogInformation("Test log message from Generic Host Console App");
    testCallsLogger.LogTrace("Test trace message Generic Host Console App");
    testCallsLogger.LogDebug("Test debug message Generic Host Console App");
    testCallsLogger.LogWarning("Test warning message Generic Host Console App");
    testCallsLogger.LogError("Test error message Generic Host Console App");
    testCallsLogger.LogCritical("Test critical message Generic Host Console App");

    // Create test class and run
    TestHttpCalls testHttpCalls = new(testCallsLogger);

    // 2. SPAN → goes to Tempo from httpclient instrumentation and custom traces
    await testHttpCalls.DemonstrateHttpCalls();

    // 3. Its required to run this to finish exporting and logs before terminating 
    //    as it is not takenc care of like a Generic host app
    sdk.Dispose();

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}