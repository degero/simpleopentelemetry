# SimpleOpenTelemetry

A lightweight, low-friction .NET library providing a simple, low code option to setup OpenTelemetry on .NET applications via IConfiguration.

---

## Overview

**Supported Frameworks:** .NET 8.0, .NET 10.0
**Supported .Net App Patterns:** .Net Generic Host / Web Core Host / Non generic host.  
**License:** MIT


SimpleOpenTelemetry handles the boilerplate configuration of manual OpenTelemetry integration, it is not in any way related to autoinstrumentation/zero-code setup and is designed to streamline setup removing any need for code based configuration for most common non-complex configurations. If thare are complex items required for you app, you can add them by code after the SimpleOpenTelemetry configurator runs. 

Support is available for enabling distros and popular component implementations: exporters, instrumentations, propagators, resource detectors, extensions and samplers. It focuses on allowing easy setup for AWS, Azure and GCP (limited as of April 2026). By adding the related nupkg to your project and adding configuration components are registered with OpenTelemetry Builder providers using reflection. It is designed to compliment OpenTelemetry's defaults and OTEL_* env var settings.


---

## Features

- Pluggable components by adding config entry and NuGet package to your app for telemetry features you need. 
- Example configuration files for common app / platform scenarios [example-configs](./example-configs/)
- Set telemetry attribute 'service.version' based on app assembly version when using builtin ResourceDetector 'AssemblyVersion' (see [Resource Detectors > Builtin](#builtin)). Overriden by setting 'service.version' in OTEL_RESOURCE_ATTRIBUTES of appsettings.json / env var
- TODO add the other features

---

## Getting Started

- Add the SimpleOpenTelemetry nupkg: `dotnet add package --prerelease SimpleOpenTelemetry`
- Add a "SimpleOpenTelemetry": {} root section to your appsettings.{environment}.json and read the next sections to setup.
- Add boostrapping code:
  - For Generic Host apps like aspnetcore (or any apps using WebApplicationBuilder/HostApplicationBuilder):
    - In your startup code (eg Program.cs) add `using SimpleOpenTelemetry.Extensions;` and before builder.build() add `builder.AddSimpleOpenTelemetry();`
    - Optionally, to validate OpenTelemetry have the key app identifiers set, run `app.Services.SimpleOpenTelemetryValidate();` after `var app = builder.Build();`.
  - For Standalone apps (no Generc host): 
    - In your startup code file add `using Microsoft.Extensions.Logging; using SimpleOpenTelemetry;` and   
    ```csharp
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddOpenTelemetry();
        // You may want to set log levels using builder.AddConfiguration()
    });

    var sdk = StandaloneApp.AddSimpleOpenTelemetry(config);
    ```

---


## Examples

If you are in TLDR; mode to bother with the next sections, take a look at the [example applications](./examples/) and [example configs](./example-configs/).


---


## OpenTelemetry hosting lifecycle

// TODO add information about needing build / run on the IHostedWebApplication / IHostedApplication

// TODO add information on controlling the life cycle in a non-generic host


---

## Configuration

Key configurable components:  

- Distributions
- Trace/Metric/Log Exporters - including allowing multiple exporters for each signal type 
- Trace/Metric Instrumentation - for key .net components (aspnetcore, runtime...) and vendor libraries (Azure, AWS...)
- Trace/Metric/Log Extensions -  eg AddAWSXRayTraceId()
- Custom meters
- Trace sources
- Resource detectors
- Samplers  


While all OpenTelemetery components in [OpenTelemetry-dotnet-contrib](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib) distros, and vendor implementations of components *could* be loaded using SimpleOpenTelemetry's configuration syntax, these are gated through registered assembly sets in the below folders to ensure those configurations have been tested in this repo:

- [Exporters](./src/SimpleOpenTelemetry/Exporter/ExporterAssemblies.cs)
- [Trace / Metric Instrumentations](./src/SimpleOpenTelemetry/Instrumentation/InstrumentationAssemblies.cs)
- [Extensions](./src/SimpleOpenTelemetry/Extensions/ExtensionAssemblies.cs)
- [Samplers](./src/SimpleOpenTelemetry/Sampler/SamplerAssemblies.cs)  
- [Propagators](./src/SimpleOpenTelemetry/Propagator/PropagatorAssemblies.cs)  

If there is one you would like added, feel free to fork and raise a PR, or [raise an issue](https://github.com/degero/simpleopentelemetry/issues/new).


---


#### Configuration file setup

**IMPORTANT**: SimpleOpenTelemetry throws an exception if no configuration is found

To get setarted, add a "SimpleOpenTelemetry" section to the root of your appsettings.json / appsettings.{Environment}.json file in your project folder. SimpleOpenTelemetry will set up all the components with OpenTelemetry for your application. If this is not set it will not run AddOpenTelemetry() with your application.  

Similarly for the subsections "Metric/Trace/Log", OpenTelemetry's WithLogging/Tracing/Metrics() extension methods will only run (and  subsequent exports etc) when the corresponding section exists. If at least on is not set it will not run AddOpenTelemetry() with your application.

For a json configuration file, you can start with one of the pre-built ones in [](./example-configs/) or setup the top level config items and follow the next secitons covering the items you can add:  


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
      "Detectors": ["envvar", "assebmlyversion"],
    },
    "ResourceDetectorConfig": {},
    "Propagators": [],
    "Sampler": ""
}
```  


---


#### Configuration sources

As SimpleOpenTelemetry uses donet's IConfiguration concepts and abstractions, it relies on the default configuration sources setup in generic host platforms to load in appsettings.json. This also means you can also [add in other configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) before calling AddSimpleOpenTelemetry(). These are particularly useful for sensitive values (keys, secrets etc).  

As the IConfigurationProvider for environment variables is enabled by default, you can define or override the json file "SimpleOpenTelemetry" settings.

For local development with sensitive values, it is recommended to take advantage of [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).  


#### Environment variables

The OTEL_* environment variables / json config are supported and load in by default but for many components those settings can be defined explicilty for their signal type/functionality in the configuration file.

Some useful OTEL_ environment variables you can make use of (* indicates a core recommended setting to set):  

- [*OTEL_SERVICE_NAME](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)
- [*OTEL_RESOURCE_ATTRIBUTES](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#general-sdk-configuration)
- [OTEL_METRICS_EXEMPLAR_FILTER](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/#exemplar)
- [OTEL_EXPORTER_OTLP_...](https://opentelemetry.io/docs/specs/otel/protocol/exporter/)


>*Important*
>
> The OpenTelemetry Docuementation [SDK Environment Variables](https://OpenTelemetry.io/docs/specs/otel/configuration/sdk-environment-variables) page is a specification not a reference for the dotnet implementation. Many are (as of april 2026) unsupported such as **OTEL_PROPAGATORS, OTEL_TRACES_EXPORTER, OTEL_LOGS_EXPORTER, OTEL_METRICS_EXPORTER**
> 
> If you wish to make use of any of the environment variables in the spec but not above, check the [dotnet documentation to confirm it is implemented](https://OpenTelemetry.io/docs/languages/dotnet/getting-started/), or even quicker too dive into the [OpenTelemetry-dotnet repo](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main) to search.
>


#### When something you need isn't configurable

If a component type (eg. Processors), extension or setting isnt available to configure you can load/configure it in code using the OpenTelemetryBuilder returned from AddSimpleOpenTelemetry().

There is also a registry of libraries or custom components/extensions you can add doing the above, 
check the [OpenTelemetry Registry](https://OpenTelemetry.io/ecosystem/registry/).   

If what you need isn't available, you can build your own following the OpenTelemetry guidelines for [traces](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/trace/extending-the-sdk/README.md) [logs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/logs/extending-the-sdk/README.md) and [metrics](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/docs/metrics/extending-the-sdk/README.md).  


---


## Configuration

The next sections cover setting up the subsections of your "SimpleOpenTelemetry" config


---


### Distribution

A distribution in terms of SimpleOpenTelemetry configuration is a library that will set up all signal collection and exporting settings for you with only a few minor settings such as exporter endpoints. By setting a distribution in your configuration, *all other configuration areas will be ignored*. This means any of the features listed previously will not be available as to not interfere with the distro

For a list of all OpenTelemetry distros see [OpenTelemtry - Third-party distributions](
https://opentelemetry.io/ecosystem/distributions/)


#### Azure Monitor AspNetCore

This sets up all signal collection and exporting to Azure monitor. It also sets up several types of instrumentation, resource detectors and more. If you want more control over your setup you can still use most (not all) features provided in the distr (see the link below) via the other configuration item covered in the following sections. NOTE: Azure RBAC auth is not currently supported.

Documentation:  
[Gihtub Azure SDK - Azure.Monitor.OpenTelemetry.AspNetCore](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/README.md)  
[MSLearn - Enable Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)  
[MSLearn - Why should I use the Azure Monitor OpenTelemetry Distro?](https://learn.microsoft.com/en-us/azure/azure-monitor/app/application-insights-faq#why-should-i-use-the-azure-monitor-opentelemetry-distro)


Nuget Package:
`dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore`   

SimpleOpenTelemtry::Distro json:  

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


IncludeFormattedMessage - bool (default: false)
IncludeScopes - bool (default: false)
ParseStateValues - bool (default: false)

[View OpenTelemetryLoggerOptions.cs for settings details](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Logs/ILogger/OpenTelemetryLoggerOptions.cs)


---


### Metrics  

#### Settings

The following are supported to switch on opentelemetry dotnet SDK settings via "SimpleOpenTelemetry:Metric:Settings":

MetricLimit - int (default: 1000)

Documentation related to thes settings [opentelemetry.io metrics best practices](https://opentelemetry.io/docs/languages/dotnet/metrics/best-practices)


---


### Tracing  

#### Settings

The following are supported to switch on opentelemetry dotnet SDK settings via "SimpleOpenTelemetry:Trace:Settings":

- SetErrorStatusOnException - bool (default: false)

Documentation related to this setting [opentelemetry.io traces reporting exceptions](https://opentelemetry.io/docs/languages/dotnet/traces/reporting-exceptions/)


---


### Instrumentation  


---


### Exporters  

These can be set under the config section "SimpleOpenTelemetry::[Metrics/Tracing/Logging]::Exporters". It supports both the OpenTelemetry SDK exporters (otlp, console,prometheus) and other contrib / vendor exporters. Each array item can have an 'options' key to specify any settings particular to that exporter.  

You can set exporter options for all signals in "SimpleOpenTelemetry::ExporterOptions::[exportername]" or under "SimpleOpenTelemetry::[Metrics/Tracing/Logging]::Exporters" array item "options" field.  Setting them here overrides an 'all signal' option

For a full list of all the supported exporters see [TraceExporterEnum / MetricExporterEnum / LogExporterEnum](./src/SimpleOpenTelemetry/Exporter/ExporterAssemblies.cs)

#### OpenTelemetry SDK OTLP exporter

Documentation:  [OpenTelemetry OTLP Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)  

Signals supported: all  

Options: optional 

Notes: All OpenTelemetry SDK OTEL_ environment variables or (root) settings json values will be used to send to OTLP endpoints for entries dont have options defined  

Nuget Package: none (builtin to OpenTelemetry .net lib)

SimpleOpenTelemetry::<SignalType>::Exporters[] json:
```json
{ "type": "otlp" }
```  

If you want to export to multiple OTLP endpoints / have full configuration options add the below, for field names/values see [OtlpExporterOptions.cs)](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs))

SimpleOpenTelemetry::<SignalType>::Exporters[] json:
```json
{ "type": "otlp", "options": {...} }
```
  
---


#### OpenTelemetry SDK Console Exporter (only for Development purposes)

Documentation: [OpenTelemetry Console Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md)

Signals supported: all  

Options: none (unsupported, see above readme for supported OTEL_* environment variables/json config)

Nuget Package: none

SimpleOpenTelemetry::<SignalType>::Exporters[] json:

```json
{ "type": "otlp" }
```


---


#### OpenTelemetry SDK Prometheus HttpListener Exporter (prerelease - only for Development purposes)


Documentation: [OpenTelemetry Prometheus HttpListener Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/README.md)

Signals supported: metrics  

Options: optional (see [PrometheusHttpListenerOptions.cs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/PrometheusHttpListenerOptions.cs))

Notes: Defaults to host prometheus scrape endpoint on http://localhost:9464/metrics.  

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Exporter.Prometheus.HttpListener`  

SimpleOpenTelemetry::Metric::Exporters[] json:

```json
{ "type": "prometheushttplistener", "options": {...} }
```


---


#### OpenTelemetry SDK Prometheus AspNetCore Exporter (prerelease)


Signals supported: metrics  

Options: [OpenTelemetry Prometheus AspNetCore Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md) the documentation doesn't seem to mention but you can set anything defined in 'PrometheusAspNetCoreOptions.cs' of this project.

Notes: Host prometheus scrape endpoint on aspnetcore WebApplication. Defaults to on http://apphost:port/metrics.  

Nuget Package:  
`dotnet add package --prerelease OpenTelemetry.Exporter.Prometheus.AspNetCore`  

SimpleOpenTelemetry::Metric::Exporters[] json:

```json
{ "type": "prometheusaspnetcore", "options": {...} }
```

Additional setup:

```csharp
Program.cs
```
var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();


---


#### Vendor distro exporters

Below are the tested Vendor exporters you can add that support all telemetry signals. 

> 
> **Azure**
> 
> Documentation: [Azure Monitor Exporter client library for .NET README.md](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/README.md) 
>
> Signals supported: all  
> 
> Options: mandatory (if not defined in SimpleOpenTelemetry::ExporterOptions::Azure) 
> [AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)   
> 
> Notes: this only utilizes the base azure exporter and does not support the AspNetCore package or EntraID Auth. You can set sampling options (it has builtin sampler setup, different to OTEL_TRACES_SAMPLER_* settings), livemetrics and more in the options. 
>
> Nuget Package:
> `dotnet add package Azure.Monitor.OpenTelemetry.Exporter`
> 
> SimpleOpenTelemetry::<SignalType>::Exporters[] json:  
> 
>  ```json
>  { "type": "azure", "options": {...} }
>  ```
>

---

### Resource Detectors

Resource detectors are set under SimpleOpenTelemetry::Resource:Detectors[] string array.

All the supported resource detectors are listed here [ResourceDetectorEnum](./src/SimpleOpenTelemetry/Resource/ResourceDetectorEnum.cs)

At a minimum, be sure to add the [OpenTelemetry SDK EnvironmentVariables detector](#opentelemetry-sdk) to ensure OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES env var settings are be loaded in.

#### Builtin

> *AssemblyVersion*
>
> Stability: Stable
>
> Nuget Package: not needed
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "AssemblyVersion"
>  ```
>


#### OpenTelemetry SDK

> *EnvVar*
>
> Stability: Stable
>
> Documentation: [OpenTelemetry SDK ResourceBuilderExtensions.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/08df7481053204a5ba10c61bb4f1a21d5d3fcefa/src/OpenTelemetry/Resources/ResourceBuilderExtensions.cs#L124)
>
> Nuget Package: not needed
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "EnvVar"
>  ```
>

#### OpenTelemetry contrib


> *Host*
>
> Stability (as of april 2026): Beta
>
> Documentation: [Resource Host Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Host/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Resources.Host --prerelease`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "host"
>  ```
>

> *Container*
>
> Stability (as of april 2026): Beta
>
> Documentation: [Container Resource Detector README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Container/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Resources.Container --prerelease`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "container"
>  ```
>

> *Operating System*
>
> Stability (as of april 2026): Alpha
>
> Documentation: [Operating System Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.OperatingSystem/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Resources.OperatingSystem --prerelease`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "os"
>  ```
>

> *Process*
>
> Stability (as of april 2026): Beta
>
> Documentation: [Process Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Process/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Resources.Process --prerelease`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "process"
>  ```
>

> *Process Runtime*
>
> Stability (as of april 2026): Beta
>
> Documentation: [Process Runtime Resource Detectors README.md](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.ProcessRuntime/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Resources.ProcessRuntime --prerelease`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "processruntime"
>  ```
>

#### OpenTelemetry contrib - Cloud platform specific


> *AWS*
>
> Stability: Stable
>
> Documentation: [AWS Resource Detectors](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.AWS/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Resources.AWS`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "aws"
>  ```
>
> (Optional) SimpleOpenTelemetry::ResourceDetectorConfig::AWS json:  
> 
>  ```json
>  { "SemanticConventionVersion": "V1_29_0" }
>  ```
>


> *Azure*
>
> Stability (as April 2026): Beta (as of march 2026)
>
> Documentation: [Resource Detectors for Azure cloud environments](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Azure/README.md)
> 
> Nuget Package:
> `dotnet add package --prerelease OpenTelemetry.Resources.Azure`
>
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "azure"
>  ```
>


> *--UNAVAILABLE-- Google Cloud Platform*
>
> Stability (as April 2026): Development (OpenTelemetry.Resources.Gcp.nupkg unavailable)
>
> Documentation: [Resource Detectors for Google Cloud Platform](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Resources.Gcp/README.md)
>
> Nuget Package:
> `dotnet add package --prerelease OpenTelemetry.Resources.Gcp`
>  
> SimpleOpenTelemetry::Resource:Detectors[] json:  
> 
>  ```json
>  "gcp"
>  ```


---


### Propagators


> *Important*
> The OpenTelemetry SDK env var OTEL_PROPAGATORS is not supported (as of April 2026) in the dotnet implementation


You can add multiple propagators in the SimpleOpenTelemetry::Propagators[] json array. 

**Default**
OpenTelemitry initialisation defaults to use a 'CompositeTextMapPropagator' of BaggagePropagator (spec: 'baggage') and TraceContextPropagator (spec:'tracestate','traceparent'). By setting as Propagators as `null` or `[]` this will use the default.

The equivalant config setting (if you wish to append more to the default) being:

```json
"Propagators": ["tracecontext", "baggage"]
```

**Disable**

If you wish to set as none, explicitly set SimpleOpenTelemetry::Propagators as:

```json
{ "SimpleOpenTelemetry": { "Propagators": [ 'none' ]] } }
```

**Nuget Packages**
You can make use of OpenTelemetry's builtin default [SDK propagators](https://github.com/open-telemetry/OpenTelemetry-dotnet/tree/main/src/OpenTelemetry.Api/Context/Propagation) without adding nupkg. To use the B3 propagator you will need to add the core sdk extensions nupkg: `dotnet add package OpenTelemetry.Extensions.Propagators`  

**Available Propagators**
For a full list of all the supported propagators see [PropagatorEnum](./src/SimpleOpenTelemetry/Propagator/PropagatorAssemblies.cs)


#### OpenTelemetry contrib

> *AWS X-Ray Id Propagator*
>
> Stability: Stable
>
> Documentation: [AWS X-Ray Id Propagator](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Extensions.AWS` 
> 
> SimpleOpenTelemetry::Propagators[] json:  
> 
>  ```json
>  "aws"
>  ```


---


### Samplers


The below allow vendor sampler configuration as an alternative to OpenTelemetry's [built-in samplers](https://OpenTelemetry.io/docs/specs/otel/trace/sdk/#built-in-samplers). Builtin samplers can be set in OTEL_TRACES_SAMPLER of the root json configuration or env var. Some requires values in OTEL_TRACES_SAMPLER_ARG. The sampler defaults to 'parentbased_always_on'.  

For a full list of all the additional supported samplers see [SamplerEnum](./src/SimpleOpenTelemetry/Sampler/SamplerAssemblies.cs)

For Azure users, sampling is built into the exporter setup/options.


#### OpenTelemetry contrib

> *AWS X-Ray Remote Sampler*
>
> Stability (as April 2026): Alpha
>
> Documentation: [AWS X-Ray Remote Sampler](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Sampler.AWS/README.md)
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Sampler.AWS --prerelease`  
>  
> SimpleOpenTelemetry::Samplers[] json:  
> 
>  ```json
>  "aws"
>  ```


---


### Extensions  


Extensions offer (as the name suggests) the ability to extend the OpenTelemetry SDK beyond the core spec where it does not (or sometimes may for some reason with B3 Propogator) fall into the key component categories above.


#### OpenTelemetry contrib


> *AWS X-Ray Remote Sampler*
>
> Stability: Stable
>
> Signal: Trace
>
> Documentation: [AWS X-Ray Remote Sampler](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)
>
> Notes: This is commonly used with the AWS Xray Propogator as mentioned in README.md above.
>
> Nuget Package:
> `dotnet add package OpenTelemetry.Extensions.AWS`  
>  
> SimpleOpenTelemetry::Trace.Extensions[] json:  
> 
>  ```json
>  "awsxraytraceid"
>  ```
>


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
