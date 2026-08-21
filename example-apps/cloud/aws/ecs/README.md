# AWS ECS SimpleOpenTelemetry ASP.NET Core example

## Overview

This setup is focused on code-based application instrumentation push exporting to the [ADOT (Aws Distro of OpenTelemetry) Collector](https://github.com/aws-observability/aws-otel-collector) and does not cover using the [AWS ADOT autoinstrumentation](https://aws-otel.github.io/docs/getting-started/dotnet-sdk/auto-instr) nor the [AWS Container Insights](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/ContainerInsights.html) or [Cloudwatch Agent](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/Install-CloudWatch-Agent.html) for host/platform level telemetry.

For help on choosing which Telemetry collection solution suits your needs see the [Amazon Cloudwatch - Getting started](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPGettingStarted.html). The sample collector yml configs can be adapted for use with the cloudwatch agent which now supports OTLP endpoints if you need the full set of telemetry from host and app.

These SimpleOpenTelemetry configurations are based on the components used in [AWS otel community dotnet sample app](https://github.com/aws-observability/aws-otel-community/tree/master/sample-apps/dotnet-sample-app). Due to the required signing of requests to AWS CloudWatch OTLP endpoints the easiest (least code setup) is to use the AWS ADOT otel collector. Even easier, but with less flexibility, is to use the AWS ADOT autoinstrumentation however log export is not supported currently (June 2026) for dotnet.

Running apps to write directly to the OTLP endpoints is NOT RECOMMENDED for production (use a collector or scrape) and loses the ability to use the X-Ray sampler but provides a simpler solution for dev/debugging (no sidecar collector). It also requires some custom code for the OpenTelemetry exporter httpclient auth with the community driven [AwsSignatureVersion4 nupkg](https://github.com/FantasticFiasco/aws-signature-version-4) (as it is not built into the AWS SDK)

OpenTelemetry and SimpleOpenTelemetry Events are pushed to console output and will appear in cloudwatch logs for extra visibility / configuration debugging. You can turn this off in the configuration files 'EnableOtelEventListeners' setting.

Included in this example is:

- AWS Resource Detectors

- Exporting Log/Trace/Metric to AWS Cloudwatch using OTLP or Legacy Cloudwatch/XRay exporters

- Trace source listening for anything under this app dotnet namespace 'soteltestaws.\*'

- AWS XRay Propagator - Required for propagating the Trace Context to AWS Services that are integrated with X-Ray

- AWS TraceId extension - To create trace ids compatible with the AWS X-Ray backend

- AWS XRay Remote Sampler (for appsettings.OtelCollector.json - UseXraySampler setting only) - Required if you need to sample requests using X-Ray Remote Sampling rules

- CloudWatch Log group: /app/soteltestaws/dev, log stream: app

- Metrics/Traces for AWS, HttpClient, AspNetCore

- Sample amazon/aws-otel-collector sidecars exporting to OTLP endpoints using otlpexport: aotcollector-ecs-otlpexport.yml or using the legacy proprietary exporters: adotcollector-ecs-legacyexport.yml (cloudwatchlogs/awsxray/awsemf for logs/traces/metrics)

- Generates log and custom trace on HomeController.cs - app logging defaults to trace level to capture all log levels generated

Note: collector--legacyexport.yml uses the awsxray exporter which does not include any of the sent attributes on child spans. This may be useful to quickly identify child spans in the transaction search. the awsemf metrics exports to a log group it creates whereas the new otlp metrics export uses cloudwatch manage metrics store

## Prereqs

- .NET 10 SDK
- Docker Desktop
- Terraform
- AWS CLI logged in (with your default region set)
- Public container repository to use with terraform deployment (eg Github Container Registry or Docker hub)

## Selecting your region

Full OTLP endpoint support in AWS is not in GA at all regions in June 2026. To enable to full set of functionality either check in your region or use the initial supported preview regions: US East (N. Virginia), US West (Oregon), Asia Pacific (Sydney), Asia Pacific (Singapore), and Europe (Ireland).

## Example SimpleOpenTelemetry config collector dependency

In simpleopentelemetry-config:

- appsettings.DirectExport.json - no otel collector possible, sends direct to cloudwatch

- appsettings.OtelCollector.json - requires ADOT otel collector sidecar using: /adotcollector-config/adotcollector-ecs-otlpexport.yml (exports to cloudwarch using OTLP) or /adotcollector-config/adotcollector-ecs-legacyexport.yml (uses AWS custom Cloudwatch/XRay exporters)

NOTE: AWS recommends to use [OTLP for metrics](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/metrics-otel-recommended.html) as it uses a metrics store supporting PromQL rather than the legacy EMF which writes to a separate log group. Traces using legacy Xray are now in maintenance mode and it is [recommended to migrate to OTLP](https://docs.aws.amazon.com/xray/latest/devguide/xray-sdk-migration.html). There is no formal stance on logs but the direction appears to have shifted to using OTLP also

## AWS Cloudwatch / XRay traces setup

1. Setup IAM access to CloudWatch (if any identity's will access it directly)
2. Enable Transaction search (you may not see it in web console if you region doesn't support it)
3. Optional: Enable OTel enrichment and resource tags (for PromQL support) and Enable resource tags on Telemetry (for easier filtering by resource tag) in Cloudwatch > Settings.

Guides:

[AWS - Enable Transaction search guide](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/Enable-TransactionSearch.html#CloudWatch-Transaction-Search-EnableConsole)

[AWS - CloudWatch IAM setup guide](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLP-UsingADOT.html#setup-iam-permissions-role)

[AWS - General OTLP endpoint guide](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLPEndpoint.html)

## X-Ray Remote Sampling

This sampler on the OpenTelemetry SDK is currently in Alpha at the time of writing, is only supported through the AWS autoinstrumentation lib or the aws-otel-collector. This example will enable it in code if you use the appsettings.OtelCollector.json file. It is not supported in SimpleOpenTelemetry configuration due to a poor pattern requiring a pre-built resource provider and no ability to sign http requests.

You can see the setup of this in [Program.cs)](./app/Program.cs) (the `UseXraySampler` code path)

For Documentation to setup X-Ray Sampling and sampling rules in your AWS env see:

[XRay Remote Sampling getting started](https://aws-otel.github.io/docs/getting-started/remote-sampling)

## Run locally and send telemetry directly to AWS

The configuration [simpleopentelemetry-config/appsettings.DirectExport.json](./simpleopentelemetry-config/appsettings.DirectExport.json) allows for a simplified quick way to run apps locally or in lower AWS envs and confirm/use telemetry in AWS Cloudwatch. It is not recommended for PRODUCTION due to no offline storage / batching amongst other reasons.

1. Create a log group and logstream in the loggroup (Cloudwatch > Log Management) matching your AWS_LOG_GROUP_NAME setting and AWS_LOG_STREAM_NAME (adjust names if necessary)

1. Copy simpleopentelemetry-config/appsettings.DirectExport.json to app/appsettings.Development.json

1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting

1. Set your OTEL*EXPORTER_OTLP*\* and AWS_REGION to the AWS endpoints and region you are using (see below guide)

1. Run the application

1. Check Metrics (CloudWatch Query Studio/Classic metrics for legacy export), Logs (Log analytics with filter to your log group), Traces (Transaction search and filter to your service name)

## Run locally using AWS otel collector

Note: the 'AWSSDK\*' packages are not needed for this type of app, it is only for direct export example above and can be removed with the code in Program.cs to reduce the app footprint.

The configuration [simpleopentelemetry-config/appsettings.OtelCollector.json](./simpleopentelemetry-config/appsettings.OtelCollector.json) requires either running the app and collector in the /localdev-docker/ or you can debug the app locally and remove the app service from the docker-compose.yml.

1. Create a log group and logstream in the loggroup (Cloudwatch > Log Management) matching your log_group_name setting and log_stream_name to match what is in /localdev-docker/.env, adjust names if you want something different.

1. Adjust copy /localdev-docker/.env.example to .env and set to the region you are using and change OTEL_COLLECTOR_CONFIG, USE_XRAY_SAMPLER to what you wish to use

1. Copy simpleopentelemetry-config/appsettings.OtelCollector.json to app/appsettings.Development.json

1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting

1. If debugging your app outside docker compose: Change the OTEL_EXPORTER_OTLP_ENDPOINT in this file to host 'localhost' and remove service: 'otel' from the docker-compose.yml

1. Run the app in container with BuildAndRunDocker.ps1 or if you removed the app service from docker-compose.yml use in /app: dotnet run, and in /localdev-docker: docker compose up

1. Check Metrics (CloudWatch Query Studio/Classic metrics for legacy export), Logs (Log analytics with filter to your log group), Traces (Transaction search and filter to your service name)

## Deploy to AWS ECS+EC2

This example only allows using the simpleopentelemetry-config/appsettings.OtelCollector.json. The terraform is NOT PRODUCTION GRADE, it is for getting the cheapest ECS instance (ec2 spot t3.micro) up to verify your telemetry, it has no vpc/gw, allows public ssh to the ec2 instance, as well as logging container stdout for extra troubleshooting. For a better guide on infra best practices see [https://github.com/aws-samples/amazon-ecs-fullstack-app-terraform](https://github.com/aws-samples/amazon-ecs-fullstack-app-terraform/tree/main).

_IMPORTANT:_ The sample adotcollector-local-X.yml configs are designed for PRODUCTION with health check, trace filters, memory limit and tail sampling to reduce load/cost of telemetry. If wanting to test all traces or using xray sampling (appsetting UseXraySampler - default on) remove the tail sampling from the config.

1. In /app: Copy the /simpleopentelemetry-config/appsettings.OtelCollector.json settings file to appsettings.Production.json adjust feature flags as needed

1. In /app: `dotnet publish -c Release -o ..\publish`

1. In this example directory: `docker build --no-cache -t <yourtagname> .`

1. Use docker push or other tooling to deliver your image to a container registry (eg public github / docker hub being the simplest)

1. In /infra: copy the terraform.tfvars.example to terraform.tfvars file and set your region, image, and options

1. Copy a adot-collector config from /adot-collector-config to the file adotcollector-config.yml beside the /infra terraform files

1. In /infra: Deploy the terraform with terraform init/plan/apply

1. View the app on the uri output and navigate between pages to generate more telemetry

1. Check Metrics (CloudWatch Query Studio/Classic metrics for legacy export), Logs (Log analytics with filter to your log group), Traces (Transaction search and filter to /app/service-name/dev default is /app/soteltestaws/dev)

1. in the /infra: Use `terraform destroy` to destroy the ECS+EC2 instance, roles etc (you may need to terminate/delete the ec2 instance to unblock)

NOTE: you may need to adjust the spot pricing higher than $0.008/hr for your region if it cant allocate. Use this cmd to determine what spots are in your region

```sh
aws ec2 describe-spot-price-history \
  --instance-types t3.micro \
  --product-descriptions "Linux/UNIX" \
  --region <yourregion> \
  --max-results 5
```

## Refining your ECS environment

To add other telemetry attributes, refer to the ECS samples from this [aws-observability repo](https://github.com/aws-observability/aws-otel-collector/tree/main/config/ecs)

## Other links

An alternative to SimpleOpenTelemetry is to send data directly to OTLP endpoints with AWS distro (ADOT) autoinstrumentation (logs are not supported):

[AWS - Exporting collector-less telemetry using AWS Distro for OpenTelemetry (ADOT) SDK](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/CloudWatch-OTLP-UsingADOT.html)

[ADOT aws-opentelemetry-collector github repo](https://github.com/aws-observability/aws-otel-collector)

[Xray remote sampler github repo](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Sampler.AWS/README.md)

[AWS dotnet manual Otel instrumentation guide for legacy and OTEL SDK](https://docs.aws.amazon.com/xray/latest/devguide/introduction-dotnet.html#manual-instrumentation-dotnet)

[AWS dotnet manual Otel instrumentation guid for OTEL SDK](https://aws-otel.github.io/docs/getting-started/dotnet-sdk/manual-instr)

[Getting Started with the AWS Distro for OpenTelemetry Collector](https://aws-otel.github.io/docs/getting-started/collector)

[AWS blog April 2026 - PromQL for Cloudwatch](https://aws.amazon.com/blogs/mt/introducing-opentelemetry-promql-support-in-amazon-cloudwatch/)
