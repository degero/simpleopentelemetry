using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.Instrumenttaion
{
    public record InstrumentationExtensionDescriptor(
         string AssemblyName,
         string TypeName,
         string MethodName,
         string? ConfigurationSection = null  // null = truly parameterless
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
                    "AddAspNetCoreInstrumentation"),
                [TracingInstrumentationEnum.HttpClient] = new(
                    "OpenTelemetry.Instrumentation.Http",
                    "OpenTelemetry.Trace.HttpClientInstrumentationTracerProviderBuilderExtensions",
                    "AddHttpClientInstrumentation"),
                [TracingInstrumentationEnum.SqlClient] = new(
                    "OpenTelemetry.Instrumentation.SqlClient",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddSqlClientInstrumentation"),
                [TracingInstrumentationEnum.EFCore] = new(
                    "OpenTelemetry.Instrumentation.EntityFrameworkCore",
                    "OpenTelemetry.Trace.EntityFrameworkInstrumentationTracerProviderBuilderExtensions",
                    "AddEntityFrameworkCoreInstrumentation"),

                // gRPC – Grpc.Net.Client (modern)
                [TracingInstrumentationEnum.GrpcNetClient] = new(
                    "OpenTelemetry.Instrumentation.GrpcNetClient",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddGrpcClientInstrumentation"),

                // gRPC – legacy Grpc.Core
                [TracingInstrumentationEnum.GrpcCore] = new(
                    "OpenTelemetry.Instrumentation.GrpcCore",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddGrpcCoreInstrumentation"),

                // WCF (client-side; server-side is configured via behavior XML/code)
                [TracingInstrumentationEnum.Wcf] = new(
                    "OpenTelemetry.Instrumentation.Wcf",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddWcfInstrumentation"),

                // Hangfire
                [TracingInstrumentationEnum.Hangfire] = new(
                    "OpenTelemetry.Instrumentation.Hangfire",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddHangfireInstrumentation"),

                // Quartz.NET
                [TracingInstrumentationEnum.Quartz] = new(
                    "OpenTelemetry.Instrumentation.Quartz",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddQuartzInstrumentation"),

                // AWS SDK
                [TracingInstrumentationEnum.AWS] = new(
                    "OpenTelemetry.Instrumentation.AWS",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddAWSInstrumentation"),

                // AWS Lambda
                [TracingInstrumentationEnum.AWSLambda] = new(
                    "OpenTelemetry.Instrumentation.AWSLambda",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddAWSLambdaConfigurations",
                    "SimpleOpenTelemetry:TraceInstrumentationConfig:AWSLambda"),

                // StackExchange.Redis
                // NOTE: AddRedisInstrumentation() has an overload that takes no
                // IConnectionMultiplexer and resolves it from IServiceProvider instead,
                // so it IS usable as a parameterless registration in DI-hosted scenarios.
                [TracingInstrumentationEnum.StackExchangeRedis] = new(
                    "OpenTelemetry.Instrumentation.StackExchangeRedis",
                    "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                    "AddRedisInstrumentation"),
            };


        public static readonly Dictionary<MetricsInstrumentationEnum, InstrumentationExtensionDescriptor>
            KnownMetricsInstrumentations = new()
            {
                [MetricsInstrumentationEnum.AspNetCore] = new(
                    "OpenTelemetry.Instrumentation.AspNetCore",
                    "OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions",
                    "AddAspNetCoreInstrumentation"),
                [MetricsInstrumentationEnum.HttpClient] = new(
                    "OpenTelemetry.Instrumentation.Http",
                    "OpenTelemetry.Metrics.HttpClientInstrumentationMeterProviderBuilderExtensions",
                    "AddHttpClientInstrumentation"),
                [MetricsInstrumentationEnum.SqlClient] = new(
                    "OpenTelemetry.Instrumentation.SqlClient",
                    "OpenTelemetry.Metrics.SqlClientMeterProviderBuilderExtensions",
                    "AddSqlClientInstrumentation"),
                [MetricsInstrumentationEnum.Runtime] = new(
                    "OpenTelemetry.Instrumentation.Runtime",
                    "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                    "AddRuntimeInstrumentation"),

                // Process metrics
                [MetricsInstrumentationEnum.Process] = new(
                    "OpenTelemetry.Instrumentation.Process",
                    "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                    "AddProcessInstrumentation"),

                // Hangfire
                [MetricsInstrumentationEnum.Hangfire] = new(
                    "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",  // namespace
                    "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                    "AddHangfireInstrumentation"),
            };
    }
}
