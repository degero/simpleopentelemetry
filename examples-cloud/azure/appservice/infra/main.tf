terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Resource Group
resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location

  tags = var.tags
}

# log analytics workspace
resource "azurerm_log_analytics_workspace" "law" {
  name                = "law-appinsights-dev"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30  # minimum allowed
}

# Application Insights
resource "azurerm_application_insights" "appinsights" {
  name                = var.app_insights_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  workspace_id        = azurerm_log_analytics_workspace.law.id
  application_type    = "web"

  retention_in_days = 30  # minimum allowed

  # Optional: cap daily data volume to control costs in dev
  daily_data_cap_in_gb                     = 1
  daily_data_cap_notifications_disabled    = false
  tags = var.tags
}

# # need this to remove auto-gen alert for appinsights
# resource "null_resource" "cleanup_failure_anomalies" {
#   triggers = {
#     rg_name       = azurerm_resource_group.rg.name
#     ai_name       = azurerm_application_insights.appinsights.name
#     # subscription  = data.azurerm_client_config.current.subscription_id
#   }

#   provisioner "local-exec" {
#     when    = destroy
#     command = <<-EOT
#       az monitor alert-rule delete \
#         --resource-group "${self.triggers.rg_name}" \
#         --name "Failure Anomalies - ${self.triggers.ai_name}" || true
#     EOT
#   }

#   depends_on = [azurerm_application_insights.appinsights]
# }

# App Service Plan (Free Tier)
resource "azurerm_service_plan" "appserviceplan" {
  name                = var.app_service_plan_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = "F1"

  tags = var.tags
}

# App Service (Windows Web App)
resource "azurerm_windows_web_app" "appservice" {
  name                = var.app_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.appserviceplan.id

  # Enable system-assigned managed identity
  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "APPLICATIONINSIGHTS_STATSBEAT_DISABLED"                          = "true"
    "OTEL_METRICS_EXEMPLAR_FILTER"                                    = "trace_based"
    "OTEL_RESOURCE_ATTRIBUTES"                                        = "service.version=1.0.0,service.namespace=demo-simpleopentelemetry,deployment.environment.name=dev"
    "OTEL_SERVICE_NAME"                                               = var.otel_service_name
    "SCM_DO_BUILD_DURING_DEPLOYMENT"                                  = "true"
    "SimpleOpenTelemetry__ExporterOptions__AzureMonitor__ConnectionString" = "InstrumentationKey=${azurerm_application_insights.appinsights.instrumentation_key}"
    "SimpleOpenTelemetry__DistroOptions__ConnectionString" = "InstrumentationKey=${azurerm_application_insights.appinsights.instrumentation_key}"
    "SimpleOpenTelemetry__BuilderExtensions__0__Options__ConnectionString" = "InstrumentationKey=${azurerm_application_insights.appinsights.instrumentation_key}"
  }

  # Site config
  site_config {
    always_on = false

    application_stack {
      dotnet_version = "v10.0"
    }
  }

  tags = merge(var.tags, {
    "azd-service-name" = "web"
  })
}

# Role Assignment: Monitoring Metrics Publisher
# This allows the App Service managed identity to write telemetry to Application Insights
resource "azurerm_role_assignment" "app_service_metrics_publisher" {
  scope              = azurerm_application_insights.appinsights.id
  role_definition_name = "Monitoring Metrics Publisher"
  principal_id       = azurerm_windows_web_app.appservice.identity[0].principal_id
}
