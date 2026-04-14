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

function Invoke-ProcessChecked([string]$FilePath, [string[]]$ArgumentList) {
    Write-Host ("[codex-hooks] > {0} {1}" -f $FilePath, ($ArgumentList -join ' ')) -ForegroundColor DarkGray
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $FilePath $($ArgumentList -join ' ')"
    }
}

function Invoke-HookRunnerPrePush([string]$RepoRoot) {
    $project = Join-Path $RepoRoot 'wpf-dev-pack/hooks/WpfDevPack.HookRunner/WpfDevPack.HookRunner.csproj'
    if (-not (Test-Path $project)) {
        Write-Warning "HookRunner project not found: $project"
        return
    }

    $configPath = Join-Path $RepoRoot 'wpf-dev-pack/scripts/codex-hook.config.json'
    Invoke-ProcessChecked -FilePath 'dotnet' -ArgumentList @('run', '--project', $project, '--', 'pre-push', '--repo-root', $RepoRoot, '--config', $configPath)
}

function Get-BuildTargets([string]$RepoRoot) {
    $solutions = Get-ChildItem -Path $RepoRoot -Filter *.sln -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/]bin[\\/]|[\\/]obj[\\/]' }
    if ($solutions.Count -gt 0) { return $solutions.FullName }

    $projects = Get-ChildItem -Path $RepoRoot -Filter *.csproj -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/]bin[\\/]|[\\/]obj[\\/]' }
    return $projects.FullName
}

function Get-TestProjects([string]$RepoRoot) {
    return Get-ChildItem -Path $RepoRoot -Filter *.csproj -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notmatch '[\\/]bin[\\/]|[\\/]obj[\\/]' -and
            ($_.Name -match 'test' -or $_.DirectoryName -match 'test')
        } |
        Select-Object -ExpandProperty FullName
}

$repoRoot = Get-RepoRoot
$config = Get-HookConfig -RepoRoot $repoRoot
$configuration = if ($config -and $config.prePush.configuration) { [string]$config.prePush.configuration } else { 'Debug' }

Write-Host '[codex-hooks] Running pre-push checks...' -ForegroundColor Cyan

Invoke-HookRunnerPrePush -RepoRoot $repoRoot

if ($null -eq $config -or $config.prePush.build) {
    $targets = Get-BuildTargets -RepoRoot $repoRoot
    if ($targets.Count -eq 0) {
        Write-Warning 'No .sln or .csproj files found. Skipping build.'
    }
    else {
        foreach ($target in $targets) {
            try {
                Invoke-ProcessChecked -FilePath 'dotnet' -ArgumentList @('build', $target, '-c', $configuration, '--nologo')
            }
            catch {
                Write-Host '[WPF Dev Pack] Build failed. Review CS/NU/XAML diagnostics above and consult related skills in .agents/skills/.' -ForegroundColor Yellow
                throw
            }
        }
    }
}

if ($null -eq $config -or $config.prePush.test) {
    $tests = Get-TestProjects -RepoRoot $repoRoot
    if ($tests.Count -eq 0) {
        Write-Host '[codex-hooks] No test projects found. Skipping dotnet test.' -ForegroundColor DarkYellow
    }
    else {
        foreach ($testProject in $tests) {
            try {
                Invoke-ProcessChecked -FilePath 'dotnet' -ArgumentList @('test', $testProject, '-c', $configuration, '--no-build', '--nologo')
            }
            catch {
                Write-Host '[WPF Dev Pack] Tests failed. Review output and relevant rules/skills before pushing.' -ForegroundColor Yellow
                throw
            }
        }
    }
}

Write-Host '[codex-hooks] Pre-push checks passed.' -ForegroundColor Green
