# SimpleOpenTelemetry Appsettings Configs for newrelic

This folder contains newrelic focused appsettings examples for SimpleOpenTelemetry.

## Included files

- `aspnetcore-newrelic-directexport.json`: Base appsettings for ASP.NET Core apps that send telemetry directly to newrelic.
- `aspnetcore-newrelic-otelcollector.json`: Base appsettings for ASP.NET Core apps that send telemetry via the standard OpenTelemetry collector sidecar.

## New Relic endpoints

For EU users use `otlp.eu01.nr-data.net:4317` rather than the endpoint given in file `aspnetcore-newrelic-directexport.json`. If using with unknown network constraints change to http/protobuf on the host 443.

## How to use

With the [examples/aspnetcore/](../../examples/aspnetcore/) app:

1. Copy the contents (except for OTEL_SERVICE_NAME, OTEL_RESOURCE_ATTRIBUTES) of `aspnetcore-newrelic-directexport.json` file into the example app `appsettings.Development.json`
1. Get your newrelic api key from the [newrelic website](https://one.newrelic.com/admin-portal/api-keys/home)
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
1. Run the app and verify telemetry on the newrelic website

## Required package install commands

If you don't need aspnetcore or httpclient metrics / traces, remove from your SimpleOpenTelemetry config and omit those packages below.

Run these in your app project folder:

```
dotnet add package SimpleOpenTelemetry
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http

```

## When using an opentelemetry collector sidecar

You can check the following guides to configure an open telemetry sidecar with a local docker compose or on your

https://docs.newrelic.com/docs/opentelemetry/get-started/collector-processing/opentelemetry-collector-processing-intro/

https://github.com/newrelic/newrelic-opentelemetry-examples/tree/main/other-examples/collector/nr-config


## Checking telemetry on newrelic

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