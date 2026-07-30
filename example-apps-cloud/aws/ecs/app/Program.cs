using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using Amazon.Runtime.Credentials;
using Amazon.XRay;
using AwsSignatureVersion4;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Sampler.AWS;
using OpenTelemetry.Trace;
using SimpleOpenTelemetry.Examples.Shared;
using SimpleOpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);


OtelEventListener? otelListener = null;
SimpleOtelEventListener? simpleOtelListener = null;

// FOR DEMO/DEBUG PURPOSES - Add Event listeners outputing to console
if (builder.Configuration.GetValue("EnableOtelEventListeners", false))
{
    otelListener = new OtelEventListener();
    simpleOtelListener = new SimpleOtelEventListener();
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

// OPTIONAL: clear loggers so only the OpenTelemetry logger is attached
// As the ECS task definition in the terraform has 'logConfiguration' removing
// this would result in duplicate logs, console and otlp exporter
//builder.Logging.ClearProviders();

// ### This is the only ESSENTIAL code to add SimpleOpenTelemetry - the next sections are only for
// ### direct exporting to AWS OTLP endpoints or AWS Xray Remote Sampler
var otelBuilder = builder.AddSimpleOpenTelemetry();

// Get env vars to determine if we need direct export or xray sampler enabled - as it can only be done via code
var servicename = builder.Configuration.GetValue("OTEL_SERVICE_NAME", "unknown");
var traceEndpoint = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");

if (!builder.Configuration.GetValue("UseOtelCollector", true))
{
    // ### - Custom Http Auth for direct export - Not recommended for production
    var region = builder.Configuration.GetValue<string>("AWS_REGION")!;
    var creds = new DefaultAWSCredentialsIdentityResolver();
    var env = builder.Configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
    var logGroupName = builder.Configuration.GetValue<string>("AWS_LOG_GROUP_NAME");
    var logStreamName = builder.Configuration.GetValue<string>("AWS_LOG_STREAM_NAME");

    var metricEndpoint = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT");
    var logEndpoint = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT");

    var credentials = new DefaultAWSCredentialsIdentityResolver();
    var credsTraces = credentials.ResolveIdentity(new AmazonXRayConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) });
    var credsMetrics = credentials.ResolveIdentity(new AmazonCloudWatchConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) });
    var credsLogs = credentials.ResolveIdentity(new AmazonCloudWatchLogsConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) });

    otelBuilder.WithTracing(t => t.AddXRayTraceId()
        .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(traceEndpoint!);
                o.HttpClientFactory = () =>
                {
                    var innerHandler = new HttpClientHandler();
                    var sigHandler = new AwsSignatureHandler(new AwsSignatureHandlerSettings(region, "xray", credsTraces))
                    {
                        InnerHandler = innerHandler
                    };
                    return new HttpClient(sigHandler);
                };
            }))
        .WithMetrics(m => m
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(metricEndpoint!);
                o.HttpClientFactory = () =>
                {
                    var innerHandler = new HttpClientHandler();
                    var sigHandler = new AwsSignatureHandler(new AwsSignatureHandlerSettings(region, "monitoring", credsMetrics))
                    {
                        InnerHandler = innerHandler
                    };
                    return new HttpClient(sigHandler);
                };
            }))
        .WithLogging(l => l
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(logEndpoint!);
                o.HttpClientFactory = () =>
                {
                    var innerHandler = new HttpClientHandler();
                    var sigHandler = new AwsSignatureHandler(new AwsSignatureHandlerSettings(region, "logs", credsLogs))
                    {
                        InnerHandler = innerHandler
                    };
                    var client = new HttpClient(sigHandler);
                    // Logs endpoint requires these headers on every request
                    client.DefaultRequestHeaders.Add("x-aws-log-group", logGroupName);
                    client.DefaultRequestHeaders.Add("x-aws-log-stream", logStreamName);
                    return client;
                };
            }));
    // ### - END Custom Http Auth for direct export
}
else if (builder.Configuration.GetValue("UseXraySampler", false))
{
    // ### - X-Ray Sampling - Only possible to enable if using aws-otel-collector as its not possible to add request sig like above
    // For more details see https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Sampler.AWS/README.md
    var resourceAttribs = builder.Configuration.GetValue("OTEL_RESOURCE_ATTRIBUTES", "");

    // Unfortunately the design of this requires a prebuilt resource unlike all other libraries using a lazy loaded Resource / ResourceBuilder
    // so in actuality there will be two resources, this replicates what is configured in the SimpleOpenTelemetry:Trace section
    var resourceBuilder = ResourceBuilder
        .CreateDefault()
        .AddService(servicename, "demo-simpleopentelemetry")
        .AddAttributes([
            new("OTEL_SERVICE_NAME",servicename),
            new("OTEL_RESOURCE_ATTRIBUTES",resourceAttribs)
        ])
        .AddEnvironmentVariableDetector();
    // .AddAWSECSDetector() // Add which ever AWSResourceBuilderExtensions extension for your target env
    // .AddAWSEC2Detector();

    var samplerEndpoint = builder.Configuration.GetValue("AWS_XRAY_SAMPLER_ENDPOINT", "http://localhost:2000");

    otelBuilder.WithTracing(t => t.AddSource("soteltestaws.*")
        .SetSampler(AWSXRayRemoteSampler.Builder(resourceBuilder.Build())
        .SetPollingInterval(TimeSpan.FromSeconds(5))
        .SetEndpoint(samplerEndpoint)
        .Build()));

    // ### - END X-Ray Sampling
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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health");

app.Run();
