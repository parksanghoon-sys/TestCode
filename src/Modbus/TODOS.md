# TODOS

## Current TODOs

- Implement concrete persistence and endpoint adapters for `IOperatorExecutionCommandPort` and `IStationWorkQueueSourcePort`, including one explicit atomic save boundary for aggregate state, `command_receipt`, `production_actuals_batch`, and eventual outbox writes.
- Confirm the pilot manufacturing mode and required genealogy depth for the first rollout.
- Confirm authoritative ownership for item, BOM, routing, resource, and quality master data.
- Select one pilot line and one representative product family for release 1 scope validation.
- Validate the documented WPF/Web responsibility split against actual user roles and workflow criticality.
- Validate the initial command/event catalog against pilot workflows and stakeholder terminology.
- Validate the offline authority matrix with OT, production, and quality stakeholders.
- Validate the release-1 WMS-lite reconciliation path with warehouse operations.
- Validate the selected operator-execution pilot slice against one pilot line and one representative product family.

## Deferred Backlog

### [P1_SOON] Formalize QMS and LIMS split

What remains:
Define whether MES quality execution stops at in-process inspection and hold/release, or whether some final disposition logic remains in QMS/LIMS.

Why deferred:
The current architecture can proceed without locking this immediately, but API and workflow boundaries will change once final quality authority is decided.

Objective:
Avoid duplicated quality ownership and rework later in inspection and disposition flows.

Relevant context:
`docs/mes/architecture-blueprint.md` currently assumes MES manages execution-time quality and forwards enterprise-level quality workflows outward.

Relevant files and scope:
`docs/mes/architecture-blueprint.md`
`docs/mes/implementation-roadmap.md`

Current status:
Release 1 baseline now states that MES owns in-process quality execution and hold gate authority, but the enterprise quality split beyond that is still not finalized.

Known blockers or open questions:
Does the target plant require lab approval, enterprise quality release, or regulated e-signature outside MES.

Natural next step:
Run a workshop with quality stakeholders and mark which quality decisions stay inside Release 1 MES authority versus enterprise-governed QMS/LIMS flow.

### [P1_SOON] Decide line-side inventory reconciliation model

What remains:
Choose how MES and WMS reconcile issue, return, shortage, and substitution events for line-side material.

Why deferred:
Release 1 architecture does not require a full warehouse redesign, but material truth and traceability accuracy depend on a clear reconciliation rule.

Objective:
Prevent WMS and MES from drifting into conflicting inventory states during pilot rollout.

Relevant context:
The current blueprint assumes MES owns in-process and line-side consumption truth while WMS keeps warehouse stock authority.

Relevant files and scope:
`docs/mes/architecture-blueprint.md`
`docs/mes/implementation-roadmap.md`

Current status:
The baseline now defines a Release 1 WMS-lite model, but the actual timing, event contract, and discrepancy closure rule for the pilot line are still unspecified.

Known blockers or open questions:
Whether the plant uses kanban replenishment, staged kitting, direct issue, or mixed material handling patterns.

Natural next step:
Map the actual material movement path for the pilot line and define one reconciliation event sequence end to end, including discrepancy closure ownership.

### [P2_LATER] Standardize edge integration protocol

What remains:
Pick the preferred protocol strategy for equipment integration across pilot and future lines.

Why deferred:
The current architecture intentionally keeps the edge layer abstract because vendor and OT constraints are not yet known.

Objective:
Reduce future adapter sprawl and keep device integration maintainable across sites.

Relevant context:
The blueprint already reserves `Site Edge Gateway` as a separate deployment boundary with offline buffering.

Relevant files and scope:
`docs/mes/architecture-blueprint.md`

Current status:
No project-specific choice has been made between OPC UA, MQTT, vendor APIs, file drops, or mixed adapters.

Known blockers or open questions:
Actual equipment vendors, existing OT standards, cybersecurity policies, and historian integration constraints are unknown.

Natural next step:
Inventory the pilot line equipment and document the supported protocols and current control interfaces.

### [P2_LATER] Prepare multi-site template and governance model

What remains:
Define which IDs, contracts, deployment conventions, and change-control rules must stay standardized across future sites.

Why deferred:
The first delivery should optimize for one pilot site first; forcing a full enterprise template now would slow down discovery.

Objective:
Enable later rollout without rebuilding the core domain model and interfaces from scratch.

Relevant context:
The blueprint is multi-site ready in principle, but only a single-site pilot baseline is documented.

Relevant files and scope:
`docs/mes/architecture-blueprint.md`
`docs/mes/implementation-roadmap.md`

Current status:
Multi-site appears only as a release 3 concern.

Known blockers or open questions:
Global template ownership, regional plant variation, and shared versus local process rules are still unknown.

Natural next step:
After pilot stabilization, extract the IDs, contracts, and module boundaries that stayed stable and promote them into a site template.

### [P1_SOON] Define shared client contract and channel policy

What remains:
Define which workflows are WPF-first, which are web-first, and which command and notification contracts must stay identical across both channels.

Why deferred:
The architecture now reserves both client paths, but the detailed screen, BFF, and offline behavior still depend on pilot workflow validation.

Objective:
Keep WPF and Web as interchangeable presentation options without splitting domain behavior or audit semantics.

Relevant context:
`docs/mes/architecture-blueprint.md` now introduces `Experience API / BFF`, `WPF Station Client`, and `Web Portal` as separate client shells over the same MES core.

Relevant files and scope:
`docs/mes/architecture-blueprint.md`
`docs/mes/implementation-roadmap.md`

Current status:
The baseline now includes command ownership and a first-pass workflow channel matrix, and dedicated draft documents exist for the channel matrix and command/event catalog.

Known blockers or open questions:
Actual shop-floor devices, kiosk constraints, browser policies, Windows deployment policy, and role-specific UX requirements are still unknown.

Natural next step:
Validate the draft matrix against pilot user roles, then turn the validated flows into screen-level WPF/Web UX definitions and payload specs.

### [P1_SOON] Reconcile domain code and architecture terminology

What remains:
Confirm that the status names and event vocabulary used in `src/Mes.Domain` match the terminology intended in the MES architecture and command/event catalog.

Why deferred:
The current cycle prioritized creating the first code foundation and establishing the XML documentation rule, but document-code drift will create confusion once API contracts are derived.

Objective:
Keep domain code, architecture documents, and future payload specs on one canonical set of workflow terms.

Relevant context:
`OperationExecutionStatus` in code currently includes `Paused`, and other aggregate/event names now act as the first executable source of truth for release-1 workflow modeling.

Relevant files and scope:
`src/Mes.Domain/Statuses/DomainStatuses.cs`
`src/Mes.Domain/Events/DomainEvents.cs`
`docs/mes/architecture-blueprint.md`
`docs/mes/command-event-catalog.md`

Current status:
The core domain seed exists and tests pass, but terminology alignment against the architecture docs has not yet been formalized.

Known blockers or open questions:
Should the code vocabulary drive the docs from here, or should the docs remain canonical and force code renames where they differ.

Natural next step:
Review the implemented status and event names against the release-1 workflow documents, then either update the docs or rename the code before defining API payload contracts.

### [P1_SOON] Decide production-order progression update rules for operation completion handlers

What remains:
Define when `complete-operation` should promote `ProductionOrder` to `PartiallyCompleted` versus `Completed`, and what additional loaded state is required to do that safely.

Why deferred:
The new Work Unit 5 handler intentionally completes `OperationExecution` and prepares production-actuals skeletons without mutating order-level completion state, because the current handler boundary only receives the parent order and the current operation. That is not enough evidence to infer whether sibling operations remain open.

Objective:
Avoid incorrect order-level lifecycle updates while still letting operation completion stay executable and replay-safe.

Relevant context:
`src/Mes.Application/OperatorExecution/OperatorExecutionCommandHandler.cs` now handles `complete-operation` and prepares a pending production-actuals batch, but it deliberately leaves `ProductionOrder` status unchanged beyond earlier start-operation progression.

Relevant files and scope:
`src/Mes.Application/OperatorExecution/OperatorExecutionCommandHandler.cs`
`src/Mes.Domain/Aggregates/ProductionOrder.cs`
`docs/mes/pilot-slice-01-operator-execution.md`
`docs/mes/pilot-slice-01-application-design.md`

Current status:
Operation completion is executable, tested, and replay-safe, but order completion semantics are still unresolved for multi-operation orders.

Known blockers or open questions:
Will the eventual handler load sibling `OperationExecution` states, a summarized order progression view, or a dedicated completion projection before deciding order-level status.

Natural next step:
Choose the minimal authoritative source for sibling-operation completion visibility, then extend the completion handler and tests to update `ProductionOrder` status from that source.

### [P1_SOON] Define the transactional persistence boundary for application-service saves

What remains:
Define exactly which records must commit atomically when `OperatorExecutionApplicationService` saves an accepted command result.

Why deferred:
The new application service now centralizes load and save orchestration through `IOperatorExecutionCommandPort`, but the project still has no concrete persistence adapter. Before implementing one, the save boundary must be explicit so handlers do not accidentally persist aggregate state, receipts, `production_actuals_batch`, and future outbox entries in inconsistent steps.

Objective:
Keep idempotency replay, aggregate mutation, and integration publication consistent under retries and partial failures.

Relevant context:
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationService.cs` now issues one `SaveAsync` call per accepted command, while `docs/mes/persistence-schema-slice-01.sql` already models `command_receipt`, `production_actuals_batch`, and `domain_outbox` as separate records.

Relevant files and scope:
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationService.cs`
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationPorts.cs`
`docs/mes/persistence-schema-slice-01.sql`
`docs/mes/logical-data-model-slice-01.md`

Current status:
The orchestration boundary is executable and tested, but atomic commit semantics are still implicit.

Known blockers or open questions:
Should `command_receipt`, aggregate state, `production_actuals_batch`, and `domain_outbox` always share one transaction, and if so which adapter layer owns that transaction boundary.

Natural next step:
Choose the minimum atomic write set for each accepted command type, then let the first concrete port adapter implement exactly that boundary.

### [P2_LATER] Decide whether rejected material scans need durable storage

What remains:
Decide whether failed or rejected `record-material-scan` attempts must be persisted separately from accepted `material_consumption` records.

Why deferred:
The first schema draft intentionally models only accepted consumption and genealogy because that is enough for the current operator-execution slice. Persisting every rejected scan would add extra tables and workflow states before the pilot audit requirement is confirmed.

Objective:
Avoid under-modeling operator audit requirements while keeping the release-1 persistence design minimal and coherent.

Relevant context:
`docs/mes/bff-payload-spec-slice-01.md` still treats material scan handling as an application or BFF validation step ahead of `record-material-consumption`, and `docs/mes/persistence-schema-slice-01.sql` currently omits a separate scan-attempt table.

Relevant files and scope:
`docs/mes/bff-payload-spec-slice-01.md`
`docs/mes/logical-data-model-slice-01.md`
`docs/mes/persistence-schema-slice-01.sql`

Current status:
Accepted material usage is persisted through `material_consumption` and `genealogy_link`, but rejected scans are not yet modeled as durable operational records.

Known blockers or open questions:
Whether the pilot line requires rejected-scan history for traceability, training, compliance, or investigation.

Natural next step:
Validate the plant audit expectation for rejected scans, then either keep scan attempts ephemeral in the BFF or add a dedicated persistence model and outbox event for them.

## Completed

- 2026-04-14: Created the initial MES project architecture baseline in `docs/mes/architecture-blueprint.md`.
- 2026-04-14: Created the initial phased rollout plan in `docs/mes/implementation-roadmap.md`.
- 2026-04-14: Updated the MES baseline to keep the client layer channel-neutral with both WPF and Web as valid shells.
- 2026-04-14: Tightened the MES baseline with explicit BFF/Edge command ownership, offline authority, Release 1 WMS-lite reconciliation, and Release 1 quality authority.
- 2026-04-14: Added `docs/mes/client-channel-matrix.md` with pilot workflow-to-channel allocation and offline interpretation.
- 2026-04-14: Added `docs/mes/command-event-catalog.md` with initial Release 1 command, domain event, and integration event definitions.
- 2026-04-14: Established the first project memory files: `CURRENT_PLAN.md`, `TODOS.md`, and `DECISIONS.md`.
- 2026-04-14: Added the first `Mes.Domain` implementation baseline and `Mes.Domain.Tests` coverage for core MES execution aggregates.
- 2026-04-14: Added the repository rule that new or modified C# classes and functions must carry Korean XML documentation comments, and applied it to the current domain/test code.
- 2026-04-14: Strengthened the root and `wpf-dev-pack` AGENT entry points so the Korean XML documentation requirement is stated explicitly as mandatory for introduced or changed C# classes and functions.
- 2026-04-16: Selected the first implementation-ready pilot slice as operator execution with material consumption and a quality hold gate, and aligned the existing architecture and command/event docs to the current code vocabulary.
- 2026-04-16: Added `docs/mes/pilot-slice-01-operator-execution.md`, `docs/mes/logical-data-model-slice-01.md`, and `docs/mes/bff-payload-spec-slice-01.md` as the first slice-specific design artifacts.
- 2026-04-16: Added end-to-end happy-path and hold-gate tests for the selected operator execution slice in `tests/Mes.Domain.Tests/OperatorExecutionWorkflowTests.cs`.
- 2026-04-16: Added `src/Mes.Application.Contracts` with concrete operator-execution command, response, query, notification, and endpoint-signature types for the first slice.
- 2026-04-16: Added `docs/mes/persistence-schema-slice-01.sql` as the first persistence-oriented SQL draft for the operator execution slice.
- 2026-04-16: Added a repository-wide guidance rule to prefer authored methods, constructors, and public APIs with five or fewer input parameters, using parameter objects when larger inputs are unavoidable.
- 2026-04-16: Added `docs/mes/pilot-slice-01-application-design.md` and tightened the next work order so hold coordination, work-queue read-model sourcing, idempotency, and compact contract shapes are resolved before handler scaffolding.
- 2026-04-16: Hardened the slice-01 design so blocking quality outcomes materialize into persisted hold provenance, operator queue requirements come from MES-side snapshots, and command receipts carry canonical idempotency fingerprints.
- 2026-04-16: Implemented Work Unit 1 with release-1 hold provenance in `Mes.Domain`, added `Mes.Application` plus `QualityHoldGateCoordinator`, and validated the coordinator rules in `Mes.Application.Tests`.
- 2026-04-16: Implemented Work Unit 2 with operation-attachment requirement projection, a MES-side station work-queue read service, and application tests that lock queue source ownership and quality gate derivation.
- 2026-04-16: Implemented Work Unit 3 with command receipt scope, canonical fingerprinting, replay/conflict policy, and application tests that lock transport-metadata-independent replay semantics.
- 2026-04-16: Implemented Work Unit 4 by compacting operator-execution command contracts to `Context + Payload`, introducing grouped command metadata contracts, and preserving replay semantics under the new request shape.
- 2026-04-16: Implemented Work Unit 5 by adding application command handlers, a station work-queue query handler, stored-response serialization, production-actuals skeleton preparation, and application tests that validate acceptance, replay, and contract mapping.
- 2026-04-16: Added operator-execution application ports and `OperatorExecutionApplicationService` so state loading, replay-aware handler invocation, and result persistence are now orchestrated through adapter-facing boundaries.
