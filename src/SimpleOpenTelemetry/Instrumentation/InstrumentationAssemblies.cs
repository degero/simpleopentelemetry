using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Instrumenttaion;

public record InstrumentationExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? ConfigurationSection
);

/// <summary>
/// 
/// </summary>
public static class InstrumentationAssemblies
{
    public static readonly Dictionary<TracingInstrumentationEnum, InstrumentationExtensionDescriptor>
        KnownTraceInstrumentations = new()
        {
            [TracingInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Trace.AspNetCoreInstrumentationTracerProviderBuilderExtensions",
                "AddAspNetCoreInstrumentation",
                null),
            [TracingInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Trace.HttpClientInstrumentationTracerProviderBuilderExtensions",
                "AddHttpClientInstrumentation",
                null),
            [TracingInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddSqlClientInstrumentation",
                null),
            [TracingInstrumentationEnum.EFCore] = new(
                "OpenTelemetry.Instrumentation.EntityFrameworkCore",
                "OpenTelemetry.Trace.EntityFrameworkInstrumentationTracerProviderBuilderExtensions",
                "AddEntityFrameworkCoreInstrumentation",
                null),

            // gRPC – Grpc.Net.Client (modern)
            [TracingInstrumentationEnum.GrpcNetClient] = new(
                "OpenTelemetry.Instrumentation.GrpcNetClient",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddGrpcClientInstrumentation",
                null),

            // gRPC – legacy Grpc.Core
            [TracingInstrumentationEnum.GrpcCore] = new(
                "OpenTelemetry.Instrumentation.GrpcCore",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddGrpcCoreInstrumentation",
                null),

            // WCF (client-side; server-side is configured via behavior XML/code)
            [TracingInstrumentationEnum.Wcf] = new(
                "OpenTelemetry.Instrumentation.Wcf",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddWcfInstrumentation",
                null),

            // Hangfire
            [TracingInstrumentationEnum.Hangfire] = new(
                "OpenTelemetry.Instrumentation.Hangfire",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddHangfireInstrumentation",
                null),

            // Quartz.NET
            [TracingInstrumentationEnum.Quartz] = new(
                "OpenTelemetry.Instrumentation.Quartz",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddQuartzInstrumentation",
                null),

            // AWS SDK
            [TracingInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddAWSInstrumentation",
                "SimpleOpenTelemetry:TraceInstrumentationConfig:AWS"),
            
            // AWS XRay TraceId
            [TracingInstrumentationEnum.AWSXRayTraceId] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddAWSInstrumentation",
                null),

            // AWS Lambda
            [TracingInstrumentationEnum.AWSLambda] = new(
                "OpenTelemetry.Instrumentation.AWSLambda",
                "OpenTelemetry.Instrumentation.AWSLambda.TracerProviderBuilderExtensions",
                "AddAWSLambdaConfigurations",
                "SimpleOpenTelemetry:TraceInstrumentationConfig:AWSLambda"),

            // StackExchange.Redis
            // NOTE: AddRedisInstrumentation() has an overload that takes no
            // IConnectionMultiplexer and resolves it from IServiceProvider instead,
            // so it IS usable as a parameterless registration in DI-hosted scenarios.
            [TracingInstrumentationEnum.StackExchangeRedis] = new(
                "OpenTelemetry.Instrumentation.StackExchangeRedis",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddRedisInstrumentation",
                null),
        };


    public static readonly Dictionary<MetricsInstrumentationEnum, InstrumentationExtensionDescriptor>
        KnownMetricsInstrumentations = new()
        {
            [MetricsInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions",
                "AddAspNetCoreInstrumentation",
                null),
            [MetricsInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Metrics.HttpClientInstrumentationMeterProviderBuilderExtensions",
                "AddHttpClientInstrumentation",
                null),
            [MetricsInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Metrics.SqlClientMeterProviderBuilderExtensions",
                "AddSqlClientInstrumentation",
                null),
            [MetricsInstrumentationEnum.Runtime] = new(
                "OpenTelemetry.Instrumentation.Runtime",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddRuntimeInstrumentation",
                null),

            // Process metrics
            [MetricsInstrumentationEnum.Process] = new(
                "OpenTelemetry.Instrumentation.Process",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddProcessInstrumentation",
                null),

            // Hangfire
            [MetricsInstrumentationEnum.Hangfire] = new(
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",  // namespace
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddHangfireInstrumentation",
                null),

            // AWS
            [MetricsInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddAWSInstrumentation",
                null)
        };
}
