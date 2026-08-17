# Exporters Configuration

**IMPORTANT**: ⚠️ **Ensure you install [these versions](../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

Set exporters in the configuration `SimpleOpenTelemetry:[Metrics/Tracing/Logging]:Exporters[]` json arrays.

Both the OpenTelemetry SDK exporters (otlp, console, prometheus) and other contrib / vendor exporters are supported. Each array item can have an 'options' key to specify any settings particular to that exporter.

You can set exporter options for all signals in `SimpleOpenTelemetry:ExporterOptions:[exportername]` or under `SimpleOpenTelemetry:[Metrics/Tracing/Logging]:Exporters` array item `options` field. Setting them here overrides an 'all signal' option

For a full list of all the supported exporters see [TraceExporterEnum / MetricExporterEnum / LogExporterEnum](./src/SimpleOpenTelemetry/Exporter/ExporterAssemblies.cs)

For examples listing all possible options (in their current default) see the [snippets/exporter folder](./snippets/exporter/)

Available exporters are:

## OTLP exporter

Signals supported: trace, metric, log

Stability: Stable

Documentation: [OpenTelemetry OTLP Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)

Options: optional

Notes: All OpenTelemetry SDK OTEL\_ environment variables or (root) settings json values will be used to send to OTLP endpoints for entries don't have options defined.

Nuget Package: none (builtin to OpenTelemetry .net lib)

SimpleOpenTelemetry:<SignalType>:Exporters[] json:

```json
{ "type": "otlp", "options": { ... } }
```

For supported configurable options see [snippets/exporter/otlp.json](./snippets/exporter/otlp.json)

There are unsupported configuration options such as HttpFactory. If you wish to utilise these, the exporter will need to be configured by code, see [OtlpExporterOptions.cs)](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs))

## Console Exporter

Signals supported: trace, metric, log

Stability: Stable (for dev purposes only)

Documentation: [OpenTelemetry Console Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md)

Options: none (unsupported, see above readme for supported OTEL\_\* environment variables/json config)

Nuget Package:
`dotnet add package OpenTelemetry.Exporter.Console --version x.x.x`

SimpleOpenTelemetry:<SignalType>:Exporters[] json:

```json
{ "type": "otlp" }
```

## Prometheus HttpListener Exporter

Signals supported: metric

Stability: Stable (for dev purposes only)

Documentation: [OpenTelemetry Prometheus HttpListener Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/README.md)

Options: optional (see [PrometheusHttpListenerOptions.cs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/PrometheusHttpListenerOptions.cs))

Notes: This is only for dev use. It is never intended for prod. Defaults to host prometheus scrape endpoint on http://localhost:9464/metrics. Not recommended for aspnetcore apps, instead use [Prometheus AspNetCore Exporter](#prometheus-aspnetcore-exporter-prerelease)

Nuget Package:
`dotnet add package OpenTelemetry.Exporter.Prometheus.HttpListener --version x.x.x`

SimpleOpenTelemetry:Metric:Exporters[] json:

```json
{ "type": "prometheushttplistener", "options": {...} }
```

For supported configurable options see [snippets/exporter/prometheushttplistener.json](./snippets/exporter/prometheushttplistener.json)

## Prometheus AspNetCore Exporter

Signals supported: metric

Stability: Beta (as of july 2026)

Documentations: [OpenTelemetry Prometheus AspNetCore Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md)

Options: optional, the documentation doesn't appear to mention, but you can set anything defined in 'PrometheusAspNetCoreOptions.cs' of this project.

Notes: For AspNetCore apps only. Hosts prometheus scrape endpoint defaulted on http://apphost:port/metrics.

Nuget Package:
`dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore --version x.x.x`

SimpleOpenTelemetry:Metric:Exporters[] json:

```json
{ "type": "prometheusaspnetcore", "options": {...} }
```

For supported configurable options see [snippets/exporter/prometheusaspnetcore.json](./snippets/exporter/prometheusaspnetcore.json)

Additional setup needed:

```csharp
Program.cs

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

```

## Azure Monitor exporter

Signals supported: trace, metric, log

Stability: Stable

Documentation: [Azure Monitor Exporter client library for .NET README.md](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/README.md)

Options: mandatory (if not defined in top level SimpleOpenTelemetry:ExporterOptions:Azure:ConnectionString)
[AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)

Notes:

There's a lot of transformation to squeeze OTLP data into Azure Monitor's data structures. eg customMetrics has a '_APPRESOURCEPREVIEW_' entry with otel resource attributes. If you want to store the 'pure' OTLP data look at using an OTLP exporter.

This exporter does not support Live Metrics, for this, use the distro if using AspNet Core or the [AzureMonitorExporter Extension](#azure-monitor-exporter-1). Also if you want all signals exported all with the same settings it is simpler to use the extension. This only utilizes most but not all of the [Azure Monitor AspNet Core Distro](#azure-monitor-aspnetcore) features.

RBAC access via the 'Credential' option is supported. See the example-config. You can set sampling options (it has builtin sampler setup, different to OTEL*TRACES_SAMPLER*\* settings), and more in the options.

You can confirm your telemetry data is flowing with KQL:

```KQL
union requests, dependencies, traces, exceptions, customMetrics
| where timestamp > ago(5m)
| where sdkVersion contains "otel"
| summarize count() by sdkVersion, itemType
| order by itemType
```

Nuget Package:
`dotnet add package Azure.Monitor.OpenTelemetry.Exporter --version x.x.x`
`dotnet add package Azure.Identity` (if using RBAC to connect to app insights)

SimpleOpenTelemetry:<SignalType>:Exporters[] json:

```json
{ "type": "AzureMonitor", "options": {...} }
```

For supported configurable options see [snippets/exporter/azuremonitor.json](./snippets/exporter/azuremonitor.json)

---
