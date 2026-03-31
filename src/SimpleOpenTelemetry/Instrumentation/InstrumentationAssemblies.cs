using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Instrumentation;

internal record InstrumentationExtensionDescriptor(
     string AssemblyName,
     string TypeName,
     string MethodName,
     string? ConfigurationSection
);

/// <summary>
/// 
/// </summary>
internal static class InstrumentationAssemblies
{
    public static readonly Dictionary<TraceInstrumentationEnum, InstrumentationExtensionDescriptor>
        KnownTraceInstrumentations = new()
        {
            [TraceInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Trace.AspNetCoreInstrumentationTracerProviderBuilderExtensions",
                "AddAspNetCoreInstrumentation",
                null),
            [TraceInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Trace.HttpClientInstrumentationTracerProviderBuilderExtensions",
                "AddHttpClientInstrumentation",
                null),
            [TraceInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddSqlClientInstrumentation",
                null),
            [TraceInstrumentationEnum.EFCore] = new(
                "OpenTelemetry.Instrumentation.EntityFrameworkCore",
                "OpenTelemetry.Trace.EntityFrameworkInstrumentationTracerProviderBuilderExtensions",
                "AddEntityFrameworkCoreInstrumentation",
                null),

            // gRPC – Grpc.Net.Client (modern)
            [TraceInstrumentationEnum.GrpcNetClient] = new(
                "OpenTelemetry.Instrumentation.GrpcNetClient",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddGrpcClientInstrumentation",
                null),

            // gRPC – legacy Grpc.Core
            [TraceInstrumentationEnum.GrpcCore] = new(
                "OpenTelemetry.Instrumentation.GrpcCore",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddGrpcCoreInstrumentation",
                null),

            // WCF (client-side; server-side is configured via behavior XML/code)
            [TraceInstrumentationEnum.Wcf] = new(
                "OpenTelemetry.Instrumentation.Wcf",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddWcfInstrumentation",
                null),

            // Hangfire
            [TraceInstrumentationEnum.Hangfire] = new(
                "OpenTelemetry.Instrumentation.Hangfire",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddHangfireInstrumentation",
                null),

            // Quartz.NET
            [TraceInstrumentationEnum.Quartz] = new(
                "OpenTelemetry.Instrumentation.Quartz",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddQuartzInstrumentation",
                null),

            // AWS SDK
            [TraceInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddAWSInstrumentation",
                "SimpleOpenTelemetry:TraceInstrumentationConfig:AWS"),
            
            // AWS Lambda
            [TraceInstrumentationEnum.AWSLambda] = new(
                "OpenTelemetry.Instrumentation.AWSLambda",
                "OpenTelemetry.Instrumentation.AWSLambda.TracerProviderBuilderExtensions",
                "AddAWSLambdaConfigurations",
                "SimpleOpenTelemetry:TraceInstrumentationConfig:AWSLambda"),

            // StackExchange.Redis
            // NOTE: AddRedisInstrumentation() has an overload that takes no
            // IConnectionMultiplexer and resolves it from IServiceProvider instead,
            // so it IS usable as a parameterless registration in DI-hosted scenarios.
            [TraceInstrumentationEnum.StackExchangeRedis] = new(
                "OpenTelemetry.Instrumentation.StackExchangeRedis",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddRedisInstrumentation",
                null),
        };


    public static readonly Dictionary<MetricInstrumentationEnum, InstrumentationExtensionDescriptor>
        KnownMetricsInstrumentations = new()
        {
            [MetricInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions",
                "AddAspNetCoreInstrumentation",
                null),
            [MetricInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Metrics.HttpClientInstrumentationMeterProviderBuilderExtensions",
                "AddHttpClientInstrumentation",
                null),
            [MetricInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Metrics.SqlClientMeterProviderBuilderExtensions",
                "AddSqlClientInstrumentation",
                null),
            [MetricInstrumentationEnum.Runtime] = new(
                "OpenTelemetry.Instrumentation.Runtime",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddRuntimeInstrumentation",
                null),

            // Process metrics
            [MetricInstrumentationEnum.Process] = new(
                "OpenTelemetry.Instrumentation.Process",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddProcessInstrumentation",
                null),

            // Hangfire
            [MetricInstrumentationEnum.Hangfire] = new(
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",  // namespace
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddHangfireInstrumentation",
                null),

            // AWS
            [MetricInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddAWSInstrumentation",
                null)
        };
}
