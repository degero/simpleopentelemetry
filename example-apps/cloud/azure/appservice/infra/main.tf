terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.81"
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

  tags = var.tags

  depends_on = [azurerm_log_analytics_workspace.law]
  lifecycle {
    ignore_changes = [workspace_id]
  }
  timeouts {
    create = "10m"
    read   = "10m"
    update = "10m"
  }
}

# Dedicated (empty) action group so we're not depending on Azure's
# auto-created "Application Insights Smart Detection" group
resource "azurerm_monitor_action_group" "noop" {
  name                = "ag-noop-${var.app_insights_name}"
  resource_group_name = azurerm_resource_group.rg.name
  short_name          = "noop"
}

# Adopts and disables the auto-generated "Failure Anomalies" rule.
# Terraform PUTs this on the same resource ID Azure already created,
# so it overwrites/adopts it in place — no manual import or az cli needed.
resource "azurerm_monitor_smart_detector_alert_rule" "failure_anomalies" {
  name                = "Failure Anomalies - ${azurerm_application_insights.appinsights.name}"
  resource_group_name = azurerm_resource_group.rg.name
  description         = "Failure Anomalies notifies you of an unusual rise in the rate of failed HTTP requests or dependency calls."
  severity            = "Sev3"
  frequency           = "PT1M"
  detector_type       = "FailureAnomaliesDetector"
  scope_resource_ids  = [azurerm_application_insights.appinsights.id]

  enabled = false   # <- disables it; Terraform re-asserts this every apply

  action_group {
    ids = [azurerm_monitor_action_group.noop.id]
  }

  tags = var.tags
}

# App Service Plan (Free Tier)
resource "azurerm_service_plan" "appserviceplan" {
  name                = var.app_service_plan_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = "F1"

  tags = var.tags
}

locals {
  conn_string = var.use_rbac ? "InstrumentationKey=${azurerm_application_insights.appinsights.instrumentation_key}" : azurerm_application_insights.appinsights.connection_string
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
    "OTEL_RESOURCE_ATTRIBUTES"                                        = "service.version=1.0.0,service.namespace=demo-simpleopentelemetry,deployment.environment.name=dev"
    "OTEL_SERVICE_NAME"                                               = var.otel_service_name
    "SCM_DO_BUILD_DURING_DEPLOYMENT"                                  = "true"
    "SimpleOpenTelemetry__ExporterOptions__AzureMonitor__ConnectionString" = local.conn_string
    "SimpleOpenTelemetry__DistroOptions__ConnectionString" = local.conn_string
    "SimpleOpenTelemetry__BuilderExtensions__0__Options__ConnectionString" = local.conn_string
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
