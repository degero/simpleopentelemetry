$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$existingLocation = [Environment]::GetEnvironmentVariable('AZURE_LOCATION')
if (-not [string]::IsNullOrWhiteSpace($existingLocation)) {
    Write-Host "Using existing AZURE_LOCATION=$existingLocation"
    exit 0
}

$locations = az account list-locations --query "[].name" -o tsv | Sort-Object
if (-not $locations -or $locations.Count -eq 0) {
    throw 'Unable to retrieve Azure locations. Ensure Azure CLI is installed and authenticated.'
}

Write-Host 'Azure regions available for deployment:'
for ($i = 0; $i -lt $locations.Count; $i++) {
    Write-Host ("[{0}] {1}" -f ($i + 1), $locations[$i])
}

$selection = Read-Host 'Choose a region number'
if ($selection -notmatch '^\d+$') {
    throw 'Invalid selection. Please enter a numeric value.'
}

$index = [int]$selection - 1
if ($index -lt 0 -or $index -ge $locations.Count) {
    throw 'Selection is out of range.'
}

$chosenLocation = $locations[$index]
Write-Host "Selected Azure region: $chosenLocation"
azd env set AZURE_LOCATION $chosenLocation
