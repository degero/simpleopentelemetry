# Google Cloud Platform CloudRun SimpleOpenTelemetry ASP.NET Core example

## Overview

This example contains low-complexity low-cost hosted app deployment on Google Cloud Run.


- Two SimpleOpenTelemetry configurations: Direct export or (Recommended) OpenTelemetry Collector sidecar [./simpleopentelemetry-config/README.md](./simpleopentelemetry-config/README.md)

- Demo AspNetCore app sending all log levels, custom trace test telemetry on homecontroller. Direct export custom code for auth, telemetry processing / filtering when not using a collector [./app](./app/)

- Two OpenTelemetry Collector configurations: OTLP and Legacy [./otel-collector-config/README.md](./otel-collector-config/README.md)

- Local docker compose for running app with collector [./ocaldev-docker](./localdev-docker/)

- Uses 'Google Built' OpenTelemetry collector to handle auth to google services, and legacy exporters

- Terraform Cloud workload: Direct public access Cloud Run 'request based' 0.75 cpu usage, 768Mb mem. Service account + role access setup for Telemetry endpoints. Secret store for collector config yaml [./infra](./infra/)

- Scope: minimal and demo-friendly, this is not PRODUCTION ready.

<br>

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Terraform
- gcloud CLI authenticated (`gcloud auth login`)
- Public container repository to use with infra deployment (eg Github Container Registry or Docker hub)

<br>

## Google observability environment and quirks

Before proceeding it is worth looking at the various options Google provides for instrumentation: [Google Cloud Observability - Choose an instrumentation approach](https://docs.cloud.google.com/stackdriver/docs/instrumentation/choose-approach)

At the time of writing (June 2026) Metrics OTLP endpoint is [Pre-GA](https://docs.cloud.google.com/stackdriver/docs/otlp-metrics/overview). For production workloads it is recommended to use the legacy 'googlemanagedprometheus' exporter in a collector sidecar config. Another reason using the collector is recommended.

Google has mandatory attributes required in telemetry sent: `gcp.project_id`, `service.instance.id` and `cloud.region` (or location, however google's example otel collectors `transform/collision` rename it and seems to be reserved). If using a collector these sample configurations take care of this. For direct export, the 'GCP' resource detector set in the [appsettings.DirectExport.json](./simpleopentelemetry-config/appsettings.DirectExport.json) will add these when in cloud run, for local use you can set them in "OTEL_RESOURCE_ATTRIBUTES" the file has these as guidance.

Google's Observability platform has some quirks / differences from some OpenTelemetry norms which are noted and worth checking in this readme as well as information about production considerations for collector configuration [./otel-collector-config/README.md](./otel-collector-config/README.md).

<br>

## Setup your Google Cloud Platform environment

Choose a name for your project. All examples use the ProjectID/Shortname 'soteltest'

Create a project:

```
gcloud projects create soteltest --name="SotelTest"
```

Set active project, enable billing (or on [web console](https://console.cloud.google.com/billing/linkedaccount?project=soteltest), request who controls billing to do so) and login for ADC (Google Application Default Credentials):

```
gcloud config set project soteltest
gcloud billing projects link soteltest --billing-account=<yourbillingaccoutid>
gcloud auth application-default login
gcloud auth application-default set-quota-project soteltest
```

Setup api access:
```
gcloud services enable logging.googleapis.com telemetry.googleapis.com monitoring.googleapis.com cloudtrace.googleapis.com --project soteltest
```

If running the app locally (direct or with collector on Docker)

```powershell
$Env:PROJECT_ID="soteltest"
$Env:USER_EMAIL="yourgoogleconsoleaccount@email.com"

gcloud projects add-iam-policy-binding $Env:PROJECT_ID --member="user:$Env:USER_EMAIL" --role="roles/monitoring.metricWriter"

gcloud projects add-iam-policy-binding $Env:PROJECT_ID --member="user:$Env:USER_EMAIL" --role="roles/cloudtrace.agent"

gcloud projects add-iam-policy-binding $Env:PROJECT_ID --member="user:$Env:USER_EMAIL" --role="roles/logging.logWriter"

gcloud projects add-iam-policy-binding $Env:PROJECT_ID --member="user:$Env:USER_EMAIL" --role="roles/serviceusage.serviceUsageConsumer"

```

<br>

## Local run with direct export

In the [app](./app/) directory:


1. Copy the [appsettings.DirectExport.json](./simpleopentelemetry-config/appsettings.DirectExport.json) to `appsettings.Development.json`
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. In `appsettings.Development.json` add at the top for extra debugging / logging:

```
{
  "EnableOtelEventListeners": "true",
  "Logging": {
    "LogLevel": {
      "Default": "Trace"
    }
  },
  ...existing lines...
}
```
1. In `appsettings.Development.json` adjust the `OTEL_RESOURCE_ATTRIBUTES` value for `gcp.project_id=soteltest,service.instance.id=otel-local-dev,location=us-east1` eg. if using a different projectid (shortname), or location or want a different resource instance id. You can also change the `GOOGLE_CLOUD_LOG_NAME` if you want this different.
1. Run the app: `dotnet run`
1. Navigate to [http://localhost:5195](http://localhost:5195) and navigate between the two pages to generate telemetry
1. Validate telemetry in Cloud Logging, Trace Explorer, and Cloud Monitoring. Logs appear under the logname 'otlp' by default. You can check app side metrics by selecting 'Prometheus Target > Aspnetcore > ...'

<br>

## Local run with sidecar collector

In the [app](./app/) directory:

1. Copy [simpleopentelemetry-config/appsettings.OtelCollector.json](./simpleopentelemetry-config/appsettings.OtelCollector.json) to `appsettings.Development.json`
1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting
1. In `appsettings.Development.json` add at the top for extra debugging / logging:

```
{
  "EnableOtelEventListeners": "true",
  "Logging": {
    "LogLevel": {
      "Default": "Trace"
    }
  },
  ...existing lines...
}
```

In the [localdev-docker](./localdev-docker/) directory:

1. Copy [localdev-docker/.env](./localdev-docker/.env.example) to `.env` file
1. Verify values for `GOOGLE_CLOUD_PROJECT`, `GOOGLE_CLOUD_REGION` and `OTEL_RESOURCE_ATTRIBUTES` related to your project id / region. Set `GCLOUD_CONFIG_DIR` to where gcloud cli stores its files, specifically `application_default_credentials.json`
1. Run:

```powershell
./BuildAndRunDocker.ps1
```

1. Check in your docker that the otel container logs show `Everything is ready. Begin running and processing data.` as it may take a moment to start up.
1. Open `http://localhost:8080` and navigate between the two pages to generate telemetry
1. Validate telemetry in Cloud Logging, Trace Explorer, and Cloud Monitoring. Logs appear under the logname 'otlp' by default. You can check app side metrics by selecting 'Prometheus Target > Aspnetcore > ...'

<br>

## Deploy to Cloud Run with Terraform

*IMPORTANT*: This terraform is not PRODUCTION ready, it contains the lowest cost request based billing Cloud Run option allowing all direct public access. Note telemetry collection/delivery my be impacted by the 'request based' cpu usage / billing.

Google Cloud Run will log stdout/stderr out of the box, this combined with the OpenTelemetry / SimpleOpenTelemetry event logging to stdout should give adequate logging to detect any configuration issues.

NOTE: If using a (recommended) github container registry image (ghcr.io). With the github cli run:

```
gh auth refresh -h github.com -s write:packages,read:packages
gh auth token | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

In the [app](./app/) directory:

1. Copy the [appsettings.DirectExport.json](./simpleopentelemetry-config/appsettings.DirectExport.json) for direct exporting in Cloud Run (no collector sidecar) or [simpleopentelemetry-config/appsettings.OtelCollector.json](simpleopentelemetry-config/appsettings.OtelCollector.json) to `appsettings.Production.json`
1. In `appsettings.Production.json` add at the top for trace level extra logging:

```
{
  "EnableOtelEventListeners": "true",
  "Logging": {
    "LogLevel": {
      "Default": "Trace"
    }
  },
  ...existing lines...
}
```
1. Run in Powershell: `rmdir ..\publish\` then `dotnet publish -c Release -o ..\publish`

In the [root](./) directory:

1. Run `docker build --no-cache -t [ghcr.io/docker.io]/username/[yourtag] .`
1. Run `docker push ghcr.io/username/[yourtag]`

In the [infra](./infra/) directory

1. Based on the app configuration you chose copy [terraform.tfvars.directexportexample](./infra/terraform.tfvars.directexportexample) or [terraform.tfvars.sidecarexample](./infra/terraform.tfvars.sidecarexample) to `terraform.tfvars`

1. Update `terraform.tfvars` file `app_image` with your image '`ghcr.io/username/[yourtag]`', set `region` to your project region and `project_id`/`otel_resource_attributes` if project is not '`soteltest`'.

1. Cloud Run injects tracing and has sampling that cannot be configured which impacts tracing negatively. To verify all your traces, set the 'demonstration' `ignore_cloudrun_trace_sampling` terraform variable true . See [otel-collector-config/README.md](./otel-collector-config/README.md) and the notes in the app [app/Program.cs](./app/Program.cs) for further detail.

1. If using `terraform.tfvars.sidecarexample`. Copy either recommended [otel-collector-config/otelcollector-cloudrun-otlpexport.yaml](./otel-collector-config/otelcollector-cloudrun-otlpexport.yaml) or [otel-collector-config/otelcollector-cloudrun-legacyexport.yaml](./otel-collector-config/otelcollector-cloudrun-legacyexport.yaml) to `otel-collector-config.yaml` beside the terraform.

1. Deploy the terraform:

```
terraform init
terraform plan
terraform apply
```

1. Use `service_url` output to navigate between the two pages to generate telemetry
1. Validate telemetry in Cloud Logging, Trace Explorer, and Cloud Monitoring. Logs appear under the logname 'otlp' by default. You can check app side metrics by selecting 'Prometheus Target > Aspnetcore > ...'


**Troubleshooting**:

- Direct export may have issues getting the ADC `Your default credentials were not found.` as the credentials have not propagated yet. Adding a new env var to the app in main.tf and running terraform apply will restart it. As direct export is just a proof of concept and not recommended for production it has not been resolved.


<br>

## Cleanup Cloud run deployment

In the [infra](./infra/) directory

```powershell
terraform destroy
```

## Cleanup the Google cloud project

If you don't having billing access, request who does to unlink before delete

```
gcloud billing projects unlink soteltest
gcloud projects delete soteltest
```

<br>

## Google documentation and resources

**Cloud run**

[Google Cloud Run OpenTelemetry sidecar guide](https://docs.cloud.google.com/stackdriver/docs/instrumentation/opentelemetry-collector-cloud-run)

[Dotnet example app with CloudRun (outdated)](https://github.com/GoogleCloudPlatform/opentelemetry-cloud-run)

**Google OpenTelemetry distro**

[Google-Built OpenTelemetry Collector Documentation](https://docs.cloud.google.com/stackdriver/docs/instrumentation/google-built-otel)

[Github - Google built collector](https://github.com/GoogleCloudPlatform/opentelemetry-operations-collector)


**Google samples**

[Google Docs - Samples for collector based Otel exports](https://docs.cloud.google.com/trace/docs/setup/sample-overview)

[Write OTLP metrics by using an OpenTelemetry Collector sidecar - Google Documentation](https://docs.cloud.google.com/run/docs/tutorials/custom-metrics-opentelemetry-sidecar)


**Google Application Default Credentials**

[Set up Application Default Credentials - Google Documentation](https://docs.cloud.google.com/docs/authentication/provide-credentials-adc)


**Google Kubernetes OpenTelemetry**

[Github - Google OTLP Kubernetes Ingest samples](https://github.com/GoogleCloudPlatform/otlp-k8s-ingest)
