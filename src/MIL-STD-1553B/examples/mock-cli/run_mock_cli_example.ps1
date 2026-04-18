param(
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepositoryRoot {
    $current = Split-Path -Parent $PSScriptRoot
    $current = Split-Path -Parent $current

    while ($null -ne $current -and $current -ne "") {
        if (Test-Path (Join-Path $current "AGENT_RULES.md")) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    throw "저장소 루트를 찾지 못했습니다."
}

$repositoryRoot = Get-RepositoryRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot "artifacts\mock-cli-example"
}

$publishRoot = Join-Path $OutputRoot "publish"
$logPath = Join-Path $OutputRoot "mock-cli-output.log"
$scenarioPath = Join-Path $repositoryRoot "examples\mock-cli\mock.scenario.json"
$cliProjectPath = Join-Path $repositoryRoot "src\dotnet\MilStd1553.Cli\MilStd1553.Cli.csproj"
$cliDllPath = Join-Path $publishRoot "MilStd1553.Cli.dll"

if (Test-Path $OutputRoot) {
    Remove-Item -LiteralPath $OutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

Write-Host "CLI example publish를 시작합니다..."
dotnet publish $cliProjectPath -c Debug -o $publishRoot
if ($LASTEXITCODE -ne 0) {
    throw "CLI publish가 실패했습니다."
}

if (-not (Test-Path $cliDllPath)) {
    throw "publish 결과에서 MilStd1553.Cli.dll을 찾지 못했습니다."
}

if (-not (Test-Path $scenarioPath)) {
    throw "example scenario 파일을 찾지 못했습니다: $scenarioPath"
}

Write-Host "mock example 세션을 실행합니다..."
$output = & dotnet $cliDllPath --scenario $scenarioPath --switch-bus B --poll-telemetry 2>&1
$exitCode = $LASTEXITCODE

$output | Set-Content -Path $logPath -Encoding UTF8
$output | ForEach-Object { Write-Host $_ }

if ($exitCode -ne 0) {
    throw "mock example 실행이 실패했습니다. exitCode=$exitCode"
}

$requiredPatterns = @(
    "sessionHandle=",
    "initialHealth activeBus=A",
    "switchedBus=B",
    "currentHealth activeBus=B",
    "telemetryCount=2",
    "telemetry[0] type=MessageFrame",
    "telemetry[1] type=BusSwitch",
    "sessionStopped=true"
)

foreach ($pattern in $requiredPatterns) {
    if (-not ($output | Select-String -SimpleMatch $pattern)) {
        throw "mock example 출력에서 필수 패턴을 찾지 못했습니다: $pattern"
    }
}

Write-Host "mock example 검증이 완료되었습니다."
Write-Host "logPath=$logPath"
