# Extensions Configuration

⚠️ **IMPORTANT** **It is recommended you install [package versions tested against SimpleOpenTelemetry](../otel-component-versions.md) referenced below.** ⚠️

<br/>

Set Extensions in the the configuration:

```json
 "SimpleOpenTelemetry": {
    "BuilderExtensions": []
 }
```

Extensions offer the ability to extend the OpenTelemetry SDK beyond the core spec where it does not fall into the key component categories.

Available extensions are:

- [AWSXrayTraceId](#aws-x-ray-trace-id-generator)
- [AzureMonitorExporter](#azure-monitor-exporter)

<br/>

## Azure Monitor Exporter

**Package Stability**: Stable

**Signals supported**: All

**Documentation**: [Azure Monitor Exporter client library for .NET - Add the Exporter for all signals](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter#add-the-exporter-for-all-signals)

**Options**: required (connection string at miniumum), [snippets/extensions/azuremonitorexporter.json](./snippets/extensions/azuremonitorexporter.json) and
[AzureMonitorExporterOptions.cs](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/AzureMonitorExporterOptions.cs)

**Notes**: This is the same underlying exporter as [Azure Monitor exporter](exporters.md#azure-monitor-exporter) with one crucial difference: supporting Live Metrics (on by default, only configurable using this extension). Live metrics will only work with a Generic host application and will not work with SimpleOpenTelemetryBootstrap.Add(). It also simplifies your config if you want exports for all signals with all the same settings. For the full features set look at distro [Azure Montor AspNetCore](distros.md#azure-monitor-aspnetcore)

**Nuget Package**:
`dotnet add package Azure.Monitor.OpenTelemetry.Exporter --version x.x.x`

SimpleOpenTelemetry:BuilderExtensions[] json:

```json
{ "Type": "AzureMonitorExporter", "Options": {...} }
```

## AWS X-Ray Trace ID Generator

**Package Stability**: Stable

**Signals supported**: Trace

**Documentation**: [Tracing with AWS Distro for OpenTelemetry .Net SDK](https://github.com/open-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)

**Options**: none

**Notes**: This is commonly used with the AWS Xray Propagator as mentioned in README.md above.

**Nuget Package**:
`dotnet add package OpenTelemetry.Extensions.AWS  --version x.x.x`

SimpleOpenTelemetry:Trace.Extensions[] json:

```json
"awsxraytraceid"
```

---
