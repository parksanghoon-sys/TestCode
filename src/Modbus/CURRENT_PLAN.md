# Current Plan

## Current Goal

Turn the MES baseline into an implementation-ready pilot slice by stabilizing the foundational domain model and deriving the first logical data model and BFF payload contracts.

## Current State

- Repo-local MES skill exists under `.agents/skills/designing-mes-systems/`.
- The MES baseline under `docs/mes/` now defines explicit BFF/Edge command ownership, offline authority, release-1 material reconciliation, and release-1 quality authority.
- Detailed pilot artifacts now exist for role/channel allocation and the initial command/event catalog.
- A first `Mes.Domain` foundation now exists under `src/Mes.Domain` for production orders, operation execution, material lots, quality records, override requests, and WIP units.
- Baseline domain tests now pass in `tests/Mes.Domain.Tests`.
- The repository now carries an explicit rule that new or modified C# classes and functions must include Korean XML documentation comments, and that requirement is now stated directly in both the root and `wpf-dev-pack` AGENT entry points.
- Project-specific manufacturing assumptions are still provisional and must be validated against one pilot line.

## Next Meaningful Work Unit

Select one pilot workflow slice from the implemented domain foundation, align domain vocabulary with the architecture documents, then derive the first logical data model and BFF/API payload definitions for that slice.

## Validation Path

- Compare `docs/mes/command-event-catalog.md` and `src/Mes.Domain/Statuses/DomainStatuses.cs` to keep names and states aligned.
- Walk one pilot workflow end to end from `ProductionOrder` through `OperationExecution`, `MaterialLot`, and `QualityRecord`.
- Extend domain tests for at least one happy path and one exception path in the selected workflow slice.
- Derive BFF/API payloads that stay consistent with the validated workflow, offline authority rules, and WPF/Web channel split.
