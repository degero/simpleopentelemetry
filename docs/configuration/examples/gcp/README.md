# SimpleOpenTelemetry Appsettings Configs for Google Cloud Platform

This folder contains Google Cloud Platform focused appsettings examples for SimpleOpenTelemetry.

## Included files

- `aspnetcore-cloudrun-collectorsidecar.json`: Base appsettings for ASP.NET Core apps that send telemetry via a 'Google Built' Google distro for OpenTelemetry collector sidecar.

## How to use

See the [example app README](../../../../example-apps/cloud/gcp/cloudrun/README.md) for how to use this config in Google and configure the sidecar. This includes application, permissions, collector config (including for local docker use) and infrastructure. This also covers google observability constraints and quirks from a normal standards. There is configuration, quirks and production use tips in this example apps [otel-collector-config/README.md](../../../../example-apps/cloud/gcp/cloudrun/otel-collector-config/README.md)

OR

with an existing / new aspnetcore app:

1. Copy the above file into your app as `appsettings.Development.json` or whichever environment you are using.
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. Update values such as `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, namespace, version, environment etc.
1. Ensure the required packages below are installed in your app.
1. Ensure Google project with your or a cloud run service account with permissions to Google Observability endpoints are setup
1. If running locally, ensure you are logged in with google cloud cli
1. Add `using SimpleOpenTelemetry.Extensions; builder.AddSimpleOpenTelemetry();` on your WebApplicationBuilder before the builder.Build();
1. Run the app locally or deploy a docker image and launch cloudrun instance
1. Verify your telemetry in [GCP console](https://console.cloud.google.com/)

## Required package install commands

**IMPORTANT**: ⚠️ **It is recommended you install [these versions tested against SimpleOpenTelemetry](../../../otel-component-versions.md) of packages referenced below.** ⚠️

<br/>

If you don't need aspnetcore or httpclient metrics / traces, remove from your SimpleOpenTelemetry config and omit those packages below.

Run these in your app project folder:

```powershell
dotnet add package SimpleOpenTelemetry
dotnet add package Google.Apis.Auth
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x
dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x
dotnet add package OpenTelemetry.Resources.Gcp --version x.x.x
```

## Configuration notes

Other trace/metric instrumentations that may be useful in the this hosting scenario for deeper metrics:

- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Instrumentation.Process

See the [SimpleOpenTelemetry README.md](../../README.md#process-runtime) to set these up

A custom meter for System.Net.NameResolution is included as an example and can be removed if this metric is unneeded
