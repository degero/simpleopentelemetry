# AWS AppSettings Configs

This folder contains AWS-focused appsettings examples for SimpleOpenTelemetry.

## Included files

- `aspnetcore-ecs-otelcollector.json`: Base appsettings for ASP.NET Core apps that send telemetry via an ADOT (AWS Distro for OpenTelemetry) collector sidecar. See the [example app](../../examples-cloud/aws/ecs/) for how to use this config in AWS.

## ADOT collector configs

AWS recommends exporting to the newer OTLP endpoints. See the [adotcollector-ecs-otlpexport.yml](../../examples-cloud/aws/ecs/adot-collector-config/adotcollector-ecs-otlpexport.yml) example config file for the ADOT collector to use with this config file. There are several methods for telemetry collection in AWS than the collector. For help on choosing which Telemetry collection solution suits your needs see the (Amazon Cloudwatch - Getting started)[https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPGettingStarted.html].

## How to use

1. Copy `aspnetcore-ecs-otelcollector.json` into your app as `appsettings.Development.json` or `appsettings.Production.json`.
2. Update values such as `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, region, and endpoint settings for your environment.
3. Ensure the required AWS/OpenTelemetry packages are installed in your app.
4. Ensure AWS Cloudwatch resources are setup

For a full working application and infrastructure example, see:

- `examples-cloud/aws/ecs/README.md`

## Required package install commands

Run these in your app project folder:

```
dotnet add package SimpleOpenTelemetry --prerelease
dotnet add package OpenTelemetry.Instrumentation.AWS
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Extensions.AWS
dotnet add package OpenTelemetry.Resources.AWS
```

## X-Ray note (important)

X-Ray remote sampling cannot be enabled by JSON config in SimpleOpenTelemetry.

Why:

- The AWS X-Ray remote sampler currently uses a non-standard setup pattern that requires building a resource in code first going against the lazy-loaded OpenTelemetery ResourceProvider pattern.


To enable in code, see:

- `examples-cloud/aws/ecs/README.md` (X-Ray Remote Sampling section)
- `examples-cloud/aws/ecs/app/Program.cs` (the `UseXraySampler` code path)

If you also implement the optional code-only X-Ray remote sampler path shown in that example, add:

```powershell
dotnet add package OpenTelemetry.Sampler.AWS --prerelease
```
