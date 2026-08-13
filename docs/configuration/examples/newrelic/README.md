# SimpleOpenTelemetry Appsettings Configs for NewRelic

This folder contains NewRelic focused appsettings examples for SimpleOpenTelemetry.

## Included files

- `aspnetcore-newrelic-directexport.json`: Base appsettings for ASP.NET Core apps that send telemetry directly to NewRelic.
- `aspnetcore-newrelic-otelcollector.json`: Base appsettings for ASP.NET Core apps that send telemetry via the standard OpenTelemetry collector sidecar.

## New Relic endpoints

For EU users use `otlp.eu01.nr-data.net:4317` rather than the endpoint given in file `aspnetcore-newrelic-directexport.json`. If using with unknown network constraints change to http/protobuf on the host 443.

## How to use

With the [example-apps/localdev/aspnetcore/](../../example-apps/localdev/aspnetcore/) app:

1. Copy the contents (except for OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES) of `aspnetcore-newrelic-directexport.json` file into the example app `appsettings.Development.json`
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. Get your NewRelic api key from the [NewRelic website](https://one.newrelic.com/admin-portal/api-keys/home)
1. Set a dotnet user-secret with the command in the example app folder:

```powershell
dotnet user-secrets set "OTEL_EXPORTER_OTLP_HEADERS" "api-key=<newreliceapikey>"
```

1. Run the app via vscode debugger or `dotnet run`

OR

With an existing / new aspnetcore app:

1. Copy content from the one of the config files into your app as `appsettings.Development.json` or `appsettings.Production.json`.
1. Update values such as `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, namespace, version, environment etc.
1. Ensure the required packages below are installed in your app.
1. look at the [component-snippets](../component-snippets/) to add any other instrumentations, resource detectors etc for your hosted env and add their relevant packages
1. Add `using SimpleOpenTelemetry.Extensions; builder.AddSimpleOpenTelemetry();` on your WebApplicationBuilder before the builder.Build();
1. Run the app and verify telemetry on the NewRelic website

## Required package install commands

**IMPORTANT**: ⚠️ **Ensure you install [these versions](../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

If you don't need aspnetcore or httpclient metrics / traces, remove from your SimpleOpenTelemetry config and omit those packages below.

Run these in your app project folder:

```
dotnet add package SimpleOpenTelemetry
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x
dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x

```

## When using an OpenTelemetry collector sidecar

You can check the following guides to configure an open telemetry sidecar with a local docker compose or on your

https://docs.newrelic.com/docs/opentelemetry/get-started/collector-processing/opentelemetry-collector-processing-intro/

https://github.com/newrelic/newrelic-opentelemetry-examples/tree/main/other-examples/collector/nr-config

## Checking telemetry on NewRelic

Viewing traces and logs are straightfoward on the site however here are some sample dashboard queries for
the sample apps httpclient / aspnetcore metrics

httpclient:

```
SELECT average(`http.client.open_connections`)
FROM Metric
TIMESERIES FACET service.name
```

aspnetcore:

```
SELECT average(`aspnetcore.memory_pool.allocated`)
FROM Metric
TIMESERIES FACET service.name
```

## Configuration notes

Other trace/metric instrumentations that may be useful in the this hosting scenario for deeper metrics:

- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Instrumentation.Process

See the [SimpleOpenTelemetry README.md](../../README.md#process-runtime) to set these up

A custom meter for System.Net.NameResolution is included as an example and can be removed if this metric is unneeded
