$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$choice = $null
while ($null -eq $choice) {
    $answer = Read-Host "Use RBAC for OpenTelemetry connection to appinsights? (true/false) [enter:true]"
    if ([string]::IsNullOrWhiteSpace($answer)) {
        $choice = "true"
        break
    }
    switch ($answer.Trim().ToLower()) {
        "true"  { $choice = "true" }
        "false" { $choice = "false" }
        default {
            Write-Host "Please enter 'true' or 'false'."
        }
    }
}

azd env set USE_RBAC $choice
Write-Host "USE_RBAC set to '$choice'."
