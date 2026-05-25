# SimpleOpenTelemetry

A lightweight, low-friction .NET library providing a simple, low code option to setup code-based OpenTelemetry instrumentation on dotnet applications via configuration file or env vars.


**Supported Frameworks:** .NET 10.0, .NET 8.0 

**Supported .Net App Host Patterns:** WebApplication Host / .Net Generic Host / Non generic host.  

**License:** MIT

| Status | |
| ------ | --- |
| Stability | Alpha |
| Code Owners | [@degero](https://github.com/degero) |


[![NuGet version badge](https://img.shields.io/nuget/v/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![NuGet download count badge](https://img.shields.io/nuget/dt/SimpleOpenTelemetry)](https://www.nuget.org/packages/SimpleOpenTelemetry)
[![codecov](https://codecov.io/gh/degero/simpleopentelemetry/graph/badge.svg?token=USK6CSKHSJ)](https://codecov.io/gh/degero/simpleopentelemetry)


---

## Overview


SimpleOpenTelemetry handles the boilerplate configuration needed when using manual code-based OpenTelemetry setup. Rather than using OpenTelemetry's fluent api, settings are defined in a configuration file / env vars. It is not in any way related to [auto-instrumentation/zero-code](https://opentelemetry.io/docs/concepts/instrumentation/zero-code/) and is designed to streamline setup for most common configurations. If you need to extend on what SimpleOpenTelemetry provides, you can access the OpenTelemetryBuilder to run any of OpenTelemetry's fluent api methods.

---

## Features

- Pluggable components by adding config entry and NuGet package to your app for telemetry features you need. 
- Example configuration files for common app / platform scenarios [example-configs](./example-configs/)
- Set telemetry attribute 'service.version' based on app assembly version when using builtin ResourceDetector 'AssemblyVersion' (see [AssemblyVersion](#assemblyversion)). Overridden by setting 'service.version' in OTEL_RESOURCE_ATTRIBUTES of appsettings.json / env var
- 'All signal' exporter option overridable at the signal level for exporter type


---

## Getting Started

- Add the SimpleOpenTelemetry nupkg: `dotnet add package --prerelease SimpleOpenTelemetry`
- Add a "SimpleOpenTelemetry": {} root section to your appsettings.{environment}.json and read the next sections to setup.
- Add boostrapping code:
  - For Generic Host apps like aspnetcore (or any apps using WebApplicationBuilder/HostApplicationBuilder):
    - In your startup code (eg Program.cs) add `using SimpleOpenTelemetry.Extensions;` and before builder.build() add `builder.AddSimpleOpenTelemetry();`
    - Optionally, to validate OpenTelemetry have the key app identifiers set, run `app.Services.SimpleOpenTelemetryValidate();` after `var app = builder.Build();`. This writes any errors to the EventLog and returns false if invalid.
  - For Standalone apps (no Generic host): 
    - In your startup code file add `using Microsoft.Extensions.Logging; using SimpleOpenTelemetry;` and   
    ```csharp
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddOpenTelemetry();
        // You may want to set log levels using builder.AddConfiguration()
    });

    var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);

    // on shutdown to push telemetry before closing - you can also access this via StandaloneApp.Sdk
    sdk.Dispose();

    ```
  - Optionally, to validate OpenTelemetry have the key app identifiers set, run `app.Services.SimpleOpenTelemetryValidate();`   after `var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);`. This writes any errors to the EventLog and returns false if invalid.
---

For more detail on OpenTelemetry's two methods of use covered above see: 
  
  - [Initialize the SDK using a host](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/README.md#initialize-the-sdk-using-a-host)

  - [Initialize the SDK manually](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/README.md#initialize-the-sdk-manually)


## Examples

If you are in TLDR; mode to bother with the next sections, head over to the [example applications](./examples/) and [example configs](./example-configs/).


---


## Diagnostics and Error handling

SimpleOpenTelemetry follows the same [spec guideline](https://opentelemetry.io/docs/specs/otel/error-handling/) as OpenTelemetry for error handling in that it 'MUST NOT throw unhandled exceptions at runtime.'. Building on that it will not throw any errors if a configuration does not work (eg config env var / files change) it will not prevent the app from running. Note that "SimpleOpenTelemetry-" prefixed events only occur at the app startup and will only emit if a listener is registered before starting.

SimpleOpenTelemetry will throw exceptions for null parameters passed to it's registration methods. SimpleOpenTelemetry records any errors as diagnostics events (as OpenTelemetry does). These events will have a "SimpleOpenTelemetry-" prefix. Projects in the [examples](./examples/) folder demonstrate listening to this and "OpenTelemetry-" events and outputting to console. This maybe useful to adapt from and use if you app environment only has stdout as a means to view events.

Some options to listen to events if not using a code based event listener/console output in the examples:

### Using dotnet-trace:

With a published app:
```
dotnet tool install --global dotnet-trace
dotnet-trace collect --providers "SimpleOpenTelemetry-Core:0xFFFFFFFF:5" -- dotnet .\AspNetCore.dll
```

### Using Perfview

1. install PerfView: `winget install PerfView`
2. Run as administrator
3. Menu 'collect' -> 'collect'
4. Uncheck all in the section starting 'Kernel base'
5. Type '*SimpleOpenTelemetry-Core'
6. Click 'Start Collection'
7. Start the app and interact with it
6. Click 'Stop Collection' and close this windows
8. View events in the datafile created



Information on collecting OpenTelemetry events: [OpenTelemetry Troubleshooting](https://opentelemetry.io/docs/languages/dotnet/troubleshooting/)

---

## Configuration

*IMPORTANT*: ⚠️ Config keys and values are CASE INSENSITIVE ⚠️

Key configurable components:  

- Distributions
- Trace/Metric/Log Exporters - including allowing multiple exporters for each signal type 
- Trace/Metric Instrumentation - for key .net components (aspnetcore, runtime...) and vendor libraries (Azure, AWS...)
- Trace/Metric/Log Extensions -  eg AddAWSXRayTraceId()
- Custom meters
- Trace sources
- Resource detectors
- Samplers  
- Extensions
- Exporters
 <br>
 <br>


While all OpenTelemetry components in [OpenTelemetry-dotnet-contrib](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib) distros, and vendor implementations of components *could* be loaded using SimpleOpenTelemetry's configuration syntax, these are gated through registered assembly sets in the below folders to ensure those configurations have been tested in this repo:

- [Distros](./src/SimpleOpenTelemetry/OtelComponents/Distro/DistroAssemblies.cs)
- [Exporters](./src/SimpleOpenTelemetry/OtelComponents/Exporter/ExporterAssemblies.cs)
- [Trace / Metric Instrumentations](./src/SimpleOpenTelemetry/OtelComponents/Instrumentation/InstrumentationAssemblies.cs)
- [Extensions](./src/SimpleOpenTelemetry/OtelComponents/Extensions/ExtensionAssemblies.cs)
- [Samplers](./src/SimpleOpenTelemetry/OtelComponents/Sampler/SamplerAssemblies.cs)  
- [Propagators](./src/SimpleOpenTelemetry/OtelComponents/Propagator/PropagatorAssemblies.cs)  
- [Resource Detectors](./src/SimpleOpenTelemetry/OtelComponents/Resource/ResourceDetectorAssemblies.cs)

<br>

If there is one you would like added, feel free to fork and raise a PR, or [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new).


---


#### Configuration sources

As SimpleOpenTelemetry uses dotnet's IConfiguration concepts and abstractions, it relies on the default configuration sources setup in generic host platforms to load in appsettings.json. This also means you can also [add in other configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) before calling AddSimpleOpenTelemetry(). These are particularly useful for sensitive values (keys, secrets etc).  

As the IConfigurationProvider for environment variables is enabled by default, you can define them all in environment variables or override the json file "SimpleOpenTelemetry" settings.

For local development with sensitive values, it is recommended to take advantage of [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).  


---

#### Environment variables

The OTEL_* environment variables / json config are partially supported (see below) and load in by default (as this is done by the underlying OpenTelemetry SDK registration) but for many components those settings can be defined/overridden explicitly for their signal type/functionality in the configuration file.

Some useful OTEL_ environment variables you can make use of (* indicates a core recommended setting to set):  

- [OTEL_SERVICE_NAME](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)
- [OTEL_RESOURCE_ATTRIBUTES](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)
- [OTEL_METRICS_EXEMPLAR_FILTER](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#exemplar)
- [OTEL_SDK_DISABLED](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)


*Important*

 The OpenTelemetry Documentation [SDK Environment Variables](https://OpenTelemetry.io/docs/specs/otel/configuration/sdk-environment-variables) page is a specification not a reference for the dotnet implementation. Many of these are (as of april 2026) unsupported such as **OTEL_PROPAGATORS, OTEL_TRACES_EXPORTER, OTEL_LOGS_EXPORTER, OTEL_METRICS_EXPORTER** 
> If you wish to make use of any of the environment variables in the spec but not above, check the [dotnet documentation to confirm it is implemented](https://OpenTelemetry.io/docs/languages/dotnet/getting-started/), or even quicker too dive into the [OpenTelemetry-dotnet repo](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main) to search.
>

---


#### Configuration file setup

*IMPORTANT*: SimpleOpenTelemetry will log error events and skip its setup if key settings are missing or misconfigured

To get started, add a "SimpleOpenTelemetry" section to the root of your appsettings.json / appsettings.{Environment}.json file in your project folder. SimpleOpenTelemetry will set up all the components with OpenTelemetry for your application. If this is not set it will not run AddOpenTelemetry() with your application.  

Similarly for the subsections "Metric/Trace/Log", OpenTelemetry's WithLogging/Tracing/Metrics() extension methods will only run (and  subsequent exports etc) when the corresponding section exists. If at least on is not set it will not run AddOpenTelemetry() with your application.

For a json configuration file, you can start with one of the pre-built ones in [](./example-configs/) or setup the top level config items and follow the next sections covering the items you can add:  


```json
"SimpleOpenTelemetry": {
    "Distro": "",
    "Trace": {
      "Instrumentations": [],
      "InstrumentationConfig": {},
      "Sources": [],
      "Exporters": [],
      "Extensions": [],
      "Settings": {}
    },
    "Metric": {
      "Instrumentations": [],
      "InstrumentationConfig": {},
      "Exporters": [],
      "Settings": {},
      "CustomMeters": []
    },
    "Log": {
      "Exporters": [],
      "Settings": {}
    },
    "ExporterOptions": {},
    "Resource": {
      "Detectors": [ ],
    },
    "ResourceDetectorConfig": {},
    "Propagators": [],
    "Sampler": ""
}

```  

---


#### When something you need isn't configurable

If a component type (eg. Processors), extension or setting isn't available to configure you can load/configure it in code using the OpenTelemetryBuilder returned from AddSimpleOpenTelemetry().

There is also a registry of libraries or custom components/extensions you can add doing the above, 
check the [OpenTelemetry Registry](https://OpenTelemetry.io/ecosystem/registry/).   

If what you need isn't available, you can build your own following the OpenTelemetry guidelines for [traces](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/trace/extending-the-sdk/README.md) [logs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/logs/extending-the-sdk/README.md) and [metrics](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/metrics/extending-the-sdk/README.md).  


---


## Configuration

The next sections cover setting up the subsections of your "SimpleOpenTelemetry" config


- [Distributions](#Distribution)
- [Logging](#logging)
- [Metrics](#metrics)
- [Tracing](#tracing)
- [Instrumentation](#Instrumentation)
- [Exporters](#exporters)
- [Resource Detectors](#resource-detectors)
- [Samplers](#samplers)
- [Extensions](#extensions)



---


### Distribution

A distribution in terms of OpenTelemetry is '... a customized version of an OpenTelemetry component...'. 

In the case of SimpleOpenTelemetry, it is a library that will set up all signal collection and exporting settings for you with only a few minor settings such as exporter endpoints. By setting a distribution in your configuration, *all other configuration areas will be ignored*. The OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES settings/env vars however are picked up. This means any of the features listed previously will not be available as to not interfere with the distro.

For a list of all OpenTelemetry distros see [OpenTelemetry - Third-party distributions](
https://opentelemetry.io/ecosystem/distributions/)


#### Azure Monitor AspNetCore

*Important* This Distro only supports use with generic host WebApplication (does not support using with StandaloneApp.AddSimpleOpenTelemetry()). If you wish to setup for Azure Monitor in a Standalone app, configure to use the [Azure Monitor Exporter](#azure-monitor-exporter) but note some features of the distro wont be included, see 'Why should I use the Azure Monitor OpenTelemetry Distro?' link below.

This Distro sets up all signal collection and exporting to Azure monitor. It also sets up several types of instrumentation, resource detectors and more. If you want more control over your setup you can still use most (not all) features provided in the Distro (see the link below) via the other configuration item covered in the following sections. NOTE: Azure RBAC auth is not currently supported.

Documentation:  
[GitHub Azure SDK - Azure.Monitor.OpenTelemetry.AspNetCore](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/README.md)  
[MSLearn - Enable Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)  
[MSLearn - Why should I use the Azure Monitor OpenTelemetry Distro?](https://learn.microsoft.com/en-us/azure/azure-monitor/app/application-insights-faq#why-should-i-use-the-azure-monitor-opentelemetry-distro)


Nuget Package:
`dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore`   

SimpleOpenTelemetry:Distro json:  

```json
"AzureMonitorAspNetCore"
```

Configuration:  

You will need to specify an Application Insights connection string. It is recommend to set as an Environment variable and for local development, using dotnet user-secrets. [MSLearn - Use OpenTelemetry with Azure Monitor and Application Insights](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-applicationinsights#3-specify-the-connection-string)

Notes:  

This distro provides no option to set Trace sources and only sets up `Azure.*` as a source. If you wish to have custom traces in your app you will need to add them in code to the OpenTelemetry builder. For an example see the [aspnetcore example WithTracing() setup](./examples/aspnetcore/Program.cs)

If you add a package `OpenTelemetry.Instrumentation.SqlClient` you will need to configure it by code. As the distro will backoff from setting up its own internal sqlclient instrumentation if it detects it.


---


### Logging  

Logging providers are not cleared by SimpleOpenTelemetry (, but one will be added if the SimpleOpenTelemetry:Log section is defined. If you wish to have only use this provider and not the defaults in a Generic host application run `builder.Logging.ClearProviders()` before AddSimpleOpenTelemetry() as you can see in the [examples](./examples/).


#### Settings

- IncludeFormattedMessage - bool (default: false)
- IncludeScopes - bool (default: false)
- ParseStateValues - bool (default: false)

[View OpenTelemetryLoggerOptions.cs for settings details](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Logs/ILogger/OpenTelemetryLoggerOptions.cs)


---


### Metrics  

#### Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via "SimpleOpenTelemetry:Metric:Settings":

- MetricLimit - int (default: 1000)

OpenTelemetry Documentation: [opentelemetry.io metrics best practices](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices)


---


### Tracing  

#### Settings

The following are supported to switch on OpenTelemetry dotnet SDK settings via "SimpleOpenTelemetry:Trace:Settings":

- SetErrorStatusOnException - bool (default: false)

OpenTelemetry Documentation: [opentelemetry.io traces reporting exceptions](https://opentelemetry.io/docs/languages/dotnet/traces/reporting-exceptions/)


---


### Instrumentation  

Any options for instrumentations can be placed in 

`SimpleOpenTelemetry:Signal:InstrumentationConfig:<Type>:<OptionsField>` 

eg `SimpleOpenTelemetry:Trace:InstrumentationConfig:AWS:SuppressDownstreamInstrumentation = "true"`



#### AspNetCore

Documentation: [ASP.NET Core Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)

Stability: Stable

Signals: trace, metric  

Options: [AspNetCoreTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/AspNetCoreTraceInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AspNetCore`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "AspNetCore"
```

#### HTTP

Documentation: [HttpClient and HttpWebRequest instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/README.md)

Stability: Stable

Signals: trace, metric  

Options: unsupported [HttpClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Http/HttpClientTraceInstrumentationOptions.cs)

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Http`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "HttpClient"
```

#### AWS

Documentation: [AWS SDK client instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/README.md)

Stability: Stable

Signals: trace, metric

Options:  [AWSClientInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWS/AWSClientInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AWS`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "AWS"
```


#### AWS Lambda

Documentation: [AWS OTel .NET SDK for Lambda](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/README.md)

Stability: Stable

Signals: trace  

Options: [AWSLambdaInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.AWSLambda/AWSLambdaInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.AWSLambda`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
 "AWSLambda"
```


#### Sql Client

Documentation: [SqlClient Instrumentation for OpenTelemetry](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/README.md)

Stability: Stable

Signals: trace, metric  

Options: [SqlClientTraceInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.SqlClient/SqlClientTraceInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.SqlClient`

SimpleOpenTelemetry:<Signal>:Instrumentations[] json:

```json
 "SqlClient"
```

#### Entity Framework Core

Documentation: [EntityFrameworkCore Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/README.md)

Stability: Beta (as of April 2026)

Signals: trace  

Options: unsupported [EntityFrameworkInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/EntityFrameworkInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
 "EFCore"
```

#### WCF

Documentation: [WCF Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/README.md)

Stability: Beta (as of April 2026)

Signals: trace  

Options: [WcfInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Wcf/WcfInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Wcf`

SimpleOpenTelemetry:Trace:Instrumentations[] json:

```json
 "WCF"
```


#### Runtime

Documentation: [Runtime Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/README.md)

Stability: Stable

Signals: metric  

Options: [RuntimeInstrumentationOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/RuntimeInstrumentationOptions.cs) 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Runtime`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
 "Runtime"
```

#### Process

Documentation: [Process Instrumentation for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Process/README.md)

Stability: Beta (as of April 2026)

Signals: metric  

Options: none 

Nuget Package: `dotnet add package OpenTelemetry.Instrumentation.Process`

SimpleOpenTelemetry:Metric:Instrumentations[] json:

```json
 "Process"
```

---


### Exporters  

These can be set under the config section "SimpleOpenTelemetry:[Metrics/Tracing/Logging]:Exporters". It supports both the OpenTelemetry SDK exporters (otlp, console, prometheus) and other contrib / vendor exporters. Each array item can have an 'options' key to specify any settings particular to that exporter.

You can set exporter options for all signals in "SimpleOpenTelemetry:ExporterOptions:[exportername]" or under "SimpleOpenTelemetry:[Metrics/Tracing/Logging]:Exporters" array item "options" field.  Setting them here overrides an 'all signal' option

For a full list of all the supported exporters see [TraceExporterEnum / MetricExporterEnum / LogExporterEnum](./src/SimpleOpenTelemetry/Exporter/ExporterAssemblies.cs)

For examples see the [example-configs/exporter folder](./example-configs/exporter/)

#### OTLP exporter

Signals supported: trace, metric, log  

Stability: Stable

Documentation:  [OpenTelemetry OTLP Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)  

Options: optional 

Notes: All OpenTelemetry SDK OTEL_ environment variables or (root) settings json values will be used to send to OTLP endpoints for entries don't have options defined  

Nuget Package: none (builtin to OpenTelemetry .net lib)

SimpleOpenTelemetry:<SignalType>:Exporters[] json:
```json
{ "type": "otlp" }
```  

If you want to export to multiple OTLP endpoints / have full configuration options add the below, for field names/values see [OtlpExporterOptions.cs)](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs))

SimpleOpenTelemetry:<SignalType>:Exporters[] json:
```json
{ "type": "otlp", "options": {...} }
```
  
---


#### Console Exporter

Signals supported: trace, metric, log  

Stability: Stable (for dev purposes only)

Documentation: [OpenTelemetry Console Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md)

Options: none (unsupported, see above readme for supported OTEL_* environment variables/json config)

Nuget Package: none (builtin to OpenTelemetry .net lib)

SimpleOpenTelemetry:<SignalType>:Exporters[] json:

```json
{ "type": "otlp" }
```


---


#### Prometheus HttpListener Exporter

Signals supported: metric 

Stability: Stable (for dev purposes only)

Documentation: [OpenTelemetry Prometheus HttpListener Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/README.md)

Options: optional (see [PrometheusHttpListenerOptions.cs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/PrometheusHttpListenerOptions.cs))

Notes: This is only for dev use. It is never intended for prod. Defaults to host prometheus scrape endpoint on http://localhost:9464/metrics. Not recommended for aspnetcore apps, instead use [Prometheus AspNetCore Exporter](#prometheus-aspnetcore-exporter-prerelease)

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Exporter.Prometheus.HttpListener`  

SimpleOpenTelemetry:Metric:Exporters[] json:

```json
{ "type": "prometheushttplistener", "options": {...} }
```


---


#### Prometheus AspNetCore Exporter

Signals supported: metric

Stability: Beta (as of april 2026)

Documentations: [OpenTelemetry Prometheus AspNetCore Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md)

Options: optional, the documentation doesn't seem to mention but you can set anything defined in 'PrometheusAspNetCoreOptions.cs' of this project.

Notes: For AspNetCore apps only. Hosts prometheus scrape endpoint defaulted on http://apphost:port/metrics.  

Nuget Package:  
`dotnet add package --prerelease OpenTelemetry.Exporter.Prometheus.AspNetCore`  

SimpleOpenTelemetry:Metric:Exporters[] json:

```json
{ "type": "prometheusaspnetcore", "options": {...} }
```

Additional setup needed:

```csharp
Program.cs
```
var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();


---


#### Azure Monitor exporter

Signals supported: trace, metric, log  

Stability: Stable

Documentation: [Azure Monitor Exporter client library for .NET README.md](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/README.md) 

Options: mandatory (if not defined in SimpleOpenTelemetry:ExporterOptions:Azure:ConnectionString or APPLICATIONINSIGHTS_CONNECTION_STRING) 
[AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)   

Notes: This exporter does not support Live Metrics, for this, use the distro if using AspNet Core or the [AzureMonitorExporter Extension](#azure-monitor-exporter-1). Also if you want all signals exported all with the same settings it is simpler to use the extension. This only utilizes most but not all of the [Azure Monitor AspNet Core Distro](#azure-monitor-aspnetcore) features.

RBAC access via the 'Credential' option is supported. See the example-config. You can set sampling options (it has builtin sampler setup, different to OTEL_TRACES_SAMPLER_* settings), and more in the options. 

Nuget Package:
`dotnet add package Azure.Monitor.OpenTelemetry.Exporter`

SimpleOpenTelemetry:<SignalType>:Exporters[] json:  

 ```json
 { "type": "AzureMonitor", "options": {...} }
 ```


---

### Resource Detectors

Resource detectors are set under SimpleOpenTelemetry:Resource:Detectors[] string array.

All the supported resource detectors are listed here [ResourceDetectorEnum](./src/SimpleOpenTelemetry/Resource/ResourceDetectorEnum.cs)


#### AssemblyVersion

Stability: Stable

Notes: Examines the 'built' assembly version that may be set in a CICD pipeline and in msbuild and assigns this to service.version resource attribute. Avoids the need to explicitly set service.version in config. eg set a dotnet build / publish parameter V-p:Version=<<MyVersion>> 

Nuget Package: not needed (built into SimpleOpenTelemetry)

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "AssemblyVersion"
 ```

---


#### Host

Stability: Beta (as of april 2026)

Documentation: [Resource Host Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Host/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Host --prerelease`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "host"
 ```


#### Container

Stability: Beta (as of april 2026)

Documentation: [Container Resource Detector README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Container/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Container --prerelease`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "container"
 ```


#### Operating System

Stability: Alpha (as of april 2026)

Documentation: [Operating System Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.OperatingSystem/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.OperatingSystem --prerelease`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "os"
 ```


#### Process

Stability: Beta (as of april 2026)

Documentation: [Process Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Process/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.Process --prerelease`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "process"
 ```


#### Process Runtime

Stability: Beta (as of april 2026)

Documentation: [Process Runtime Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.ProcessRuntime/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.ProcessRuntime --prerelease`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "processruntime"
 ```


#### AWS

*AWS*

Stability: Stable

Documentation: [AWS Resource Detectors](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.AWS/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Resources.AWS`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "aws"
 ```

(Optional) SimpleOpenTelemetry:ResourceDetectorConfig:AWS json:  

 ```json
 { "SemanticConventionVersion": "V1_29_0" }
 ```



#### Azure

Stability (as April 2026): Beta (as of march 2026)

Documentation: [Resource Detectors for Azure cloud environments](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Azure/README.md)

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Resources.Azure`

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "azure"
 ```



#### Google Cloud Platform

Stability: Alpha (as April 2026)

Documentation: [Resource Detectors for Google Cloud Platform](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Gcp/README.md)

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Resources.Gcp`
 
SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "gcp"
 ```


#### EnvVar

Stability: Stable

Notes: OpenTelemetry SDK adds this by default. Only use this if the SDK changes to not include it by default.

Documentation: [OpenTelemetry SDK ResourceBuilderExtensions.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/08df7481053204a5ba10c61bb4f1a21d5d3fcefa/src/OpenTelemetry/Resources/ResourceBuilderExtensions.cs#L124)

Nuget Package: not needed (Opentelemetry SDK)

SimpleOpenTelemetry:Resource:Detectors[] json:  

 ```json
 "EnvVar"
 ```


---


### Propagators


> *Important*
> The OpenTelemetry SDK env var OTEL_PROPAGATORS is not supported (as of April 2026) in the dotnet sdk implementation


You can add multiple propagators in the SimpleOpenTelemetry:Propagators[] json array. 


**Nuget Packages**
You can make use of OpenTelemetry's builtin default [SDK propagators](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main/src/OpenTelemetry.Api/Context/Propagation) without adding nupkg. To use the B3 propagator you will need to add the core sdk extensions nupkg: `dotnet add package OpenTelemetry.Extensions.Propagators`  

**Available Propagators**
For a full list of all the supported propagators see [PropagatorEnum](./src/SimpleOpenTelemetry/Propagator/PropagatorAssemblies.cs)


#### Default

OpenTelemetry initialisation defaults to use a 'CompositeTextMapPropagator' of BaggagePropagator (spec: 'baggage') and TraceContextPropagator (spec:'tracestate','traceparent'). By setting as Propagators as `null` or `[]` this will use the default.

The equivalent config setting (if you wish to append more to the default) being:

```json
"Propagators": ["tracecontext", "baggage"]
```

#### Disable

If you wish to disable this, explicitly set SimpleOpenTelemetry:Propagators as:

```json
{ "SimpleOpenTelemetry": { "Propagators": [ 'none' ]] } }
```


#### AWS X-Ray Id Propagator

Stability: Stable

Documentation: [AWS X-Ray Id Propagator](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Extensions.AWS` 

SimpleOpenTelemetry:Propagators[] json:  

 ```json
 "aws"
 ```


---


### Samplers


The below allow vendor sampler configuration as an alternative to OpenTelemetry's [built-in samplers](https://OpenTelemetry.io/docs/specs/otel/trace/sdk/#built-in-samplers). Builtin samplers can be set in OTEL_TRACES_SAMPLER of the root json configuration or env var. Some requires values in OTEL_TRACES_SAMPLER_ARG. The sampler defaults to 'parentbased_always_on'.  

For a full list of all the additional supported samplers see [SamplerEnum](./src/SimpleOpenTelemetry/Sampler/SamplerAssemblies.cs)

For Azure users, sampling is built into the exporter setup/options.


#### AWS X-Ray Remote Sampler (Currently unsupported due to irregular registration pattern requiring prebuilt resource)

Stability (as April 2026): Alpha

Documentation: [AWS X-Ray Remote Sampler](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Sampler.AWS/README.md)

Nuget Package:
`dotnet add package OpenTelemetry.Sampler.AWS --prerelease`  
 
SimpleOpenTelemetry:Samplers[] json:  

```json
 "aws"
```


---


### Extensions  


Extensions offer (as the name suggests) the ability to extend the OpenTelemetry SDK beyond the core spec where it does not fall into the key component categories above.



#### Azure Monitor Exporter

Stability: Stable

Signal: All

Documentation: [Azure Monitor Exporter client library for .NET - Add the Exporter for all signals]https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter#add-the-exporter-for-all-signals)

Notes: This is the same underlying exporter as [Azure Monitor exporter](#azure-monitor-exporter) with one crucial difference supporting Live Metrics (on by default, only configurable using this extension). Live metrics will only work with a Generic host application and will not work with StandaloneApp.AddSimpleOpenTelemetry(). It also simplifies your config if you want exports for all signals with all the same settings.

Nuget Package:
`dotnet add package Azure.Monitor.OpenTelemetry.Exporter`
 
SimpleOpenTelemetry:BuilderExtensions[] json:  

 ```json
 { "Type": "AzureMonitorExporter", "Options": {...} }
 ```


---

#### AWS X-Ray Remote Sampler

Stability: Stable

Signal: Trace

Documentation: [AWS X-Ray Remote Sampler](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)

Notes: This is commonly used with the AWS Xray Propagator as mentioned in README.md above.

Nuget Package:
`dotnet add package OpenTelemetry.Extensions.AWS`  
 
SimpleOpenTelemetry:Trace.Extensions[] json:  

 ```json
 "awsxraytraceid"
 ```



---



## Monitoring your apps

### Distributed tracing

For an example of all the dotnet tracing features see (MSLearn - Adding distributed tracing instrumentation)[https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs]


---


## License

MIT License - see LICENSE file for details


---

## Feedback

For issues, feature requests, or contributions, visit:
https://github.com/degero/SimpleOpenTelemetry
