# SimpleOpenTelemetry Appsettings Configs for Google Cloud Platform

This folder contains Google Cloud Platform focused appsettings examples for SimpleOpenTelemetry.

## Included files

- `aspnetcore-cloudrun-collectorsidecar.json`: Base appsettings for ASP.NET Core apps that send telemetry via a 'Google Built' Google distro for OpenTelemetry collector sidecar. See the [example app README](../../examples-cloud/gcp/cloudrun/README.md) for how to use this config in Google and confiugre the sidecar.

## How to use

See the [full working example](../../examples-cloud/gcp/cloudrun/) with application, permissions, collector config (including for local docker use) and infrastructure. This also covers google observability constraints and quirks from a normal standards.

OR

with an existing / new aspnetcore app:

1. Copy the above file into your app as `appsettings.Development.json` or `appsettings.Production.json`.
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. Update values such as `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, namespace, version, environment etc.
1. Ensure the required packages below are installed in your app.
1. Ensure Google project with your or a cloud run service account with permissions to Google Observability endpoints are setup
1. If running locally, ensure you are logged in with google cloud cli
1. Add `using SimpleOpenTelemetry.Extensions; builder.AddSimpleOpenTelemetry();` on your WebApplicationBuilder before the builder.Build();


## Required package install commands

If you don't need aspnetcore or httpclient metrics / traces, remove from your SimpleOpenTelemetry config and omit those packages below.

Run these in your app project folder:

```
dotnet add package SimpleOpenTelemetry
dotnet add package Google.Apis.Auth
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Resources.Gcp
```


## Configuration notes

Other trace/metric instrumentations that may be useful in the this hosting scenario for deeper metrics:

- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Instrumentation.Process

See the [SimpleOpenTelemetry README.md](../../README.md#process-runtime) to set these up

A custom meter for System.Net.NameResolution is included as an example and can be removed if this metric is unneeded
