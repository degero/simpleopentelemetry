
/// ... Program.cs code...
var factory = new AzureMonitorHttpClientFactory();

var AzureMonitorHttpClientFactory = factory.HttpClientFactory;

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("<load from config");
                otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                otlp.HttpClientFactory = factory.HttpClientFactory;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddView(instrument =>
            {
                return instrument.GetType().GetGenericTypeDefinition()
                    == typeof(Histogram<>)
                    ? new Base2ExponentialBucketHistogramConfiguration()
                    : null;
            })
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri("<load from config");
                otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                otlp.HttpClientFactory = factory.HttpClientFactory;
            });
    })
    .WithLogging(logging =>
    {
        logging.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri("<load from config");
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
            otlp.HttpClientFactory = factory.HttpClientFactory;
        });
    });

/// ... Program.cs code...
