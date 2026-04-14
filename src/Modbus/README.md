[🇰🇷 한국어](./README.ko.md)

# dotnet-with-codex

.NET and WPF template repository for **OpenAI Codex**.

## Overview

This repository is organized for Codex-native workflows:

- `AGENTS.md` for project instructions
- `.agents/skills/` for reusable repository skills
- `.codex/config.toml` for project-scoped Codex configuration
- `wpf-dev-pack/` for WPF-focused guidance, skills, helpers, and optional custom agents

## Repository layout

### [wpf-dev-pack](./wpf-dev-pack)

WPF-oriented Codex workspace with focused guidance for project creation, MVVM, styling, rendering, and review tasks.

## Requirements

- Codex CLI or another Codex-supported client
- .NET SDK for local helper scripts under `wpf-dev-pack/hooks`
- Optional MCP servers for docs lookup, semantic code search, and C# tooling

## Install Codex

```bash
npm i -g @openai/codex
codex
```

## Recommended usage

1. Open the repository in Codex.
2. Let Codex read the nearest `AGENTS.md` file.
3. Keep shared repository skills in `.agents/skills/`.
4. Use `wpf-dev-pack/` when you want WPF-specific skills and custom agents.
5. Add project-specific MCP servers through `.codex/config.toml` or `codex mcp add`.

## Hook setup

Install the local Git hook workflow once per clone:

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

What gets wired:

- `.githooks/pre-commit` → `wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1`
- `.githooks/pre-push` → `wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1`
- behavior toggles → `wpf-dev-pack/scripts/codex-hook.config.json`

`pre-commit` formats staged C# files and validates staged XAML. `pre-push` builds the repo and runs discovered test projects.

## Notes

- The clean release removes legacy plugin-oriented files and broken release/version hooks.
- `wpf-dev-pack/hooks/` keeps optional advisory utilities, while `wpf-dev-pack/scripts/` contains the active Codex-friendly Git hook workflow.

## License

MIT.


## Git hook workflow

Install hooks with `pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1`. The active hook path uses `.githooks/*`, PowerShell entry scripts, and `wpf-dev-pack/hooks/WpfDevPack.HookRunner/` to apply the helper checks from `wpf-dev-pack/hooks/*.cs`.


HookRunner now prints skill reasons in the form `[matched: ...]` so Codex users can see why a skill was suggested from file content or path heuristics.
