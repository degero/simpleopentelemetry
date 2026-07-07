using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using SimpleOpenTelemetry.Extensions;
using soteltestgcp;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using OpenTelemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Google.Apis.Auth.OAuth2;
using OpenTelemetry.Resources;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

OtelEventListener? otelListener = null;
SimpleOtelEventListener? simpleOtelListener = null;

// FOR DEMO/DEBUG PURPOSES - Add Event listeners outputing to console 
if (builder.Configuration.GetValue("EnableOtelEventListeners",false))
{
    otelListener = new OtelEventListener();
    simpleOtelListener = new SimpleOtelEventListener();
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

var otelBuilder = builder.AddSimpleOpenTelemetry();

// GCP Direct export requires bearer token authentication
var otlpEndpoint = builder.Configuration.GetValue("OTEL_EXPORTER_OTLP_ENDPOINT","")?.TrimEnd('/') ?? "https://telemetry.googleapis.com";
if (otlpEndpoint.Contains("https://telemetry.googleapis.com", StringComparison.InvariantCultureIgnoreCase))
{
    var bearerTokenProvider = new GoogleCloudBearerTokenProvider();
    var protocolString = builder.Configuration.GetValue("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
    var protocol = protocolString.Equals("grpc", StringComparison.InvariantCultureIgnoreCase) ?
        OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;

    var logName = builder.Configuration.GetValue("GOOGLE_CLOUD_LOG_NAME", "otlp");

    // Configure all three signals with custom HttpClient factory that injects bearer token
    otelBuilder.WithTracing(tracing => tracing.AddOtlpExporter(options =>
        {
            // IMPORTANT direct SDK export only works correctly with Oauth Bearer token in http/protobuf
            options.Protocol = protocol;
            options.Endpoint = new Uri(otlpEndpoint + "/v1/traces");
            options.HttpClientFactory = bearerTokenProvider.CreateHttpClientFactory();
        }) // An example to drop traces on healthcheck, this can also be done in the aspnetcore instrumentation code registration
            .SetSampler(new HealthCheckFilteringSampler(new ParentBasedSampler(new AlwaysOnSampler())))
        )
        .WithMetrics(metrics => metrics.AddOtlpExporter(options =>
       {
            options.Protocol = protocol;
            options.Endpoint = new Uri(otlpEndpoint + "/v1/metrics");
            options.HttpClientFactory = bearerTokenProvider.CreateHttpClientFactory();
        }))
        .WithLogging(logging => logging.AddOtlpExporter(options => {
            options.Protocol = protocol;
            options.Endpoint = new Uri(otlpEndpoint + "/v1/logs");
            options.HttpClientFactory = bearerTokenProvider.CreateHttpClientFactory();
        }).AddProcessor(new GcpLogNameProcessor(logName)) // Allow setting log name and fix severity as the collector-cloudrun-otlpexport.yaml does
          .AddProcessor(new GcpLogSeverityTextProcessor())
        );

    // Cloud Run exposes its own instance id via env/metadata even though
    // the .NET GCP detector 1.0.0-alpha.1 fails to surface it as faas.instance
    string? instanceId = Environment.GetEnvironmentVariable("K_REVISION") is not null
            ? GcpResourceDetector.DetectInstanceId() // GET http://metadata.google.internal/computeMetadata/v1/instance/id
            : null;

    if (instanceId != null)
    {
        otelBuilder.ConfigureResource(r => r.AddAttributes(new Dictionary<string, object>
            {
                ["faas.instance"] = instanceId,
                // Set the 'norm' for otel data instance id. this is done by the example collector transforms 
                // in otel-collector-config
                ["service.instance.id"] = instanceId 
            })
        );
    }
}


// var ratio = builder.Configuration.GetValue<double>("TracingSampleRatio", 1.0);

// otelBuilder.WithTracing(t => t.SetSampler(new ParentBasedSampler(
//     rootSampler: new TraceIdRatioBasedSampler(ratio),
//     remoteParentSampled: new AlwaysOnSampler(),
//     remoteParentNotSampled: new TraceIdRatioBasedSampler(ratio)
// )));

// If you wish to use the default 'parent based' sampling, enabling this
// will stop the issue introduced by Cloud Run injecting a parent span / making sampling decisions
// which affect trace settings / sampling settings in app configuration.
// IMPORTANT: this is just for demonstration purposes, it only solves a simple app trace and doesn't solve
// cases like app to api or client side app to api parent based tracing. Using a different sampler with a 
// ratio and potentially tail sampling  is a better PRODUCTIOn way to handle all cases.
// Allowing configuration or turning off this CloudRun 'feature' is the proper way to resolve. 
// see: https://issuetracker.google.com/issues/363032992
// https://cloud.google.com/run/docs/trace
// https://discuss.google.dev/t/google-cloud-trace-is-missing-all-spans-from-cloud-load-balancer/147087
if (builder.Configuration.GetValue("IgnoreInboundTraceRules",false))
{
    builder.Services.Replace( // There is also another demonstration RenamedHeaderPropagator you can try here
        ServiceDescriptor.Singleton<DistributedContextPropagator, IgnoreInboundContextPropagator>());

    Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new List<TextMapPropagator>()));
}

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() =>
{
    // cleanup eventlisteners
    if (otelListener is not null)
        otelListener.Dispose();
    if (simpleOtelListener is not null)
        simpleOtelListener.Dispose();
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var aspNetCoreUrls = builder.Configuration["ASPNETCORE_URLS"];
if (!string.IsNullOrWhiteSpace(aspNetCoreUrls) && aspNetCoreUrls.Contains("https://", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health");

app.Run();
