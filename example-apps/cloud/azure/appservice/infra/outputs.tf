output "app_service_url" {
  description = "URL of the deployed App Service"
  value       = "https://${azurerm_windows_web_app.appservice.default_hostname}"
}

output "app_service_id" {
  description = "ID of the App Service"
  value       = azurerm_windows_web_app.appservice.id
}

output "app_insights_instrumentation_key" {
  description = "Instrumentation key of the Application Insights resource"
  value       = azurerm_application_insights.appinsights.instrumentation_key
  sensitive   = true
}

output "app_insights_connection_string" {
  description = "Connection string of the Application Insights resource"
  value       = azurerm_application_insights.appinsights.connection_string
  sensitive   = true
}

output "app_service_principal_id" {
  description = "Principal ID of the App Service managed identity"
  value       = azurerm_windows_web_app.appservice.identity[0].principal_id
}

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.rg.name
}

output "location" {
  description = "Azure region where resources were deployed"
  value       = azurerm_resource_group.rg.location
}
