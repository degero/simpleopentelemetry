# SimpleOpenTelemetry

A lightweight, .Net Generic Host / Web Core Host (I) .NET library for simplified OpenTelemetry integration. Abstracts the complexity of OpenTelemetry configuration through supporting multiple exporters, easy metrics/tracing instrumentation for many platforms and logging settings. This is not autoinstrumentation, but a low-code alternative to OpenTelemetryBuilder with some added configuration features.

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

Under the config section "SimpleOpenTelemtry__Exporters__[Metrics/Tracing/Logging]" you can add array items to register exporting of these sets. It supports both the OpenTelemetry SDK exporters (otlp, console, inmemory TODO add prometheus)Each array can have an 'options' key to specify any settings for that exporter. Note keys under the 'options' object are case insensitive.  

If a Vendor  exporter does have mandatory options and you have not specified them either for all data sets in "SimpleOpenTelemtry__ExporterOptions__[VendorExporterName]" or under each array item under "options"

#### OTLP exporters

All OpenTelemetry SDK OTEL_ env vars or (root) settings json values will be used to send to standard OTLP endpoints for any array item of just `{ "type": "otlp" }`

OR (if you want to export to multiple OTLP endpoints / have full configuration options) 

Specify your options within `{ "type": "otlp", "options": {} }`. For a full list of available optons see [OtlpExporterOptions.cs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs)  


#### Vendor distro exporters

Below are the tested Vendor exporters you can add that support all telemetry sets. Add a [new issue here](https://github.com/degero/simpleopentelemetry/issues/new) if there are any others you wish to use. 

> 
> **Azure**
> 
> NOTE: this only utilizes the base azure exporter and does not support the AspNetCore package  
> 
> dotnet add package Azure.Monitor.OpenTelemetry.Exporter
> 
>  ```json
>  { "type": "azure", "options": {} }
>  ```
> 
>  For options see [AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)  
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
