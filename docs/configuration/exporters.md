# Exporters Configuration

⚠️ **IMPORTANT** **It is recommended you install [package versions tested against SimpleOpenTelemetry](../otel-component-versions.md) referenced below.** ⚠️

<br/>

Set exporters in the configuration sections:

```json
 "SimpleOpenTelemetry": {
    "Trace": {
      "Exporters": [],
    },
    "Metric": {
      "Exporters": [],
    },
    "Log": {
      "Exporters": [],
    },
    "ExporterOptions": {}
}
```

Both the OpenTelemetry SDK exporters (otlp, console, prometheus) and other contrib / vendor exporters are supported. Each array item can have an 'options' key to specify any settings particular to that exporter.

You can set exporter options for all signals in `SimpleOpenTelemetry:ExporterOptions:exportername` or under `SimpleOpenTelemetry:[Metric/Trace/Log]:Exporters` array item `options` field. Setting them in the latter overrides an 'all signal' option.

For examples listing all possible options (in their current default) see the [snippets/exporter folder](./snippets/exporter/)

Available exporters are:

- [azuremonitor](#azure-monitor-exporter)
- [console](#console-exporter)
- [otlp](#otlp-exporter)
- [prometheusaspnetcore](#prometheus-aspnetcore-exporter)
- [prometheushttplistener](#prometheus-httplistener-exporter)

<br/>

## OTLP exporter

**Signals supported**: trace, metric, log

**Package Stability**: Stable

**Options**: optional, see [snippets/exporter/otlp.json](./snippets/exporter/otlp.json) and [OtlpExporterOptions.cs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/OtlpExporterOptions.cs)

**Nuget Package**: none (builtin to OpenTelemetry .net lib)

SimpleOpenTelemetry:Log/Trace/Metric:Exporters[] json:

```json
{ "type": "otlp", "options": { ... } }
```

**Documentation**: [OpenTelemetry OTLP Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)

**Notes**: All OpenTelemetry SDK OTEL\_ environment variables or (root) settings json values will be used to send to OTLP endpoints for entries don't have options defined.

There are unsupported configuration options such as HttpFactory. If you wish to utilise these, the exporter will need to be configured by code.

## Console Exporter

**Signals supported**: trace, metric, log

**Package Stability**: Stable (for dev purposes only)

**Options**: none (unsupported, see documentation for supported OTEL environment variables/json config)

**Nuget Package**: `dotnet add package OpenTelemetry.Exporter.Console --version x.x.x`

SimpleOpenTelemetry:Log/Trace/Metric:Exporters[] json:

```json
{ "type": "Console" }
```

**Documentation**: [OpenTelemetry Console Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md)

## Prometheus HttpListener Exporter

**Signals supported**: metric

**Package Stability**: Stable (for dev purposes only)

**Options**: optional, see [snippets/exporter/prometheushttplistener.json](./snippets/exporter/prometheushttplistener.json) and [PrometheusHttpListenerOptions.cs](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/PrometheusHttpListenerOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Exporter.Prometheus.HttpListener --version x.x.x`

SimpleOpenTelemetry:Metric:Exporters[] json:

```json
{ "type": "prometheushttplistener", "options": {...} }
```

**Documentation**: [OpenTelemetry Prometheus HttpListener Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.HttpListener/README.md)

**Notes**: This is only for dev use. It is never intended for prod. Defaults to host prometheus scrape endpoint on http://localhost:9464/metrics. Not recommended for aspnetcore apps, instead use [Prometheus AspNetCore Exporter](#prometheus-aspnetcore-exporter-prerelease)

## Prometheus AspNetCore Exporter

**Signals supported**: metric

**Package Stability**: Beta (as of july 2026)

**Options**: optional, see [snippets/exporter/prometheusaspnetcore.json](./snippets/exporter/prometheusaspnetcore.json) and [PrometheusAspNetCoreOptions.cs)](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/PrometheusAspNetCoreOptions.cs)

**Nuget Package**: `dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore --version x.x.x`

SimpleOpenTelemetry:Metric:Exporters[] json:

```json
{ "type": "prometheusaspnetcore", "options": {...} }
```

**Documentation**: [OpenTelemetry Prometheus AspNetCore Exporter README.md](https://github.com/open-telemetry/OpenTelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore/README.md)

**Notes**: For AspNetCore apps only. Hosts prometheus scrape endpoint defaulted on http://apphost:port/metrics.

Additional setup needed:

```csharp
Program.cs

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

```

## Azure Monitor exporter

**Signals supported**: trace, metric, log

**Package Stability**: Stable

**Options**: required (if not defined in top level SimpleOpenTelemetry:ExporterOptions:Azure:ConnectionString), see [snippets/exporter/azuremonitor.json](./snippets/exporter/azuremonitor.json) and
[AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)

**Nuget Package**: `dotnet add package Azure.Monitor.OpenTelemetry.Exporter --version x.x.x`, (if using RBAC to connect to app insights)`dotnet add package Azure.Identity`

SimpleOpenTelemetry:Log/Trace/Metric:Exporters[] json:

```json
{ "type": "AzureMonitor", "options": {...} }
```

**Documentation**: [Azure Monitor Exporter client library for .NET README.md](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/README.md)

**Notes**:

There's a lot of transformation to squeeze OTLP data into Azure Monitor's data structures. eg customMetrics has a '_APPRESOURCEPREVIEW_' entry with otel resource attributes. If you want to store the 'pure' OTLP data look at using an OTLP exporter.

This exporter does not support Live Metrics, for this, use the distro if using AspNet Core or the [AzureMonitorExporter Extension](extensions.md#azure-monitor-exporter). Also if you want all signals exported all with the same settings it is simpler to use this extension. This utilizes most but not all of the [Azure Monitor AspNet Core Distro](distros.md#azure-monitor-aspnetcore) features.

RBAC access via the 'Credential' option is supported. See the example-config. You can set sampling options (it has builtin sampler setup, different to OTEL*TRACES_SAMPLER*\* settings), and more in the options.

You can confirm your telemetry data is flowing with KQL:

```KQL
union requests, dependencies, traces, exceptions, customMetrics
| where timestamp > ago(5m)
| where sdkVersion contains "otel"
| summarize count() by sdkVersion, itemType
| order by itemType
```

---
