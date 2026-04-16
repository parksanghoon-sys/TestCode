# Current Plan

## Current Goal

Turn the MES baseline into an implementation-ready pilot slice by stabilizing the foundational domain model and deriving the first logical data model and BFF payload contracts.

## Current State

- Repo-local MES skill exists under `.agents/skills/designing-mes-systems/`.
- The MES baseline under `docs/mes/` now defines explicit BFF/Edge command ownership, offline authority, release-1 material reconciliation, and release-1 quality authority.
- Detailed pilot artifacts now exist for role/channel allocation and the initial command/event catalog.
- A first `Mes.Domain` foundation now exists under `src/Mes.Domain` for production orders, operation execution, material lots, quality records, override requests, and WIP units.
- Baseline domain tests now pass in `tests/Mes.Domain.Tests`.
- The first implementation-ready pilot slice is now explicitly documented as operator execution with material consumption and a quality hold gate.
- Initial logical data model and BFF payload drafts now exist for that selected slice under `docs/mes/`.
- A new `Mes.Application.Contracts` project now defines concrete command, response, query, notification, and endpoint-signature types for the selected operator-execution slice.
- A first persistence-oriented SQL draft for the selected slice now exists in `docs/mes/persistence-schema-slice-01.sql`.
- A dedicated application hardening design now exists in `docs/mes/pilot-slice-01-application-design.md`, and it sequences the next work into hold coordination, work-queue read-model sourcing, idempotency, compact contract shapes, and only then handler scaffolding.
- The application hardening docs now explicitly persist release-1 hold provenance, separate quality decision outcome from current hold state, anchor `required_materials` to an MES-side snapshot, and reserve `review-required` for a later supervisory slice.
- The persistence draft now includes `status_before_hold`, `hold_source_type`, `hold_source_id`, `quality_record.decision_status`, `operation_material_requirement`, and a stronger `command_receipt` idempotency shape with request fingerprints.
- `Mes.Domain` now exposes release-1 hold provenance and quality decision outcome directly in code through `OperationExecution` and `QualityRecord`.
- A new `Mes.Application` project now contains the first coordinator-centered application boundary with `QualityHoldGateCoordinator`.
- Dedicated `Mes.Application.Tests` coverage now validates blocking quality materialization, duplicate replay, reconciliation, and provenance-aware release behavior.
- `Mes.Application` now also contains the first work-queue read-model source with operation-attachment requirement projection and a station queue read service built only from MES-side snapshots plus execution state.
- The slice docs now explicitly name the authoritative source for each release-1 work queue field and make the `quality_record -> wip_unit -> operation_execution` gate derivation path explicit.
- `Mes.Application` now contains the first command receipt policy and canonical fingerprint builder for the operator-execution slice, with receipt scope fixed to `channel + command_type + idempotency_key`.
- The slice docs now explicitly state that `request_fingerprint` excludes transport-only metadata such as `command_id`, `correlation_id`, `client_timestamp`, and `revision_refs`.
- `Mes.Application.Contracts` now groups command metadata into `CommandIdentityContract`, `CommandOriginContract`, and `CommandContextContract`, while operator-execution command requests themselves now use a compact `Context + Payload` constructor shape.
- The repository now carries an explicit rule that new or modified C# classes and functions must include Korean XML documentation comments, and that requirement is now stated directly in both the root and `wpf-dev-pack` AGENT entry points.
- The repository guidance now also prefers authored methods, constructors, and public APIs with five or fewer input parameters, using parameter objects when larger inputs are unavoidable.
- Project-specific manufacturing assumptions are still provisional and must be validated against one pilot line.

## Next Meaningful Work Unit

Execute Work Unit 5 from `docs/mes/pilot-slice-01-application-design.md`: scaffold command handlers, query handlers, and production-actuals preparation on top of the stabilized coordinator, work-queue, idempotency, and compact-contract foundations.

## Validation Path

- Compare `docs/mes/pilot-slice-01-operator-execution.md`, `docs/mes/logical-data-model-slice-01.md`, and `docs/mes/bff-payload-spec-slice-01.md` against the current domain code and selected pilot assumptions.
- Keep `docs/mes/pilot-slice-01-application-design.md` aligned with the slice docs whenever the sequence, ownership, or read-model strategy changes.
- Keep `src/Mes.Application.Contracts` and `docs/mes/persistence-schema-slice-01.sql` aligned with the slice documents whenever command semantics or persistence boundaries move.
- Keep `src/Mes.Application` aligned with the slice documents whenever coordinator ownership or reconciliation rules move.
- Keep `docs/mes/command-event-catalog.md` and `src/Mes.Domain/Statuses/DomainStatuses.cs` aligned as new commands or workflow events are promoted into code.
- Preserve the release-1 rule that operator queue `quality_gate_state` emits `open` or `hold`, while `review-required` stays reserved for a later slice.
- Extend slice tests in the same order as the hardening plan, starting with hold-gate coordinator behavior before handler or query scaffolding.
- Re-run both `Mes.Domain.Tests` and `Mes.Application.Tests` whenever coordinator rules, hold provenance, or quality decision persistence changes.
- Re-run `Mes.Application.Tests` whenever work-queue source ownership, projection ordering, or quality gate derivation rules change.
- Re-run `Mes.Application.Tests` whenever receipt scope, fingerprint canonicalization, or replay/conflict policy changes.
- Re-run `Mes.Application.Tests` whenever compact command-contract shapes change so canonical fingerprint semantics and scope extraction stay stable.
- Preserve the rule that BFF payload semantics stay identical across WPF and Web even if channel UX diverges.
- Prefer request or parameter objects over long authored signatures as the application layer grows past simple domain calls.
