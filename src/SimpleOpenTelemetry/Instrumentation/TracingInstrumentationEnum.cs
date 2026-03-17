
namespace SimpleOpenTelemetry.Instrumentation;

// TODO Chad check these as some may be missing
public enum TracingInstrumentationEnum
{
    AspNetCore,
    HttpClient,
    SqlClient,
    EFCore,
    GrpcNetClient,
    GrpcCore,
    Wcf,
    Hangfire,
    Quartz,
    StackExchangeRedis,
    AWS,
    AWSXRayTraceId,
    AWSLambda,
}
