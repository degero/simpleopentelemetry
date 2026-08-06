
# Instrumentation Configuration

Set instrumentations in the the configuration `SimpleOpenTelemetry:[Metrics/Tracing]:Instrumentations[]` json arrays.

Options for instrumentations can be placed in `SimpleOpenTelemetry:[Metrics/Tracing]:InstrumentationConfig:<Type>:<OptionsField>`

eg `SimpleOpenTelemetry:Trace:InstrumentationConfig:AWS:SuppressDownstreamInstrumentation = "true"`

**IMPORTANT**: ⚠️ *Complex types or Func<>/Action<>/etc aren't supported on Options fields. It will NOT be possible to use filters to prevent instrumentation of specific scenarios for AspNetCore, HttpClient, SqlClient etc eg (GET /health). You can either add+configure the instrumentation manually in code after AddSimpleOpenTelemetry() or if using an otel collector use a filter there (this generates more telemetry traffic/processing load).* ⚠️

Available instrumentations are:

## AspNetCore

Documentation: [ASP.NET Core Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)

Stability: Stable

Signals: trace, metric

Options: [AspNetCoreTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/AspNetCoreTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.15.2`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "AspNetCore"
```


## HTTPClient

Documentation: [HttpClient and HttpWebRequest instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md)

Stability: Stable

Signals: trace, metric

Options: unsupported [HttpClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/HttpClientTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Http --version 1.15.1`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "HttpClient"
```


## AWS

Documentation: [AWS SDK client instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/README.md)

Stability: Stable

Signals: trace, metric

Options:  [AWSClientInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/AWSClientInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AWS --version 1.15.1`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "AWS"
```

For supported configurable options see [snippets/instrumentations/aws.json](./snippets/instrumentations/aws.json)


## AWS Lambda

Documentation: [AWS OTel .NET SDK for Lambda](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/README.md)

Stability: Stable

Signals: trace

Options: [AWSLambdaInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/AWSLambdaInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AWSLambda --version 1.15.1`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
 "AWSLambda"
```

For supported configurable options see [snippets/instrumentations/awslambda.json](./snippets/instrumentations/awslambda.json)


## Sql Client

Documentation: [SqlClient Instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/README.md)

Stability: Stable

Signals: trace, metric

Options: [SqlClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/SqlClientTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.SqlClient --version 1.15.2`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "SqlClient"
```


## Entity Framework Core

Documentation: [EntityFrameworkCore Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/README.md)

Stability: Beta (as of July 2026)

Signals: trace

Options: unsupported [EntityFrameworkInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/EntityFrameworkInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version 1.15.1-beta.1`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
 "EFCore"
```


## WCF

Documentation: [WCF Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/README.md)

Stability: Beta (as of July 2026)

Signals: trace

Options: [WcfInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/WcfInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Wcf --prerelease --version 1.15.1-beta.2`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
 "WCF"
```


## Runtime

Documentation: [Runtime Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/README.md)

Stability: Stable

Signals: metric

Options: [RuntimeInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/RuntimeInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Runtime --version 1.15.1`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
 "Runtime"
```


## Process

Documentation: [Process Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Process/README.md)

Stability: Beta (as of July 2026)

Signals: metric

Options: none

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Process --prerelease --version 1.15.1-beta.1`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
 "Process"
```


---

