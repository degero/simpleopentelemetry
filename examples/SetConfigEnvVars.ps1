# Set-ConfigEnvVars.ps1
# Sets session environment variables from a JSON config using ASP.NET IConfiguration
# key conventions: ':' as separator, numeric indices for arrays.

function ConvertTo-FlatEnvVars {
    param(
        [Parameter(Mandatory)]
        $Node,
        [string]$Prefix = ""
    )

    if ($Node -is [System.Collections.Hashtable] -or $Node -is [System.Management.Automation.PSCustomObject]) {
        $dict = if ($Node -is [System.Management.Automation.PSCustomObject]) {
            $Node.PSObject.Properties
        } else {
            $Node.GetEnumerator()
        }

        foreach ($entry in $dict) {
            $key   = $entry.Name
            $value = $entry.Value
            $fullKey = if ($Prefix) { "$Prefix`:$key" } else { $key }
            ConvertTo-FlatEnvVars -Node $value -Prefix $fullKey
        }
    }
    elseif ($Node -is [System.Object[]] -or $Node -is [System.Collections.ArrayList]) {
        $index = 0
        foreach ($item in $Node) {
            $fullKey = "$Prefix`:$index"
            ConvertTo-FlatEnvVars -Node $item -Prefix $fullKey
            $index++
        }
    }
    else {
        # Leaf value — emit the key/value pair
        [PSCustomObject]@{
            Key   = $Prefix
            Value = if ($null -eq $Node) { "" } else { [string]$Node }
        }
    }
}

# ── Load JSON ──────────────────────────────────────────────────────────────────
$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$jsonPath   = Join-Path $scriptDir "envvars.json"

if (-not (Test-Path $jsonPath)) {
    Write-Error "Could not find '$jsonPath'. Place envvars.json next to this script."
    exit 1
}

$json   = Get-Content $jsonPath -Raw
$config = $json | ConvertFrom-Json

# ── Flatten ────────────────────────────────────────────────────────────────────
$pairs = ConvertTo-FlatEnvVars -Node $config

# ── Apply to current session ───────────────────────────────────────────────────
$count = 0
foreach ($pair in $pairs) {
    [System.Environment]::SetEnvironmentVariable($pair.Key, $pair.Value, "Process")
    Write-Host "  SET  $($pair.Key) = $($pair.Value)"
    $count++
}

Write-Host ""
Write-Host "$count environment variable(s) set for this session." -ForegroundColor Green
Write-Host "Note: variables are scoped to this PowerShell process only." -ForegroundColor DarkGray

# ── Quick verification helper ──────────────────────────────────────────────────
# Uncomment to dump all vars that start with "SimpleOpenTelemetry" or "OTEL"
#
# Write-Host ""
# Write-Host "=== Verification ===" -ForegroundColor Cyan
# Get-ChildItem Env: | Where-Object { $_.Name -match '^(OTEL|SimpleOpenTelemetry)' } |
#     Sort-Object Name | Format-Table -AutoSize