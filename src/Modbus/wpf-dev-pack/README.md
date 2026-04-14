[🇰🇷 한국어](./README.ko.md)

# wpf-dev-pack

WPF development pack for **OpenAI Codex**.

## What this pack contains

- `AGENTS.md` for WPF-specific project instructions
- `.agents/skills/` for reusable WPF workflows
- `.codex/agents/` for optional project-scoped custom agents
- `rules/` for reusable WPF constraints and patterns
- `hooks/` for optional advisory utilities
- `scripts/` for the active local Git hook workflow

## Requirements

- Codex CLI or another Codex-supported client
- .NET SDK for local helper utilities
- Optional MCP servers for framework docs, code search, and C# intelligence

## Install Codex

```bash
npm i -g @openai/codex
codex
```

## Use this pack

- Open `wpf-dev-pack/` in Codex.
- Let Codex read `AGENTS.md`.
- Keep reusable workflows under `.agents/skills/`.
- Use `.codex/agents/` when you want narrow, repeatable specialist agents.

## Common skills

- `make-wpf-project`
- `make-wpf-custom-control`
- `make-wpf-usercontrol`
- `make-wpf-viewmodel`
- `implementing-communitytoolkit-mvvm`
- `customizing-controltemplate`
- `rendering-with-drawingcontext`
- `rendering-wpf-high-performance`

## Local Git hook workflow

Install once per clone:

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

This wires:

- `.githooks/pre-commit` → `scripts/Invoke-CodexPreCommit.ps1`
- `.githooks/pre-push` → `scripts/Invoke-CodexPrePush.ps1`

Optional advisory helper sources remain under `hooks/`, while the active hook path lives under `scripts/`.


## Git hook workflow

Install hooks with `pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1`. The active hook path uses `.githooks/*`, PowerShell entry scripts, and `wpf-dev-pack/hooks/WpfDevPack.HookRunner/` to apply the helper checks from `wpf-dev-pack/hooks/*.cs`.


HookRunner now prints skill reasons in the form `[matched: ...]` so Codex users can see why a skill was suggested from file content or path heuristics.
