[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $root = git rev-parse --show-toplevel 2>$null
    if (-not $root) { throw 'Unable to resolve Git repository root.' }
    return $root.Trim()
}

function Get-HookConfig([string]$RepoRoot) {
    $path = Join-Path $RepoRoot 'wpf-dev-pack/scripts/codex-hook.config.json'
    if (-not (Test-Path $path)) { return $null }
    return Get-Content $path -Raw | ConvertFrom-Json
}

function Get-StagedFiles {
    $files = git diff --cached --name-only --diff-filter=ACMR
    if (-not $files) { return @() }
    return $files | Where-Object { $_ -and $_.Trim() }
}

function Get-ClosestCsproj([string]$RepoRoot, [string]$RelativePath) {
    $current = Split-Path (Join-Path $RepoRoot $RelativePath) -Parent
    while ($current -and (Test-Path $current)) {
        $csproj = Get-ChildItem -Path $current -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($csproj) { return $csproj.FullName }
        if ($current -eq [System.IO.Path]::GetPathRoot($current)) { break }
        $current = Split-Path $current -Parent
    }
    return $null
}

function Invoke-ProcessChecked([string]$FilePath, [string[]]$ArgumentList) {
    Write-Host ("[codex-hooks] > {0} {1}" -f $FilePath, ($ArgumentList -join ' ')) -ForegroundColor DarkGray
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $FilePath $($ArgumentList -join ' ')"
    }
}

function Invoke-HookRunnerPreCommit([string]$RepoRoot, [string[]]$Files) {
    $project = Join-Path $RepoRoot 'wpf-dev-pack/hooks/WpfDevPack.HookRunner/WpfDevPack.HookRunner.csproj'
    if (-not (Test-Path $project)) {
        Write-Warning "HookRunner project not found: $project"
        return
    }

    $fileArg = [string]::Join(';', $Files)
    $configPath = Join-Path $RepoRoot 'wpf-dev-pack/scripts/codex-hook.config.json'
    Invoke-ProcessChecked -FilePath 'dotnet' -ArgumentList @('run', '--project', $project, '--', 'pre-commit', '--repo-root', $RepoRoot, '--config', $configPath, '--files', $fileArg)
}

function Format-CSharpFiles([string]$RepoRoot, [string[]]$Files) {
    $grouped = @{}
    foreach ($file in $Files) {
        if (-not $file.EndsWith('.cs', [System.StringComparison]::OrdinalIgnoreCase)) { continue }
        $project = Get-ClosestCsproj -RepoRoot $RepoRoot -RelativePath $file
        if (-not $project) {
            Write-Warning "Skipping format; no .csproj found for $file"
            continue
        }
        if (-not $grouped.ContainsKey($project)) {
            $grouped[$project] = [System.Collections.Generic.List[string]]::new()
        }
        $grouped[$project].Add((Join-Path $RepoRoot $file))
    }

    foreach ($entry in $grouped.GetEnumerator()) {
        $args = @('format', $entry.Key, '--no-restore', '--verbosity', 'minimal', '--include') + $entry.Value
        Invoke-ProcessChecked -FilePath 'dotnet' -ArgumentList $args
    }
}

$repoRoot = Get-RepoRoot
$config = Get-HookConfig -RepoRoot $repoRoot
$stagedFiles = Get-StagedFiles

if ($stagedFiles.Count -eq 0) {
    Write-Host '[codex-hooks] No staged files. Skipping pre-commit checks.' -ForegroundColor DarkGray
    exit 0
}

Write-Host '[codex-hooks] Running pre-commit checks...' -ForegroundColor Cyan

Invoke-HookRunnerPreCommit -RepoRoot $repoRoot -Files $stagedFiles

if ($null -eq $config -or $config.preCommit.formatCSharp) {
    Format-CSharpFiles -RepoRoot $repoRoot -Files $stagedFiles
}

if ($null -eq $config -or $config.preCommit.restageFiles) {
    $restage = $stagedFiles | Where-Object { $_ -match '\.(cs|xaml)$' }
    if ($restage.Count -gt 0) {
        & git add -- $restage
        if ($LASTEXITCODE -ne 0) {
            throw 'git add failed while restaging formatted files.'
        }
    }
}

Write-Host '[codex-hooks] Pre-commit checks passed.' -ForegroundColor Green
