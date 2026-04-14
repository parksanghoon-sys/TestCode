# Codex-friendly local hook scripts

## Files

- `Install-GitHooks.ps1` installs `core.hooksPath=.githooks` for this clone.
- `Invoke-CodexPreCommit.ps1` formats staged C# files and validates staged XAML files.
- `Invoke-CodexPrePush.ps1` builds the repository and runs detected test projects.
- `codex-hook.config.json` toggles the default behavior.

## Install

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

## Run manually

```powershell
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1
```


## HookRunner

The pre-commit and pre-push scripts also invoke `wpf-dev-pack/hooks/WpfDevPack.HookRunner/WpfDevPack.HookRunner.csproj`, which adapts the helper utilities in `wpf-dev-pack/hooks/*.cs` to the Git hook workflow.
