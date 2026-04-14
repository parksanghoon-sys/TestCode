# Hook utilities and active workflow

This folder now contains the active hook runner for the WPF dev pack.

## What is active now

The actual Git hook workflow is wired through:

- `../../.githooks/pre-commit`
- `../../.githooks/pre-push`
- `../scripts/Install-GitHooks.ps1`
- `../scripts/Invoke-CodexPreCommit.ps1`
- `../scripts/Invoke-CodexPrePush.ps1`
- `../scripts/codex-hook.config.json`
- `./WpfDevPack.HookRunner/WpfDevPack.HookRunner.csproj`

Install it once per clone:

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

## What is auto-invoked now

The Git hooks now call the HookRunner project, which adapts the helper utilities in this folder to the Git hook pipeline.

Active in `pre-commit`:

- `CodeFormatter.cs` intent -> C# formatting remains enforced via `dotnet format`
- `XamlValidator.cs` intent -> XAML/XML validation and WPF-specific warnings
- `MvvmViolationDetector.cs` intent -> ViewModel layer misuse warnings
- `WpfKeywordDetector.cs` intent -> recommends matching `.agents/skills/*`
- `McpDependencyChecker.cs` intent -> warns when recommended MCP references are missing

Active in `pre-push`:

- `BuildErrorDiagnoser.cs` intent -> pre-push diagnostics messaging around build/test failures
- `McpDependencyChecker.cs` intent -> warns when recommended MCP references are missing

Still advisory only:

- `HandMirrorReminder.cs` is Codex/MCP query guidance and is not suitable for Git-triggered execution, so it remains documentation/reference only.

## Manual execution

You can run the HookRunner directly:

```powershell
# Pre-commit style checks for selected files
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1

# Pre-push validation
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1
```


HookRunner now prints skill reasons in the form `[matched: ...]` so Codex users can see why a skill was suggested from file content or path heuristics.
