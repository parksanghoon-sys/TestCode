$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$executablePath = & (Join-Path $scriptDirectory "build_native_artifact.ps1") -ArtifactType TestsExe

& $executablePath
if ($LASTEXITCODE -ne 0)
{
    throw "네이티브 테스트 실행에 실패했습니다."
}
