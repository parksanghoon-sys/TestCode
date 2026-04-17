# Current Plan

## Current Goal

Turn the MES baseline into an executable pilot slice by carrying the current operator-execution flow through domain, application, infrastructure, and a thin shared BFF entry point.

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
- A dedicated application hardening design now exists in `docs/mes/pilot-slice-01-application-design.md`, and it now sequences both the original slice build-out plus the next pilot-hardening work units for order completion, canonical save boundaries, HTTP error normalization, and future PostgreSQL handoff.
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
- `Mes.Application` now contains command handling models, stored-response serialization, and production-actuals preparation primitives for the operator-execution slice.
- `Mes.Application` now contains `OperatorExecutionCommandHandler` and `GetStationWorkQueueQueryHandler`, both operating on preloaded state bundles so command policy, replay behavior, and query composition stay inside the application layer instead of leaking into BFF-specific code.
- `Mes.Application.Tests` now cover handler acceptance, safe replay, production-actuals skeleton preparation, and work-queue contract mapping in addition to the earlier coordinator, projection, and idempotency tests.
- `Mes.Application` now also contains `IOperatorExecutionCommandPort`, `IStationWorkQueueSourcePort`, and `OperatorExecutionApplicationService`, so handler/query orchestration can load state bundles, invoke application policies, and persist results without binding the current slice to a specific repository or endpoint technology.
- `Mes.Application.Tests` now validate that the new application service loads state through ports, skips persistence on replay, persists prepared actuals on accepted completion, and composes work-queue queries through the source port.
- A new `Mes.Infrastructure` project now provides the first concrete port implementation as an in-memory reference adapter for operator execution, including `InMemoryOperatorExecutionStore`, command/query adapters, and a thin `OperatorExecutionBffEndpointAdapter`.
- The first concrete adapter now commits `command_receipt`, prepared `production_actuals_batch`, and in-memory outbox entries through one explicit write-set boundary, while aggregate mutations remain owned by the already-loaded domain objects.
- `Mes.Application.Tests` now include end-to-end reference-adapter coverage for accepted command persistence, replay-safe outbox behavior, prepared actuals persistence, and station work-queue projection through the infrastructure boundary.
- A new `Mes.ExperienceApi` project now provides the first thin shared BFF host for the slice, wiring minimal API routes to `OperatorExecutionBffEndpointAdapter` through dependency-injected infrastructure services.
- `Mes.ExperienceApi.Tests` now verify that the documented operator-execution endpoint signatures are exposed as concrete routes and that the host resolves the endpoint adapter plus its configured store through DI.
- `Mes.Domain` now exposes explicit restore boundaries for `ProductionOrder`, `OperationExecution`, `MaterialLot`, `QualityRecord`, and `WipUnit`, so detached persistence snapshots can reconstruct the current slice without replaying business commands.
- `Mes.Infrastructure` now also provides `OperatorExecution/FileStore/` as the first durable operational adapter, persisting aggregate/entity snapshots, `command_receipt`, prepared `production_actuals_batch`, and outbox entries to a JSON snapshot file through the existing application ports.
- `Mes.Application.Tests` now validate that the file-backed durable adapter preserves receipt replay, prepared actuals, and work-queue snapshots across store reload.
- `Mes.Infrastructure` now also carries an `OperatorExecution/Sqlite/` relational adapter candidate built on `Microsoft.Data.Sqlite`, including a SQLite store, command port, and station work-queue source adapter that compile against the existing application ports.
- `Mes.Application.Tests` now also validate the SQLite relational adapter candidate through the same durable acceptance path used for `FileStore/`: receipt replay, outbox durability, prepared actuals persistence, and station work-queue projection all survive store reopen.
- `Mes.ExperienceApi` durable composition is now provider-selectable at the host layer, with `Sqlite` as the current default runtime, `FileStore` kept only as an explicit comparison path, and `Postgres` reserved as a future provider slot without changing the application boundary.
- `Mes.ExperienceApi.Tests` now verify the default `Sqlite` runtime, explicit comparison-path `FileStore` selection, protection against legacy file-path drift, and the current reserved-provider guard for `Postgres`.
- The current executable relational path covers the write-set the application actually mutates today, but the SQL draft still documents additional tables such as `material_consumption` and `override_request` that are not yet exercised by the slice implementation.
- The slice docs now explicitly record the current SQLite coverage versus deferred relational targets, and the provider-neutral SQL draft now marks `material_consumption` plus `override_request` as deferred while keeping `wip_unit.status_before_hold` aligned with executable persistence.
- The slice application design now also documents Work Units 6 through 9 for pilot hardening: cross-operation order completion progression, canonical save transaction boundaries, Experience API error normalization, and the first PostgreSQL provider handoff contract.
- Work Unit 6 is now executable in code: `complete-operation` loads an authoritative sibling-operation summary, advances `ProductionOrder` to `PartiallyCompleted` or `Completed`, and the new behavior is locked by handler, application-service, and durable adapter tests.
- Work Unit 7 is now executable as the explicit pilot save contract: `InMemory`, `FileStore`, and `Sqlite` adapter tests all lock the same accepted-command write-set, including touched aggregate state, `command_receipt`, `production_actuals_batch`, and the full outbox set for accepted completion.
- Work Unit 8 is now executable in code: deterministic operator-execution failures are promoted into typed application exceptions, `Mes.ExperienceApi` normalizes them into stable problem-details responses for `400`, `404`, `409`, `422`, and fallback `500`, and dedicated host tests lock the route-thin transport behavior.
- Each current source and test project now carries a local `README.md` that explains its purpose, responsibility boundary, key classes, folder structure, dependency direction, and current limitations.
- The repository now carries an explicit rule that new or modified C# classes and functions must include Korean XML documentation comments, and that requirement is now stated directly in both the root and `wpf-dev-pack` AGENT entry points.
- The repository guidance now also prefers authored methods, constructors, and public APIs with five or fewer input parameters, using parameter objects when larger inputs are unavoidable.
- The repository guidance now also requires a project-level `README.md` whenever a new project is created, and that README must be updated when the project's structure or role changes materially.
- Project-specific manufacturing assumptions are still provisional and must be validated against one pilot line.

## Next Meaningful Work Unit

Confirm the pilot manufacturing mode and required genealogy depth for the first rollout.

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
- Re-run `Mes.Application.Tests` whenever handler-side state validation, stored-response replay restoration, or production-actuals preparation rules change.
- Re-run `Mes.Application.Tests` whenever the work-queue query handler mapping or contract field ownership changes.
- Re-run `Mes.Application.Tests` whenever application-service orchestration, load/save port contracts, or replay persistence conditions change.
- Re-run `Mes.Application.Tests` whenever `complete-operation` starts mutating `ProductionOrder` status or loading a new order progression summary source.
- Re-run `Mes.Application.Tests` whenever the reference infrastructure adapter changes its write-set composition, outbox capture, or state-loading assumptions.
- Re-run `Mes.Application.Tests` whenever detached restore state, file snapshot mapping, or file-store replay behavior changes.
- Re-run `dotnet test Mes.slnx -v minimal` whenever `src/Mes.Infrastructure/OperatorExecution/Sqlite/`, durable host DI composition, or relational package references change.
- Keep both `FileStore/` and `Sqlite/` durable tests alive while slice-01 relational coverage is still being clarified, so host composition changes can compare two proven persistence paths instead of replacing one verified path with another.
- Keep `Mes.ExperienceApi.Tests` aligned whenever durable service registration changes, and assert which concrete store the default host resolves.
- Re-run `Mes.ExperienceApi.Tests` whenever deterministic exception types or host-level problem-details mapping changes.
- Compare `src/Mes.Infrastructure/OperatorExecution/Sqlite/SqliteOperatorExecutionStore.cs` against `docs/mes/persistence-schema-slice-01.sql` whenever relational persistence coverage expands, and explicitly record any intentionally deferred tables such as `material_consumption` or `override_request`.
- Preserve the new rule that future relational providers, including PostgreSQL, must plug in through the same host composition seam instead of changing `Mes.Application` or route handlers.
- Keep the PostgreSQL handoff section in `docs/mes/pilot-slice-01-application-design.md` aligned whenever provider configuration keys or durable port contracts change.
- Keep any future HTTP host thin: route handlers should call `OperatorExecutionBffEndpointAdapter` or the application service boundary rather than re-implementing orchestration or validation.
- Re-run `Mes.ExperienceApi.Tests` whenever route signatures, host DI wiring, or the default durable host composition changes.
- Preserve the rule that BFF payload semantics stay identical across WPF and Web even if channel UX diverges.
- Prefer request or parameter objects over long authored signatures as the application layer grows past simple domain calls.
