# SimpleOpenTelemetry

A lightweight .NET library for simplified OpenTelemetry integration, supports .Net Generic Host / Web Core Host / Non generic host. Abstracts the complexity of OpenTelemetry configuration through supporting multiple exporters, easy metrics/tracing instrumentation for many platforms and logging settings. This is not for autoinstrumentation (and related vendor distros), but a low-code alternative to OpenTelemetryBuilder with some added configuration features.

## Overview

SimpleOpenTelemetry provides a straightforward way to add distributed tracing to .NET applications with minimal setup. It handles the boilerplate configuration of OpenTelemetry and includes built-in support for popular exporters and instrumentation types.

**Supported Frameworks:** .NET 8.0, .NET 10.0
**License:** MIT

---

## Features

- Sets OTEL_RESOURCE_ATTRIBUTES 'service.version' from app assembly version if one is not provided in env var / config

---

## Configuration

While all configuration can be done via json settings, it is possible to specify or override any of these with environment variables (eg for secure loading senstive values). As well as using dotnet user-secrets for local development, information on using it with web and non-web applications can be found on [MSLearn](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0&tabs=windows#user-secrets-in-non-web-applications).

---

### Exporters

Under the config section "SimpleOpenTelemtry::Exporters::[Metrics/Tracing/Logging]" you can add array items to register exporting of these signals. It supports both the OpenTelemetry SDK exporters (otlp, console, inmemory TODO add prometheus)Each array can have an 'options' key to specify any settings for that exporter. Note keys under the 'options' object are case insensitive.  

If a Vendor  exporter does have mandatory options and you have not specified them either for all signals in "SimpleOpenTelemtry::ExporterOptions::[VendorExporterName]" or under each array item under "options"

#### Opentelemetry Console Exporter (only for Development purposes)

Options currently unsupported but [env vars/json config](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md) supported.

Signals supported: all  

Options: none (unsupported)

Nuget Package:
`dotnet add package Azure.Monitor.OpenTelemetry.Exporter`  

SimpleOpenTelemtry::Exporters::<SignalType>[] json:

```json
{ "type": "otlp" }
```

---

#### Opentelemetry Prometheus HttpListener Exporter (prerelease - only for Development purposes)

Defaults to host prometheus scrape endpoint on http://localhost:9464/metrics.  

Signals supported: metrics  

Options: optional [Opentelemetry Prometheus HttpListener Exporter README.md]](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/README.md)  

Nuget Package:
`dotnet add package --prerelease OpenTelemetry.Exporter.Prometheus.HttpListener`  

SimpleOpenTelemtry::Exporters::Metrics[] json:

```json
{ "type": "prometheushttplistener", "options": {...} }
```

---

#### Opentelemetry Prometheus AspNetCore Exporter (prerelease)

Host prometheus scrape endpoint on aspnetcore WebApplication. Defaults to on http://apphost:port/metrics.  

Signals supported: metrics  

Options: [Opentelemetry Prometheus AspNetCore Exporter README.md](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md) the documentation doesn't seem to mention but you can set anything defined in 'PrometheusAspNetCoreOptions.cs' of this project.

Nuget Package:  
`dotnet add package --prerelease OpenTelemetry.Exporter.Prometheus.AspNetCore`  

SimpleOpenTelemtry::Exporters::Metrics[] json:

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

#### Opentelemetry OTLP exporter

All OpenTelemetry SDK OTEL_ env vars or (root) settings json values will be used to send to OTLP endpoints for entries dont have options defined  

Signals supported: all  

Options: optional [Opentelemetry OTLP Exporter README.md](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs)  

Nuget Package: none (builtin to OpenTelemetry .net lib)

SimpleOpenTelemtry::Exporters::<SignalType>[] json:
```json
{ "type": "otlp" }
```  

If you want to export to multiple OTLP endpoints / have full configuration options

SimpleOpenTelemtry::Exporters::<SignalType>[] json:
```json
{ "type": "otlp", "options": {...} }
```
  
---

#### Vendor distro exporters

Below are the tested Vendor exporters you can add that support all telemetry signals. Add a [new issue here](https://github.com/degero/simpleopentelemetry/issues/new) if there are any others you wish to use. 

> 
> **Azure**
> 
> NOTE: this only utilizes the base azure exporter and does not support the AspNetCore package or EntraID Auth
> 
> Signals supported: all  
> 
> Options: mandatory (if not defined in ExporterOptions::Azure) [AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)   
> 
>
> Nuget Package:
> `dotnet add package Azure.Monitor.OpenTelemetry.Exporter`
> 
> SimpleOpenTelemtry::Exporters::<SignalType>[] json:  
> 
>  ```json
>  { "type": "azure", "options": {...} }
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

## Support

For issues, feature requests, or contributions, visit:
https://github.com/degero/SimpleOpenTelemetry
