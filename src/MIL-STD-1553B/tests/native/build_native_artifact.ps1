param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("TestsExe", "NativeDll", "PlaceholderDll")]
    [string]$ArtifactType = "TestsExe"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..")).Path
$artifactDirectory = Join-Path $scriptDirectory "artifacts"

New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null

$vcVarsPath = Get-ChildItem "C:\Program Files\Microsoft Visual Studio" -Recurse -Filter "vcvars64.bat" -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $vcVarsPath)
{
    throw "vcvars64.bat 파일을 찾지 못했습니다. Visual Studio C++ 도구 설치를 확인해 주세요."
}

$coreSources = @(
    (Join-Path $repositoryRoot "src\native\Domain\CommandWord.cpp"),
    (Join-Path $repositoryRoot "src\native\Domain\StatusWord.cpp"),
    (Join-Path $repositoryRoot "src\native\Application\BusMonitorService.cpp"),
    (Join-Path $repositoryRoot "src\native\Application\BusControllerService.cpp"),
    (Join-Path $repositoryRoot "src\native\Infrastructure\JsonLinesBusEventStore.cpp"),
    (Join-Path $repositoryRoot "src\native\Infrastructure\SimulatorBusAdapter.cpp"),
    (Join-Path $repositoryRoot "src\native\Interop\NativeSessionApi.cpp")
)

$sources = @($coreSources)
$outputPath = Join-Path $artifactDirectory "NativeTests.exe"
$compileModeArgument = ""

if ($ArtifactType -eq "TestsExe")
{
    $sources += (Join-Path $repositoryRoot "tests\native\NativeTests.cpp")
    $outputPath = Join-Path $artifactDirectory "NativeTests.exe"
}
elseif ($ArtifactType -eq "NativeDll")
{
    $outputPath = Join-Path $artifactDirectory "MilStd1553.Native.dll"
    $compileModeArgument = "/LD"
}
else
{
    $sources = @((Join-Path $repositoryRoot "tests\native\PlaceholderExports.cpp"))
    $outputPath = Join-Path $artifactDirectory "MilStd1553.Native.dll"
    $compileModeArgument = "/LD"
}

$quotedSources = ($sources | ForEach-Object { '"' + $_ + '"' }) -join " "
$includeDirectory = Join-Path $repositoryRoot "src\native"

$compileCommand =
    'call "' + $vcVarsPath + '" >nul && cl /nologo /utf-8 /std:c++20 /EHsc ' + $compileModeArgument +
    ' /I "' + $includeDirectory + '" ' + $quotedSources + ' /Fe:"' + $outputPath + '"'

$null = cmd.exe /d /s /c $compileCommand | Out-Host
if ($LASTEXITCODE -ne 0)
{
    throw "네이티브 $ArtifactType 빌드에 실패했습니다."
}

Write-Output $outputPath
