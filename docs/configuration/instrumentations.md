# Instrumentation Configuration

**IMPORTANT**: ⚠️ **It is recommended you install [these versions tested against SimpleOpenTelemetry](../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

Set instrumentations in the the configuration `SimpleOpenTelemetry:[Metrics/Tracing]:Instrumentations[]` json arrays.

Options for instrumentations can be placed in `SimpleOpenTelemetry:[Metrics/Tracing]:InstrumentationConfig:<Type>:<OptionsField>`

eg `SimpleOpenTelemetry:Trace:InstrumentationConfig:AWS:SuppressDownstreamInstrumentation = "true"`

⚠️ _Complex types or Func<>/Action<>/etc aren't supported on Options fields. It will NOT be possible to use filters to prevent instrumentation of specific scenarios for AspNetCore, HttpClient, SqlClient etc eg (GET /health). You can either add+configure the instrumentation manually in code after AddSimpleOpenTelemetry() or if using an otel collector use a filter there (this generates more telemetry traffic/processing load)._ ⚠️

For a list of supported instrumentations see [MetricsInstrumentationEnum.cs](../../src/SimpleOpenTelemetry/OtelComponents/Instrumentation/MetricsInstrumentationEnum.cs) and [TracingInstrumentationEnum.cs](../../src/SimpleOpenTelemetry/OtelComponents/Instrumentation/TracingInstrumentationEnum.cs)

Available instrumentations are:

## AspNetCore

Documentation: [ASP.NET Core Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)

Package Stability: Stable

Signals: trace, metric

Options: trace only, unsupported, [AspNetCoreTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/AspNetCoreTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
"AspNetCore"
```

## HTTPClient

Documentation: [HttpClient and HttpWebRequest instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md)

Package Stability: Stable

Signals: trace, metric

Options: trace only, unsupported, [HttpClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/HttpClientTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
"HttpClient"
```

## AWS

Documentation: [AWS SDK client instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/README.md)

Package Stability: Stable

Signals: trace, metric

Options: metric only, optional, see [snippets/instrumentations/aws.json](./snippets/instrumentations/aws.json) and [AWSClientInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/AWSClientInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AWS --version x.x.x`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
"AWS"
```

## AWS Lambda

Documentation: [AWS OTel .NET SDK for Lambda](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/README.md)

Package Stability: Stable

Signals: trace

Options: optional, see [snippets/instrumentations/awslambda.json](./snippets/instrumentations/awslambda.json) and [AWSLambdaInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/AWSLambdaInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AWSLambda --version x.x.x`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
"AWSLambda"
```

## Sql Client

Documentation: [SqlClient Instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/README.md)

Package Stability: Stable

Signals: trace, metric

Options: trace only, unsupported, see [SqlClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/SqlClientTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.SqlClient --version x.x.x`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
"SqlClient"
```

## Entity Framework Core

Documentation: [EntityFrameworkCore Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/README.md)

Package Stability: Beta (as of July 2026)

Signals: trace

Options: unsupported, see [EntityFrameworkInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/EntityFrameworkInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version x.x.x`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
"EFCore"
```

## WCF

Documentation: [WCF Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/README.md)

Package Stability: Beta (as of July 2026)

Signals: trace

Options: unsupported, see [WcfInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/WcfInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Wcf --version x.x.x`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
"WCF"
```

## Runtime

Documentation: [Runtime Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/README.md)

Package Stability: Stable

Signals: metric

Options: unsupported, see [RuntimeInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/RuntimeInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Runtime --version x.x.x`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
"Runtime"
```

## Process

Documentation: [Process Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Process/README.md)

Package Stability: Beta (as of July 2026)

Signals: metric

Options: none

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Process --version x.x.x`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
"Process"
```

---
