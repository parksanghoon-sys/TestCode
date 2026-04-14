[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$RepoRoot = git rev-parse --show-toplevel 2>$null
if (-not $RepoRoot) {
    throw 'Run this script inside a Git repository.'
}

$HooksPath = Join-Path $RepoRoot '.githooks'
if (-not (Test-Path $HooksPath)) {
    throw "Missing hooks directory: $HooksPath"
}

$current = git config --local --get core.hooksPath 2>$null
if ($current -and -not $Force -and $current -ne '.githooks') {
    throw "core.hooksPath is already set to '$current'. Use -Force to overwrite it."
}

git config --local core.hooksPath .githooks
Write-Host '[codex-hooks] Git hooks installed.' -ForegroundColor Green
Write-Host 'Next step: test with git commit or run the scripts directly:' -ForegroundColor Cyan
Write-Host '  wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1'
Write-Host '  wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1'
