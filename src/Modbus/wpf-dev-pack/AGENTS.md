# WPF Dev Pack for Codex

## Purpose

This directory contains a WPF-focused Codex workspace: instructions, reusable skills, rules, helper scripts, and optional custom agents.

## Core operating rules

### MVVM approach: View First

- Use **View First MVVM** by default.
- The View is created first and determines its ViewModel.
- View wiring depends on the selected MVVM framework.
- Do not introduce ViewModelLocator unless the user explicitly requests compatibility with an existing codebase.

### Essential rules

These should survive context loss:

1. No ViewModelLocator by default. Use DI + DataTemplate mapping.
2. No `System.Windows` dependencies in ViewModel unless interop is explicitly required.
3. Freeze `Freezable` objects where practical.
4. Keep `Generic.xaml` as a merged-dictionary hub.
5. Verify unfamiliar API signatures with authoritative docs before coding.
6. Prefer the most specific matching skill.

## .NET defaults

- Minimum target for newly generated WPF apps: **.NET 8**.
- If the user specifies a target version, follow it.
- Otherwise use the latest stable version supported by the user environment.

## Skill routing

### Use these skills first

- Project creation → `.agents/skills/make-wpf-project`
- CustomControl authoring → `.agents/skills/make-wpf-custom-control`
- UserControl scaffolding → `.agents/skills/make-wpf-usercontrol`
- ViewModel scaffolding → `.agents/skills/make-wpf-viewmodel`
- MVVM review or implementation → `.agents/skills/implementing-communitytoolkit-mvvm`
- Styling and templates → `.agents/skills/customizing-controltemplate`, `.agents/skills/managing-styles-resourcedictionary`
- Rendering/performance → `.agents/skills/rendering-with-drawingcontext`, `.agents/skills/rendering-wpf-high-performance`

## Codex notes

- Codex discovers project instructions through `AGENTS.md`.
- Repository skills live under `.agents/skills/`.
- Optional custom agents live under `.codex/agents/`.
- Use `.codex/config.toml` when you need project-scoped Codex configuration such as MCP servers or agent limits.


## Skill index

- Browse `wpf-dev-pack/.agents/skills/README.md` (or `README.ko.md`) for topic-grouped skill discovery.
- When a rule or document names a skill without a path, resolve it under `wpf-dev-pack/.agents/skills/<skill-name>/`.


## Local hook workflow

- Install Git hooks once per clone: `pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1`
- `pre-commit` formats staged C# files and validates staged XAML files.
- `pre-push` builds the repository and runs detected test projects.
- Tune behavior in `wpf-dev-pack/scripts/codex-hook.config.json`.
- If a task changes C# or XAML and hooks are not installed, run the matching script manually before finishing:
  - `wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1`
  - `wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1`


## Git hook workflow

Install hooks with `pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1`. The active hook path uses `.githooks/*`, PowerShell entry scripts, and `wpf-dev-pack/hooks/WpfDevPack.HookRunner/` to apply the helper checks from `wpf-dev-pack/hooks/*.cs`.


HookRunner now prints skill reasons in the form `[matched: ...]` so Codex users can see why a skill was suggested from file content or path heuristics.
