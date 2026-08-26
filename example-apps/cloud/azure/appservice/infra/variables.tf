variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "eastus"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-soteltestazure"
}

variable "app_name" {
  description = "Name of the App Service"
  type        = string
  default     = "soteltestazure"
}

variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
  default     = "plan-soteltestazure"
}

variable "app_insights_name" {
  description = "Name of the Application Insights resource"
  type        = string
  default     = "ai-soteltestazure"
}

variable "otel_service_name" {
  description = "OpenTelemetry service name"
  type        = string
  default     = "soteltestazure"
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default = {
    "project"     = "simple-opentelemetry"
    "environment" = "development"
  }
}

variable "use_rbac" {
  description = "Use RBAC access from appservice to appinsights"
  type        = bool
}
