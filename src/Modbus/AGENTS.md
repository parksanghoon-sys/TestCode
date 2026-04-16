# dotnet-with-codex

## Repository purpose

This repository provides reusable Codex-first guidance for .NET, WPF, and Avalonia work.

- Root-level `.agents/skills/` contains shared repository skills.
- `rules/` contains reusable implementation constraints and preferences.
- `wpf-dev-pack/` contains the WPF-focused workspace.

## How Codex should use this repository

- Read this file before starting work in the repository.
- Prefer the nearest `AGENTS.md` when working in a subdirectory.
- Prefer repo-local skills when the task matches an existing skill.
- Keep reusable workflows under `.agents/skills/<skill-name>/SKILL.md`.
- Keep repository guidance short and push detailed process into skill docs or focused rules.
- When writing or modifying C# code, you must follow `rules/dotnet/csharp/xml-doc-comments.md`.
- Every introduced or changed C# class and function must include Korean XML documentation comments. Treat this as mandatory, not optional.
- Prefer authored methods, constructors, and public APIs with five or fewer input parameters.
- If more than five inputs are genuinely needed, strongly prefer grouping them into a parameter object, request record, or value object unless an external framework or library signature forces the shape.

## Shared skill areas

### Avalonia and shared UI skills

| Skill | Purpose |
|---|---|
| `configuring-avalonia-dependency-injection` | Avalonia GenericHost / DI setup |
| `designing-avalonia-customcontrol-architecture` | Avalonia custom control structure |
| `structuring-avalonia-projects` | Avalonia solution layout |
| `using-avalonia-collectionview` | `DataGridCollectionView`, ReactiveUI patterns |
| `fixing-avaloniaui-radialgradientbrush` | Avalonia brush compatibility issues |
| `converting-html-css-to-wpf-xaml` | HTML/CSS to WPF XAML conversion |

### Manufacturing and solution design skills

| Skill | Purpose |
|---|---|
| `designing-mes-systems` | MES capability scoping, execution modeling, system boundary definition, and rollout-oriented architecture guidance |


## Local automation workflow

- Git hook entrypoints live under `.githooks/`.
- Install them once per clone with `pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1`.
- `pre-commit` runs `wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1`.
- `pre-push` runs `wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1`.
- Hook behavior is configured in `wpf-dev-pack/scripts/codex-hook.config.json`.
- When Git hooks are unavailable, run the same scripts manually before asking Codex for a final review.

## Maintenance notes

- Prefer portable Codex conventions over client-specific wrappers.
- Keep helper automation optional unless it is validated in this repository.
- Remove stale docs or broken automation when they no longer match the current structure.


## Bridge to WPF/.NET skills

- Generic .NET and WPF implementation skills currently live under `wpf-dev-pack/.agents/skills/`.
- When a rule under `rules/dotnet/` references a skill name, resolve it against `wpf-dev-pack/.agents/skills/<skill-name>/`.
- For .NET application work, prefer opening or working from `wpf-dev-pack/` so the nearest `AGENTS.md` and skill index are loaded naturally.


## Git hook workflow

Install hooks with `pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1`. The active hook path uses `.githooks/*`, PowerShell entry scripts, and `wpf-dev-pack/hooks/WpfDevPack.HookRunner/` to apply the helper checks from `wpf-dev-pack/hooks/*.cs`.


HookRunner now prints skill reasons in the form `[matched: ...]` so Codex users can see why a skill was suggested from file content or path heuristics.
