$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$projectDir = Resolve-Path -LiteralPath (Join-Path $scriptDir 'app\')

Push-Location $projectDir
try {
    if (Test-Path '..\publish') {
        Remove-Item '..\publish' -Recurse -Force
    }

    dotnet publish -c Release -o ..\publish
}
finally {
    Pop-Location
}

$dockerDir = Resolve-Path -LiteralPath (Join-Path $scriptDir 'localdev-docker\')
Push-Location $dockerDir
try {
    docker compose build --no-cache
    docker compose up -d

    Write-Host 'press any key to exit'
    [void][System.Console]::ReadKey($true)

    docker compose down
}
finally {
    Pop-Location
}
