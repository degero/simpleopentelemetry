# Azure App Service SimpleOpenTelemetry ASP.NET Core example

## Overview

This example contains a low-complexity Azure App Service demo app that shows how to use SimpleOpenTelemetry with Azure Monitor / Application Insights.

- Two main folders: [app/](./app/) for the ASP.NET Core app and [simpleopentelemetry-config/](./simpleopentelemetry-config/) for OpenTelemetry config templates.
- Three sample config files are provided:
  - `aspnetcore-azureotel-distro-rbac.json` — (Recommended) Azure Monitor AspNetCore distro library config. Feature rich with livemetrics
  - `aspnetcore-azureotel-exporter-rbac.json` — Azure Monitor exporter library with RBAC auth to app insights config. Supports live metrics
  - `aspnetcore-azureotel-exporter-by-signal-rbac.json` — Azure Monitor exporter library with explicit trace/metric/log exporter config. No live metrics
- The sample app uses `builder.AddSimpleOpenTelemetry()` and the Azure resource detector.
- For more Azure-specific config guidance and package notes, see [docs/configuration/examples/azure/README.md](../../../../docs/configuration/examples/azure/README.md).
- OpenTelemetry/SimpleOpenTelemetry events to console via `EnableOtelEventListeners` app setting
- Trace/Metrics for AspNetCore and HttpClient (distro includes httpclient, sqlclient automatically)
- Distro config adds the OpenTelemetry.Instrumentation.AspNetCore for metrics as the distro only includes a subset in dotnet 8+ of `Microsoft.AspNetCore.Hosting` of the meters used in the lib: aspnetcore hosting, aspnetcore.memory and kestrel etc.

## Prerequisites

- .NET 10 SDK
- Azure CLI authenticated: `az login`
- Azure subscription
- If using apps locally, ability to create a Resource group and Appinsights instance and, if using RBAC (default), assign your Azure user the required Azure Monitor roles as described below. Scripts are provided below.
- If deploying to Azure, contributor role on the subscription or a management group. Various tools [covered below](#prerequisites-1)

## Azure observability environment setup and configuration

- This sample sends telemetry to Azure Monitor / Application Insights.
- `Azure.Identity.DefaultAzureCredential` works locally with `az login` and in App Service with managed identity.
- If using a plain connection string instead of RBAC, configure the correct secret environment variable and remove the `Credential` setting where required.
- Recommended environment variable:
  - `APPLICATIONINSIGHTS_STATSBEAT_DISABLED=true`
- `OTEL_RESOURCE_ATTRIBUTES` can be used to attach service metadata, while the Azure resource detector adds Azure host metadata.
- `OTEL_SERVICE_NAME` may be overwritten by the Azure resource detector for App Service. If you need a stable service name, use custom resource configuration or explicit `service.name` settings.
- Use `EnableOtelEventListeners=true` for additional OpenTelemetry diagnostic output.

## Setup your Azure CLI

1. Sign in and select your subscription:

```powershell
az login
az account set --subscription "<subscription-id>"
```

## Setup your SimpleOpentelemetry configuration

In `example-apps/cloud/azure/appservice/app/`:

1. Copy one of the sample configs to `appsettings.Development.json` or (for deployment to appservice) `appsettings.Production.json`:
   - `simpleopentelemetry-config/aspnetcore-azureotel-exporter-rbac.json`
   - `simpleopentelemetry-config/aspnetcore-azureotel-exporter-by-signal-rbac.json`
   - `simpleopentelemetry-config/aspnetcore-azureotel-distro-rbac.json`

1. For local vscode debugging launch use, remove `Microsoft.Hosting.Lifetime` logging setting

1. Customize the config values if need be
   - `OTEL_SERVICE_NAME`
   - `OTEL_RESOURCE_ATTRIBUTES`

1. Sampling is set to 100% for dev/debugging purposes

## Local run with selected config

1. Create a Resource Group

```powershell
az group create --location "eastus" --name "rg-soteltestazure"
```

1. Create an Application Insights resource:

```powershell
az monitor app-insights component create `
  -g rg-soteltestazure `
  --app "soteltestazure-ai" `
  --location "eastus" `
  --kind "web" `
  --application-type "web"
```

1. Get the Application Insights connection string:

```powershell
az monitor app-insights component show `
  --app "soteltestazure-ai" `
   -g rg-soteltestazure `
  --query connectionString -o tsv
```

1. If using RBAC (default), follow [docs/configuration/examples/azure/README.md](../../../../docs/configuration/examples/azure/README.md) for role assignment and Azure RBAC setup.

1. Add debug logging at the top of `appsettings.Development.json`:

```json
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

1. Customize the config values if need be
   - `OTEL_SERVICE_NAME`
   - `OTEL_RESOURCE_ATTRIBUTES`
   - Azure exporter / distro options
   - connection string or RBAC credential settings

1. Set the appropriate connection string secret environment variable for your config:
   - distro config: `SimpleOpenTelemetry__DistroOptions__ConnectionString`
   - exporter by-signal config `SimpleOpenTelemetry__ExporterOptions__AzureMonitor__ConnectionString`
   - exporter config `SimpleOpenTelemetry__BuilderExtensions__0__Options__ConnectionString`

1. Run the app:

```powershell
dotnet run --project app
```

6. Open the URL shown in the console and navigate the sample pages.

7. Validate telemetry in Azure Monitor / Application Insights (see further below).

8. Delete the resource group

```powershell
az group delete --name "rg-soteltestazure"
```

## Deploy to App Service with Terraform and Azure Developer CLI (azd)

This approach uses Infrastructure as Code (Terraform) with Azure Developer CLI for a fully automated deployment. This uses RBAC access for the appservice to send telemetry to appinsights.

### Prerequisites

- [Terraform](https://www.terraform.io/downloads.html) (>= 1.0)
- [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) (azd)
- Azure CLI authenticated: `az login`
- AZD authenticated: `azd auth login --use-device-code`
- A `appsettings.Production.json` file setup in the /app folder from sample in [simpleopentelemetry-config/](./simpleopentelemetry-config/)

### Deployment

This uses Azure Developer CLI combined with terraform to provision infrastructure then deploy to it.

Terraform will create:

- A resource group
- A log analytics workspace for app insights
- An Application Insights resource
- An App Service Plan (Free tier `F1`)
- An App Service with system-assigned managed identity
- A role assignment granting the App Service identity `Monitoring Metrics Publisher` access to Application Insights

The deployment will also configure the [required app settings](../../../../docs/configuration/examples/azure/README.md#app-settings-configuration-reference), including the app insights connectionstring for RBAC access. You can test using the full connectionstring (no RBAC) by removing the 'Credential' setting from your `appsettings.Production.json`

The values for the Terraform deployment are passed directly from [azure.yaml](azure.yaml) to the infrastructure module, so there is no need to setup a local `terraform.tfvars` file.

Before provisioning starts, the deployment will prompt you to choose an environment name, subscription, and an Azure region from the list of available locations, and if you wish to use RBAC, select which suits. These settings are saved in the `.azure` folder. There are also other defaults, such as the resourcegroup name of `rg-soteltestazure` and other resource names, if you want to override, edit the `parameters` block in [azure.yaml](azure.yaml) before running the `azd up` command.

To deploy:

```powershell
azd up
```

To redeploy the app on changes:

```powershell
azd deploy
```

The app url is output to the console to navigate the pages to generate telemetry.

### Run Terraform directly for troubleshooting

Optionally if you have any issues with the deployment

```powershell
cd infra
terraform init
terraform plan
terraform apply
```

Or see [troubleshooting](#troubleshooting-production-use-and-other-documentation)

### Verify telemetry in Azure Monitor

- Go to the [Azure Portal](https://portal.azure.com)
- Navigate to the Application Insights resource (name: `ai-soteltestazure` by default)
- Use **Live Metrics** or **Logs** to verify traces, metrics, and dependency calls
- Query the `traces`, `requests`, or `dependencies` tables in the **Logs** section

Example KQL query to view recent requests:

```kusto
requests
| where timestamp > ago(10m)
| project timestamp, name, duration, success
| order by timestamp desc
```

### Cleanup

```powershell
azd down
```

To remove all deployed resources on manual terraform deployment:

```powershell
# From infra directory
terraform destroy
```

Or delete the resource group directly if any issues occur

```powershell
az group delete --name "rg-soteltestazure" --yes --no-wait
```

## Production use and other documentation

For production templates, troubleshooting and documentation see [docs/configuration/examples/azure/README.md](../../../../docs/configuration/examples/azure/README.md)
