# Contributing to dotnet-with-codex

Thank you for contributing.

## Workflow

### 1. Clone

```bash
git clone <your-fork-or-template-url>
cd dotnet-with-codex
```

### 2. Create a branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-bug-fix
```

### 3. Make focused changes

- Keep repository instructions in `AGENTS.md` files concise.
- Keep reusable workflows under `.agents/skills/<skill-name>/SKILL.md`.
- Prefer small, reviewable commits.
- Remove broken automation rather than preserving dead files.

### 4. Commit

```bash
git add .
git commit -m "feat: add new WPF skill"
```

Suggested commit prefixes:

- `feat:` new capability
- `fix:` bug fix
- `docs:` documentation change
- `refactor:` structural cleanup
- `chore:` maintenance work

### 5. Open a pull request

1. Push your branch.
2. Open a pull request against your main integration branch.
3. Describe what changed, why, and how you verified it.

## Skill authoring guidelines

- Keep `SKILL.md` task-focused.
- Put examples, scripts, and references next to the skill that uses them.
- Write descriptions so Codex can tell when the skill should and should not trigger.

## Questions

Use your repository issue tracker or project discussion channel.
