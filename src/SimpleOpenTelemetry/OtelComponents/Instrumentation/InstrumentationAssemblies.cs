using SimpleOpenTelemetry.OtelComponents.Common;

namespace SimpleOpenTelemetry.OtelComponents.Instrumentation;

internal static class InstrumentationAssemblies
{
    public static readonly Dictionary<TraceInstrumentationEnum, AssemblyDescriptor>
        KnownTraceInstrumentations = new()
        {
            [TraceInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Trace.AspNetCoreInstrumentationTracerProviderBuilderExtensions",
                [ "AddAspNetCoreInstrumentation" ]),

            [TraceInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Trace.HttpClientInstrumentationTracerProviderBuilderExtensions",
                [ "AddHttpClientInstrumentation" ]),

            [TraceInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                [ "AddSqlClientInstrumentation" ]),

            [TraceInstrumentationEnum.EFCore] = new(
                "OpenTelemetry.Instrumentation.EntityFrameworkCore",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                [ "AddEntityFrameworkCoreInstrumentation" ]),


            // WCF (client-side; server-side is configured via behavior XML/code)
            [TraceInstrumentationEnum.Wcf] = new(
                "OpenTelemetry.Instrumentation.Wcf",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                [ "AddWcfInstrumentation" ]),

            // AWS SDK
            [TraceInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Trace.TracerProviderBuilderExtensions",
                [ "AddAWSInstrumentation" ],
                "AWSClientInstrumentationOptions"),

            // AWS Lambda
            [TraceInstrumentationEnum.AWSLambda] = new(
                "OpenTelemetry.Instrumentation.AWSLambda",
                "OpenTelemetry.Instrumentation.AWSLambda.TracerProviderBuilderExtensions",
                [ "AddAWSLambdaConfigurations" ],
                "AWSLambdaInstrumentationOptions"),

        };


    public static readonly Dictionary<MetricInstrumentationEnum, AssemblyDescriptor>
        KnownMetricsInstrumentations = new()
        {
            [MetricInstrumentationEnum.AspNetCore] = new(
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Metrics.AspNetCoreInstrumentationMeterProviderBuilderExtensions",
                [ "AddAspNetCoreInstrumentation" ]),

            [MetricInstrumentationEnum.HttpClient] = new(
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Metrics.HttpClientInstrumentationMeterProviderBuilderExtensions",
                [ "AddHttpClientInstrumentation" ]),

            [MetricInstrumentationEnum.SqlClient] = new(
                "OpenTelemetry.Instrumentation.SqlClient",
                "OpenTelemetry.Metrics.SqlClientMeterProviderBuilderExtensions",
                [ "AddSqlClientInstrumentation" ]),

            [MetricInstrumentationEnum.Runtime] = new(
                "OpenTelemetry.Instrumentation.Runtime",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                [ "AddRuntimeInstrumentation" ]),

            // Process metrics
            [MetricInstrumentationEnum.Process] = new(
                "OpenTelemetry.Instrumentation.Process",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                [ "AddProcessInstrumentation" ]),

            // AWS
            [MetricInstrumentationEnum.AWS] = new(
                "OpenTelemetry.Instrumentation.AWS",
                "OpenTelemetry.Metrics.MeterProviderBuilderExtensions",
                [ "AddAWSInstrumentation" ])
        };
}
