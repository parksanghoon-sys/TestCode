param(
    [int]$Port = 51498
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Wait-ForQueueEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string]$QueueUri
    )

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            return Invoke-RestMethod -Method Get -Uri $QueueUri -TimeoutSec 2
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Mock Experience API did not become ready in time."
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

function New-CommandContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandId,
        [Parameter(Mandatory = $true)]
        [string]$CorrelationId,
        [Parameter(Mandatory = $true)]
        [string]$IdempotencyKey,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Manifest
    )

    return @{
        identity = @{
            commandId = $CommandId
            correlationId = $CorrelationId
            idempotencyKey = $IdempotencyKey
        }
        origin = @{
            actorId = $Manifest.defaultActorId
            channel = 'wpf'
            stationId = $Manifest.stationId
        }
        clientTimestamp = [DateTimeOffset]::UtcNow.ToString('O')
        revisionRefs = $null
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$runtimeRoot = Join-Path $PSScriptRoot '.runtime'
$exampleProject = Join-Path $PSScriptRoot 'Mes.MockStation.Example.csproj'
$apiProject = Join-Path $repoRoot 'src\Mes.ExperienceApi\Mes.ExperienceApi.csproj'
$resolvedPort = Get-AvailablePort -PreferredPort $Port
$stdoutLog = Join-Path $runtimeRoot 'mock-api.stdout.log'
$stderrLog = Join-Path $runtimeRoot 'mock-api.stderr.log'

dotnet run --project $exampleProject -- --output-root $runtimeRoot | Out-Host

$manifestPath = Join-Path $runtimeRoot 'mock-station-scenario.json'
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$baseAddress = "http://127.0.0.1:$resolvedPort"
$aspNetCoreUrls = "$baseAddress/"
$queueUri = "$baseAddress/api/bff/operator-execution/stations/$($manifest.stationId)/work-queue"
$previousSqlitePath = $env:Mes__OperatorExecutionSqliteDatabasePath
$previousAspNetCoreUrls = $env:ASPNETCORE_URLS
$env:Mes__OperatorExecutionSqliteDatabasePath = $manifest.databaseFilePath
$env:ASPNETCORE_URLS = $aspNetCoreUrls
$apiProcess = $null

try {
    $apiProcess = Start-Process dotnet `
        -WorkingDirectory $repoRoot `
        -ArgumentList @('run', '--no-launch-profile', '--project', $apiProject) `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog `
        -PassThru

    $queue = Wait-ForQueueEndpoint -QueueUri $queueUri
    if ($queue.items.Count -lt 2) {
        throw "Expected at least two queue items in the mock scenario."
    }

    $startBody = @{
        context = New-CommandContext `
            -CommandId 'CMD-EXAMPLE-START-01' `
            -CorrelationId 'CORR-EXAMPLE-START-01' `
            -IdempotencyKey 'KEY-EXAMPLE-START-01' `
            -Manifest $manifest
        payload = @{
            productionOrderId = $manifest.queuedOperation.productionOrderId
            operationExecutionId = $manifest.queuedOperation.operationExecutionId
            operationSequence = $manifest.queuedOperation.operationSequence
            quantityUnit = $null
        }
    } | ConvertTo-Json -Depth 8

    $startResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/bff/operator-execution/commands/start-operation" `
        -ContentType 'application/json' `
        -Body $startBody
    if ($startResponse.status -ne 'Running') {
        throw "Queued example operation did not move to Running."
    }

    $materialBody = @{
        context = New-CommandContext `
            -CommandId 'CMD-EXAMPLE-MATERIAL-01' `
            -CorrelationId 'CORR-EXAMPLE-MATERIAL-01' `
            -IdempotencyKey 'KEY-EXAMPLE-MATERIAL-01' `
            -Manifest $manifest
        payload = @{
            operationExecutionId = $manifest.runningOperation.operationExecutionId
            wipUnitId = $manifest.runningOperation.wipUnitId
            materialLotId = $manifest.runningOperation.materialLotId
            materialCode = $manifest.runningOperation.materialCode
            quantity = @{
                value = 2
                unit = $manifest.runningOperation.materialQuantityUnit
            }
        }
    } | ConvertTo-Json -Depth 8

    $materialResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/bff/operator-execution/commands/material-consumption" `
        -ContentType 'application/json' `
        -Body $materialBody
    if ([decimal]$materialResponse.remainingQuantity.value -ne 10) {
        throw "Running example lot remaining quantity was not updated as expected."
    }

    $completeBody = @{
        context = New-CommandContext `
            -CommandId 'CMD-EXAMPLE-COMPLETE-01' `
            -CorrelationId 'CORR-EXAMPLE-COMPLETE-01' `
            -IdempotencyKey 'KEY-EXAMPLE-COMPLETE-01' `
            -Manifest $manifest
        payload = @{
            operationExecutionId = $manifest.runningOperation.operationExecutionId
            goodQuantity = @{
                value = 5
                unit = $manifest.runningOperation.quantityUnit
            }
            scrapQuantity = @{
                value = 1
                unit = $manifest.runningOperation.quantityUnit
            }
            completionMode = 'manual'
        }
    } | ConvertTo-Json -Depth 8

    $completeResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$baseAddress/api/bff/operator-execution/commands/complete-operation" `
        -ContentType 'application/json' `
        -Body $completeBody
    if ($completeResponse.status -ne 'Done') {
        throw "Running example operation did not move to Done."
    }

    $refreshedQueue = Invoke-RestMethod -Method Get -Uri $queueUri -TimeoutSec 5
    $queuedItem = $refreshedQueue.items | Where-Object { $_.operationExecutionId -eq $manifest.queuedOperation.operationExecutionId }
    $runningItem = $refreshedQueue.items | Where-Object { $_.operationExecutionId -eq $manifest.runningOperation.operationExecutionId }

    if ($queuedItem.status -ne 'Running') {
        throw "Queued example operation did not remain visible as Running after start."
    }

    if ($runningItem.status -ne 'Done') {
        throw "Running example operation did not remain visible as Done after completion."
    }

    Write-Host 'Mock station demo smoke test passed.'
}
catch {
    Write-Host "BaseAddress : $baseAddress"
    if (Test-Path $stdoutLog) {
        Write-Host '--- mock-api.stdout.log ---'
        Get-Content $stdoutLog | Out-Host
    }

    if (Test-Path $stderrLog) {
        Write-Host '--- mock-api.stderr.log ---'
        Get-Content $stderrLog | Out-Host
    }

    throw
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }

    if ($null -eq $previousSqlitePath) {
        Remove-Item Env:Mes__OperatorExecutionSqliteDatabasePath -ErrorAction SilentlyContinue
    }
    else {
        $env:Mes__OperatorExecutionSqliteDatabasePath = $previousSqlitePath
    }

    if ($null -eq $previousAspNetCoreUrls) {
        Remove-Item Env:ASPNETCORE_URLS -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
    }
}
