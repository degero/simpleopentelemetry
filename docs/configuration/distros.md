# Distribution Configuration

⚠️ **IMPORTANT** **It is recommended you install [package versions tested against SimpleOpenTelemetry](../otel-component-versions.md) referenced below.** ⚠️

<br/>

Set a Distribution and it's options in the configuration:

```json
 "SimpleOpenTelemetry": {
    "Distro": "",
    "DistroOptions": {}
 }
```

A distribution in terms of OpenTelemetry is _'... a customized version of an OpenTelemetry component...'_.

In the case of SimpleOpenTelemetry, it is a library that will set up all signal collection and exporting settings for you with only a few minor settings you can set in "DistroOptions": {}. The OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES settings/env vars should be set also.

⚠️ _Any other SimpleOpenTelemetry configuration will also be added after the distro is loaded. Ensure you carefully read what the distro is setting up before adding any other SimpleOpenTelemetry configuration or OpenTelemetry 'OTEL\_' settings._ ⚠️

For examples listing all possible options (in their current default) see the [snippets/distro folder](./snippets/distro/)

For a list of supported distros see [DistroEnum.cs](../../src/SimpleOpenTelemetry/OtelComponents/Distro/DistroEnum.cs)

For a list of all OpenTelemetry distros see [OpenTelemetry - Third-party distributions](https://opentelemetry.io/ecosystem/distributions/)

Available distros are:

- [AzureMonitorAspNetCore](#azure-monitor-aspnetcore)

<br/>

## Azure Monitor AspNetCore

**IMPORTANT**: ⚠️ _This Distro only supports use with generic host WebApplication (does not support using with SimpleOpenTelemetryBootstrap.Add())._ ⚠️

**Nuget Package**:
`dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore --version x.x.x`
`dotnet add package Azure.Identity` (if using RBAC to connect to app insights)

SimpleOpenTelemetry:Distro json:

```json
"AzureMonitorAspNetCore"
```

This Distro sets up all signal collection and exporting to Azure monitor. It also sets up several types of instrumentation, resource detectors, offline storage, live metrics, sampling and more. Normally you will not need to add anything in the other configuration areas of SimpleOpenTelemetry save for custom meters or trace sources.

**Options**: required, (ConnectionString at minimum) see [snippets/distro/azuremonitoraspnetcore.json](./snippets/distro/azuremonitoraspnetcore.json) and [AzureMonitorOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/src/AzureMonitorOptions.cs). For a full configuration file see [examples/azure/aspnetcore-azureotel-distro-rbac.json](./examples/azure/aspnetcore-azureotel-distro-rbac.json)

If you wish to setup for Azure Monitor in a Standalone app, configure to use the exporter [Azure Monitor](exporters.md#azure-monitor-exporter) or for all signals, the extension: [Azure Monitor Exporter](extensions.md#azure-monitor-exporter). Note some features of the distro wont be included, see 'Why should I use the Azure Monitor OpenTelemetry Distro?' link below.

If you want more control over your setup you can still use most (not all) features provided in the Distro (see the link below) via the other configuration item covered in the following sections. NOTE: Azure RBAC auth is not currently supported.

**Documentation**:
[GitHub Azure SDK - Azure.Monitor.OpenTelemetry.AspNetCore](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.AspNetCore/README.md)
[MSLearn - Enable Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)
[MSLearn - Why should I use the Azure Monitor OpenTelemetry Distro?](https://learn.microsoft.com/en-us/azure/azure-monitor/app/application-insights-faq#why-should-i-use-the-azure-monitor-opentelemetry-distro)

**Configuration**:

You must specify an Application Insights connection string, or use RBAC (by adding the 'Credential' field in DistroOptions). You can set the ConnectionString via: 'SimpleOpenTelemetry:DistroOptions:ConnectionString'.
It is recommended to set as using 'dotnet user-secrets' or as a secret setting in Azure. [MSLearn - Use OpenTelemetry with Azure Monitor and Application Insights](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-applicationinsights#3-specify-the-connection-string)

SimpleOpenTelemetry:DistroOptions json:

RBAC (only the key is needed in connectionstring, you can use this placeholder and the real key as a secret in hosted envs)

```json
{
  "Credential": "Azure.Identity.DefaultAzureCredential",
  "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000"
}
```

You can confirm your telemetry data is flowing with KQL:

```KQL
union requests, dependencies, traces, exceptions, customMetrics
| where timestamp > ago(5m)
| where sdkVersion contains "otel"
| summarize count() by sdkVersion, itemType
| order by itemType
```

**Notes**:

There's a lot of transformation to squeeze OTLP data into Azure Monitor's data structures. eg customMetrics has a '_APPRESOURCEPREVIEW_' entry with otel resource attributes. If you can sacrifice the benefits of this distro (see 'Why should I use the Azure Monitor OpenTelemetry Distro' above) and want to store the 'pure' OTLP data look at using an OTLP exporter.

This distro provides no option to set Trace sources and only sets up `Azure.*` as a source. If you wish to have custom traces in your app you will need to add them in "SimpleOpenTelemetry:Trace:Sources" or by code. For an example see the [aspnetcore example WithTracing() setup](../../example-apps/localdev/aspnetcore/Program.cs)

If you add a package `OpenTelemetry.Instrumentation.SqlClient` you will need to configure it by code. As the distro will backoff from setting up its own internal sqlclient instrumentation if it detects it.

---
