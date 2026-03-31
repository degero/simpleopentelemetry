
namespace SimpleOpenTelemetry.Instrumentation;

// TODO Chad check these as some may be missing
public enum TraceInstrumentationEnum
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
    AWSLambda
}
