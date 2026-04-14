# CHANGELOG_AGENT

## 2026-04-14

- Added `docs/mes/architecture-blueprint.md` with baseline MES scope, system boundary, logical modules, deployment topology, canonical objects, release plan, and open questions.
- Added `docs/mes/implementation-roadmap.md` with phased delivery guidance, workstreams, initial team shape, early actions, and risks.
- Updated the MES architecture to support both `WPF Station Client` and `Web Portal` over a shared `Experience API / BFF` boundary.
- Tightened the baseline by defining BFF command ownership, an offline authority matrix, a Release 1 WMS-lite reconciliation model, and Release 1 MES quality authority.
- Added `docs/mes/client-channel-matrix.md` to allocate pilot workflows across `WPF-first`, `Web-first`, and `Shared` channels.
- Added `docs/mes/command-event-catalog.md` to define the initial BFF command catalog, domain events, and integration events for Release 1.
- Added `CURRENT_PLAN.md`, `TODOS.md`, and `DECISIONS.md` to support the repository execution loop for the new MES project kickoff.
- Added `Mes.slnx`, `src/Mes.Domain`, and `tests/Mes.Domain.Tests` as the first executable MES domain foundation and validated them with `dotnet test Mes.slnx`.
- Added `rules/dotnet/csharp/xml-doc-comments.md` and updated `AGENTS.md` so new or modified C# classes and functions require Korean XML documentation comments.
- Applied Korean XML documentation comments to the current `Mes.Domain` and `Mes.Domain.Tests` classes and methods.
- Strengthened both `AGENTS.md` and `wpf-dev-pack/AGENTS.md` so the Korean XML documentation requirement is stated explicitly as mandatory for introduced or changed C# classes and functions.
