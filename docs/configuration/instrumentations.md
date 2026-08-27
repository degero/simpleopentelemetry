# Instrumentation Configuration

⚠️ **IMPORTANT** **It is recommended you install [package versions tested against SimpleOpenTelemetry](../otel-component-versions.md) referenced below.** ⚠️

<br/>

Set metrics and trace instrumentations in the the configuration:

```json
 "SimpleOpenTelemetry": {
    "Trace": {
      "Instrumentations": [],
      "InstrumentationConfig": {},
    },
    "Metric": {
      "Instrumentations": [],
      "InstrumentationConfig": {}
    }
 }
```

Options for instrumentations can be placed in `SimpleOpenTelemetry:[Metric/Trace]:InstrumentationConfig:<Type>:<OptionsField>`

eg `SimpleOpenTelemetry:Trace:InstrumentationConfig:AWS:SuppressDownstreamInstrumentation = "true"`

⚠️ _Complex types or Func<>/Action<>/etc aren't supported on Options fields. It will NOT be possible to use filters to prevent instrumentation of specific scenarios for AspNetCore, HttpClient, SqlClient etc eg (GET /health). You can either add+configure the instrumentation manually in code after AddSimpleOpenTelemetry() or if using an otel collector use a filter there (this generates more telemetry traffic/processing load)._ ⚠️

Available instrumentations are:

- [AspNetCore](#aspnetcore)
- [AWS](#aws)
- [AWSLambda](#aws-lambda)
- [EFCore](#entity-framework-core)
- [HttpClient](#httpclient)
- [Process](#process)
- [Runtime](#runtime)
- [SqlClient](#sql-client)
- [WCF](#wcf)

<br/>

## AspNetCore

**Signals supported**: trace, metric

**Package Stability**: Stable

**Options**: trace only, unsupported, [AspNetCoreTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/AspNetCoreTraceInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x`

SimpleOpenTelemetry:Trace/Metric:Instrumentations[] json:

```json
"AspNetCore"
```

**Documentation**: [ASP.NET Core Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)

## HTTPClient

**Signals supported**: trace, metric

**Package Stability**: Stable

**Options**: trace only, unsupported, [HttpClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/HttpClientTraceInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x`

SimpleOpenTelemetry:Trace/Metric:Instrumentations[] json:

```json
"HttpClient"
```

**Documentation**: [HttpClient and HttpWebRequest instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md)

## AWS

**Signals supported**: trace, metric

**Package Stability**: Stable

**Options**: metric only, optional, see [snippets/instrumentations/aws.json](./snippets/instrumentations/aws.json) and [AWSClientInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/AWSClientInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.AWS --version x.x.x`

SimpleOpenTelemetry:Trace/Metric:Instrumentations[] json:

```json
"AWS"
```

**Documentation**: [AWS SDK client instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/README.md)

## AWS Lambda

**Signals supported**: trace

**Package Stability**: Stable

**Options**: optional, see [snippets/instrumentations/awslambda.json](./snippets/instrumentations/awslambda.json) and [AWSLambdaInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/AWSLambdaInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.AWSLambda --version x.x.x`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
"AWSLambda"
```

**Documentation**: [AWS OTel .NET SDK for Lambda](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/README.md)

## Sql Client

**Signals supported**: trace, metric

**Package Stability**: Stable

**Options**: trace only, unsupported, see [SqlClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/SqlClientTraceInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.SqlClient --version x.x.x`

SimpleOpenTelemetry:Trace/Metric:Instrumentations[] json:

```json
"SqlClient"
```

**Documentation**: [SqlClient Instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/README.md)

## Entity Framework Core

**Signals supported**: trace

**Package Stability**: Beta (as of July 2026)

**Options**: unsupported, see [EntityFrameworkInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/EntityFrameworkInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version x.x.x`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
"EFCore"
```

**Documentation**: [EntityFrameworkCore Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/README.md)

## WCF

**Signals supported**: trace

**Package Stability**: Beta (as of July 2026)

**Options**: unsupported, see [WcfInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/WcfInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.Wcf --version x.x.x`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
"WCF"
```

**Documentation**: [WCF Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/README.md)

## Runtime

**Signals supported**: metric

**Package Stability**: Stable

**Options**: unsupported, see [RuntimeInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/RuntimeInstrumentationOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.Runtime --version x.x.x`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
"Runtime"
```

**Documentation**: [Runtime Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/README.md)

## Process

**Signals supported**: metric

**Package Stability**: Beta (as of July 2026)

**Options**: none

**Nuget Package**: `dotnet add package OpenTelemetry.Instrumentation.Process --version x.x.x`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
"Process"
```

**Documentation**: [Process Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Process/README.md)

---
