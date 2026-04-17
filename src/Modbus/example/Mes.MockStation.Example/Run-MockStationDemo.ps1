param(
    [int]$Port = 51398,
    [switch]$LaunchWpf
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-SingleQuotedLiteral {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return "'" + $Value.Replace("'", "''") + "'"
}

function Test-PortInUse {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
        $listener.Start()
        $listener.Stop()
        return $false
    }
    catch {
        return $true
    }
}

function Get-AvailablePort {
    param(
        [Parameter(Mandatory = $true)]
        [int]$PreferredPort
    )

    if (-not (Test-PortInUse -Port $PreferredPort)) {
        return $PreferredPort
    }

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $freePort = $listener.LocalEndpoint.Port
    $listener.Stop()
    return $freePort
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$runtimeRoot = Join-Path $PSScriptRoot '.runtime'
$exampleProject = Join-Path $PSScriptRoot 'Mes.MockStation.Example.csproj'
$apiProject = Join-Path $repoRoot 'src\Mes.ExperienceApi\Mes.ExperienceApi.csproj'
$wpfProject = Join-Path $repoRoot 'src\Mes.Client.Wpf\Mes.Client.Wpf.csproj'
$resolvedPort = Get-AvailablePort -PreferredPort $Port

dotnet run --project $exampleProject -- --output-root $runtimeRoot | Out-Host

$manifestPath = Join-Path $runtimeRoot 'mock-station-scenario.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$baseAddress = "http://127.0.0.1:$resolvedPort"
$aspNetCoreUrls = "$baseAddress/"

$apiCommand = @"
`$env:Mes__OperatorExecutionSqliteDatabasePath = $(Get-SingleQuotedLiteral $manifest.databaseFilePath)
`$env:ASPNETCORE_URLS = $(Get-SingleQuotedLiteral $aspNetCoreUrls)
dotnet run --no-launch-profile --project $(Get-SingleQuotedLiteral $apiProject)
"@

Start-Process pwsh `
    -WorkingDirectory $repoRoot `
    -ArgumentList '-NoLogo', '-NoExit', '-Command', $apiCommand | Out-Null

Write-Host ''
Write-Host "Mock Experience API started in a new PowerShell window."
Write-Host "BaseAddress : $baseAddress"
Write-Host "StationId   : $($manifest.stationId)"
Write-Host "ActorId     : $($manifest.defaultActorId)"
Write-Host "Queued op   : $($manifest.queuedOperation.operationExecutionId)"
Write-Host "Running op  : $($manifest.runningOperation.operationExecutionId)"
Write-Host "Running WIP : $($manifest.runningOperation.wipUnitId)"
Write-Host "Running lot : $($manifest.runningOperation.materialLotId)"
if ($resolvedPort -ne $Port) {
    Write-Host "Requested port $Port was busy, so the script used $resolvedPort instead."
}

if ($LaunchWpf) {
    $wpfCommand = @"
`$env:Mes__OperatorExecutionBff__BaseAddress = $(Get-SingleQuotedLiteral $baseAddress)
`$env:Mes__OperatorExecutionBff__DefaultStationId = $(Get-SingleQuotedLiteral $manifest.stationId)
`$env:Mes__OperatorExecutionBff__DefaultActorId = $(Get-SingleQuotedLiteral $manifest.defaultActorId)
`$env:Mes__OperatorExecutionBff__DefaultCompletionQuantityUnit = $(Get-SingleQuotedLiteral 'EA')
dotnet run --no-launch-profile --project $(Get-SingleQuotedLiteral $wpfProject)
"@

    Start-Process pwsh `
        -WorkingDirectory $repoRoot `
        -ArgumentList '-NoLogo', '-NoExit', '-Command', $wpfCommand | Out-Null

    Write-Host "Mock WPF shell also started in a new PowerShell window."
}
else {
    Write-Host ''
    Write-Host "To launch the WPF shell too, run:"
    Write-Host "  pwsh -File `"$PSScriptRoot\Run-MockStationDemo.ps1`" -LaunchWpf"
}
