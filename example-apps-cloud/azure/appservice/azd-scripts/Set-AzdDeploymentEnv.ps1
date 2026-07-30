$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$resourceGroupName = 'rg-soteltestazure'
$webAppName = 'soteltestazure'

azd env set AZURE_RESOURCE_GROUP $resourceGroupName
azd env set AZURE_WEBAPP_NAME $webAppName

Write-Host "Configured azd environment with resource group '$resourceGroupName' and web app '$webAppName'."
