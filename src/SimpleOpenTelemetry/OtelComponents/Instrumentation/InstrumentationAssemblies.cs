using SimpleOpenTelemetry.Instrumentation;

namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

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
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddEntityFrameworkCoreInstrumentation",
                null),


            // WCF (client-side; server-side is configured via behavior XML/code)
            [TraceInstrumentationEnum.Wcf] = new(
                "OpenTelemetry.Instrumentation.Wcf",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddWcfInstrumentation",
                null),

            // AWS SDK
            [TraceInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                "AddAWSInstrumentation",
                "SimpleOpenTelemetry:Trace:InstrumentationConfig:AWS"),
            
            // AWS Lambda
            [TraceInstrumentationEnum.AWSLambda] = new(
                "OpenTelemetry.Instrumentation.AWSLambda",
                "OpenTelemetry.Instrumentation.AWSLambda.TracerProviderBuilderExtensions",
                "AddAWSLambdaConfigurations",
                "SimpleOpenTelemetry:Trace:InstrumentationConfig:AWSLambda"),

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

            // AWS
            [MetricInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                "AddAWSInstrumentation",
                null)
        };
}
