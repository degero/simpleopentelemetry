# Extensions Configuration

Set Extensions in the configuration `SimpleOpenTelemetry:BuilderExtensions[]` json array.

Extensions offer the ability to extend the OpenTelemetry SDK beyond the core spec where it does not fall into the key component categories.

Available extensions are:


## Azure Monitor Exporter

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

For supported configurable options see [snippets/extensions/azuremonitorexporter.json](./snippets/extensions/azuremonitorexporter.json)



## AWS X-Ray Trace ID Generator

Stability: Stable

Signal: Trace

Documentation: [Tracing with AWS Distro for OpenTelemetry .Net SDK](https://github.com/ope  n-telemetry/OpenTelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Extensions.AWS/README.md)

Notes: This is commonly used with the AWS Xray Propagator as mentioned in README.md above.

Nuget Package:
`dotnet add package OpenTelemetry.Extensions.AWS`

SimpleOpenTelemetry:Trace.Extensions[] json:

 ```json
 "awsxraytraceid"
 ```


---

