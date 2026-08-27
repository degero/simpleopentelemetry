# SimpleOpenTelemetry AppSettings Configs for AWS

This folder contains AWS-focused appsettings examples for SimpleOpenTelemetry.

## Included files

- `aspnetcore-ecs-otelcollector.json`: Base appsettings for ASP.NET Core apps that send telemetry via an ADOT (AWS Distro for OpenTelemetry) collector sidecar.

## ADOT collector configs

AWS recommends exporting to the newer OTLP endpoints. See the [adotcollector-ecs-otlpexport.yml](../../../../example-apps/cloud/aws/ecs/adot-collector-config/adotcollector-ecs-otlpexport.yml) example config file for the ADOT collector to use with this config file. There are several methods for telemetry collection in AWS than the collector. For help on choosing which Telemetry collection solution suits your needs see the [Amazon Cloudwatch - Getting started](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPGettingStarted.html).

If using the AWS legacy metrics export from the collector, you will need to query metrics via the 'Classic metrics' area.

## How to use

See the [example app](../../../../example-apps/cloud/aws/ecs/) for how to use this config in AWS and configure the sidecar.

OR

Follow the below:

**IMPORTANT**: ⚠️ **It is recommended you install these [package versions](../../../otel-component-versions.md) used below.** ⚠️

<br/>

Run these in your app project folder:

```powershell
dotnet add package SimpleOpenTelemetry
dotnet add package OpenTelemetry.Instrumentation.AWS --version x.x.x
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version x.x.x
dotnet add package OpenTelemetry.Instrumentation.Http --version x.x.x
dotnet add package OpenTelemetry.Extensions.AWS --version x.x.x
dotnet add package OpenTelemetry.Resources.AWS --version x.x.x
```

1. Copy `aspnetcore-ecs-otelcollector.json` into your app as `appsettings.Development.json` or or whichever environment you are using.
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. Update values such as `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, region, and endpoint settings for your environment.
1. Ensure the required AWS/OpenTelemetry packages are installed in your app.
1. Ensure AWS Cloudwatch resources are setup
1. Add `using SimpleOpenTelemetry.Extensions; builder.AddSimpleOpenTelemetry();` on your WebApplicationBuilder before the builder.Build();
1. Run the app in docker compose with a sidecar (see the example app for a dockerfile and dockercompose)
1. Confirm your telemetry on Cloudwatch

## X-Ray Remote Sampling

X-Ray remote sampling cannot be enabled by JSON config in SimpleOpenTelemetry.

Why:

- The AWS X-Ray remote sampler currently uses a non-standard setup pattern that requires building a resource in code first going against the lazy-loaded OpenTelemetry ResourceProvider pattern.

To enable in code, see:

- [example-apps/cloud/aws/ecs/README.md](../../../../example-apps/cloud/aws/ecs/README.md) (X-Ray Remote Sampling section)

If you also implement the optional code-only X-Ray remote sampler path shown in that example, add:

```powershell
dotnet add package OpenTelemetry.Sampler.AWS --version x.x.x
```

## Configuration notes

AWS ECS logs console output by default so you may wish to add `builder.Logging.ClearProviders()` before calling AddSimpleOpenTelemetry() to remove console logging output.

Other trace/metric instrumentations that may be useful in the this hosting scenario for deeper metrics:

- OpenTelemetry.Instrumentation.Runtime
- OpenTelemetry.Instrumentation.Process

See the [SimpleOpenTelemetry README.md](../../README.md#process-runtime) to set these up

A custom meter for System.Net.NameResolution is included as an example and can be removed if this metric is unneeded
