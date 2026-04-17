# TODOS

## Current TODOs

- Select one pilot line and one representative product family for release 1 scope validation.
- Validate the documented pilot profile assumptions for manufacturing mode, genealogy depth, and station-side device-handshake needs against that selected pilot target.
- Confirm authoritative ownership for item, BOM, routing, resource, and quality master data.
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

### [P2_LATER] Decide when future side effects should widen the canonical save boundary

What remains:
Decide when new persisted side effects such as `material_consumption`, `override_request`, or later audit and publication records should join the current accepted-command atomic boundary.

Why deferred:
Work Unit 7 now locks the current pilot contract in code and adapter documentation. The remaining question only matters when the slice starts persisting new executable side effects beyond today's accepted-command write-set.

Objective:
Keep the current pilot contract stable while making future widening explicit, intentional, and testable.

Relevant context:
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationService.cs` still issues one `SaveAsync` per accepted command, and Work Unit 7 now makes the current logical write-set explicit across `InMemory`, `FileStore`, and `Sqlite`.

Relevant files and scope:
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationService.cs`
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationPorts.cs`
`docs/mes/persistence-schema-slice-01.sql`
`docs/mes/logical-data-model-slice-01.md`
`docs/mes/pilot-slice-01-application-design.md`
`src/Mes.Infrastructure/README.md`

Current status:
The current canonical write-set is now test-locked as touched aggregate state plus `command_receipt`, `production_actuals_batch`, and `domain_outbox`, with replay-safe parity proven across all current providers. What remains open is when later relational tables such as `material_consumption` or `override_request` should join that same atomic boundary.

Known blockers or open questions:
- Which new side effect should be promoted first when the slice expands beyond today's operator-execution path.
- Whether a future widened contract should stay provider-neutral or expose provider-specific helper records at the documentation edge.

Natural next step:
When a new durable side effect becomes executable, update the design docs first, then widen the provider-facing tests and adapter documentation in the same work unit before changing persistence code.

### [P1_SOON] Replace the file-backed durable bridge with a relational persistence adapter

What remains:
Design and implement the first relational persistence adapter that preserves the current `IOperatorExecutionCommandPort` and `IStationWorkQueueSourcePort` behavior while replacing the current file-backed durable bridge.

Why deferred:
The current cycle intentionally landed a file-backed durable adapter first so the project could prove detached aggregate reconstruction, replay-safe persistence, and host composition without prematurely committing to raw SQL access strategy or transaction plumbing.

Objective:
Keep the now-proven application boundary stable while moving the same write-set onto a relational store aligned with the SQL draft.

Relevant context:
`src/Mes.Infrastructure/OperatorExecution/FileStore/` now proves detached snapshot reconstruction and durable replay behavior, but it is still a JSON file bridge rather than the target relational operational store.

Relevant files and scope:
`src/Mes.Infrastructure/OperatorExecution/FileStore/FileOperatorExecutionStore.cs`
`src/Mes.Infrastructure/OperatorExecution/FileStore/FileOperatorExecutionCommandAdapter.cs`
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationPorts.cs`
`docs/mes/persistence-schema-slice-01.sql`
`docs/mes/logical-data-model-slice-01.md`

Current status:
`src/Mes.Infrastructure/OperatorExecution/Sqlite/` now contains the active default durable runtime, `Mes.ExperienceApi` can still be pointed at `FileStore` only through explicit provider selection, `Mes.Application.Tests` plus `Mes.ExperienceApi.Tests` now cover provider selection and durable parity, and `docs/mes/pilot-slice-01-application-design.md` now carries the first concrete PostgreSQL handoff contract for the same seam.

Known blockers or open questions:
- Whether slice 01 must execute `material_consumption` and `override_request` as relational tables now, or can keep them documented-but-deferred until those flows become executable in the application layer.
- What minimum connection-string and transaction assumptions the future PostgreSQL provider should inherit from the current host seam.

Natural next step:
Keep the documented handoff contract aligned while implementing Work Units 6 through 8 first, and only start a PostgreSQL adapter when it becomes an active delivery requirement.

### [P2_LATER] Add a PostgreSQL durable provider through the existing host seam

What remains:
Implement a concrete PostgreSQL-backed operator-execution durable adapter that plugs into the current provider-selectable `Mes.ExperienceApi` composition path.

Why deferred:
The user direction for this cycle was to run on SQLite now while keeping the database replaceable later. The host seam and reserved configuration slot now exist, but there is still no immediate execution need to widen the slice into a second relational adapter.

Objective:
Allow the team to switch from SQLite to PostgreSQL later without changing `Mes.Application`, route handlers, or the operator-execution contracts.

Relevant context:
`OperatorExecutionServiceCollectionExtensions` now defaults to `Sqlite`, still supports `FileStore`, and reserves `Postgres` as a provider value plus `Mes:OperatorExecutionConnectionString` as the future relational setting.

Relevant files and scope:
`src/Mes.ExperienceApi/OperatorExecution/OperatorExecutionServiceCollectionExtensions.cs`
`src/Mes.Infrastructure/OperatorExecution/Sqlite/`
`src/Mes.Application/OperatorExecution/OperatorExecutionApplicationPorts.cs`
`tests/Mes.ExperienceApi.Tests/OperatorExecutionExperienceApiEndpointRouteBuilderTests.cs`

Current status:
The host seam is in place and tested, but the `Postgres` provider intentionally throws a reserved-provider exception because no adapter exists yet.

Known blockers or open questions:
Which PostgreSQL access strategy to use, how much of the SQLite schema should map 1:1, and whether provider-specific transaction or concurrency behavior needs to surface in infrastructure tests.

Natural next step:
When PostgreSQL becomes an active requirement, mirror the current SQLite acceptance coverage first, then implement provider registration plus adapter code behind the already reserved `Postgres` provider value.

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

- 2026-04-17: Added `docs/mes/quick-start.md` as the canonical local usage and test guide for the current no-equipment operator-execution path, linked it from the WPF project README and the station/edge implementation plan, and re-saved the guide as UTF-8 BOM so PowerShell opens it without mojibake.
- 2026-04-17: Completed a byte-level audit of `docs/mes/*.md`, confirmed `docs/mes/command-event-catalog.md` was the only remaining mojibake file, restored it as readable UTF-8 Korean, and re-aligned the WPF station-plan docs plus `src/Mes.Client.Wpf/README.md` to the current scan-capable shell.
- 2026-04-17: Restored `docs/mes/architecture-blueprint.md` and `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md` as readable UTF-8 Korean documents, re-synced both to the current executable seams, and recorded the remaining docs-directory mojibake scan as deferred follow-up work.
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
- 2026-04-16: Added `src/Mes.Infrastructure` with an in-memory operator-execution reference store, concrete command/query adapters, a thin BFF endpoint adapter, and end-to-end adapter tests for receipts, outbox capture, prepared actuals, and station work-queue projection.
- 2026-04-16: Added `src/Mes.ExperienceApi` as the first thin shared BFF host for the operator-execution slice, with minimal API route mapping, DI composition over the in-memory reference adapter, and `Mes.ExperienceApi.Tests` route/DI smoke coverage.
- 2026-04-16: Added project-level `README.md` files for all current source and test projects, and updated repository guidance so future project creation must include and maintain the same local project documentation.
- 2026-04-16: Added explicit domain restore boundaries plus `Mes.Infrastructure/OperatorExecution/FileStore` as the first durable operational adapter, switched `Mes.ExperienceApi` to that durable adapter by default, and added reload-safe adapter tests.
- 2026-04-17: Added `tests/Mes.Application.Tests/SqliteOperatorExecutionAdapterTests.cs` so the SQLite relational adapter candidate now proves receipt replay, outbox durability, prepared actuals persistence, and station work-queue projection across store reopen.
- 2026-04-17: Made `Mes.ExperienceApi` durable provider selection configuration-driven, switched the default runtime to `Sqlite`, kept `FileStore` selectable through the same seam, and reserved `Postgres` as the future provider slot.
- 2026-04-17: Updated the slice docs so SQLite is explicitly documented as the current executable runtime, `material_consumption` and `override_request` are marked as deferred relational targets, and `wip_unit.status_before_hold` is aligned across the logical and physical persistence drafts.
- 2026-04-17: Locked `FileStore` down as an explicit comparison-only path, kept SQLite as the only active runtime, and added host smoke coverage so legacy file-store path settings cannot silently override the SQLite default.
- 2026-04-17: Extended `docs/mes/pilot-slice-01-application-design.md` with pilot-hardening Work Units 6 through 9 for order completion progression, canonical save boundaries, Experience API error normalization, and the first PostgreSQL handoff contract.
- 2026-04-17: Implemented Work Unit 6 so `complete-operation` now advances `ProductionOrder` from an authoritative sibling-operation summary, with handler, application-service, and durable adapter tests covering both `PartiallyCompleted` and `Completed` progression.
- 2026-04-17: Implemented Work Unit 7 so the current accepted-command write-set is now locked across `InMemory`, `FileStore`, and `Sqlite` adapter tests, and the adapter READMEs explicitly document the canonical pilot save boundary.
- 2026-04-17: Implemented Work Unit 8 so deterministic operator-execution failures now normalize to stable `400`, `404`, `409`, `422`, and fallback `500` problem-details responses through one thin-host mapping seam, with dedicated `Mes.ExperienceApi.Tests` coverage.
- 2026-04-17: Reworked `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md` into a subordinate WPF station client plus Modbus/edge implementation plan that aligns with the canonical BFF command path, channel policy, and current SQLite-backed backend seams.
- 2026-04-17: Added `docs/mes/pilot-profile-working-assumptions.md` so the current executable pilot profile is explicit as a working assumption set rather than an untracked guess, covering station-based discrete/hybrid execution, lot-first genealogy, and optional Modbus/PLC handshake scope.
- 2026-04-17: Added `src/Mes.Client.Wpf` and `tests/Mes.Client.Wpf.Tests` as the first executable WPF station shell, wired it through Generic Host plus a typed operator-execution BFF client, documented both projects locally, and validated the repository with `dotnet test tests/Mes.Client.Wpf.Tests/Mes.Client.Wpf.Tests.csproj -v minimal`, `dotnet build Mes.slnx -v minimal`, and `dotnet test Mes.slnx -v minimal`.
- 2026-04-17: Hardened the WPF station shell so station rebind clears stale queue snapshots, message severity now reaches the shell panel, client-side malformed success responses are normalized into operator-visible failures, and fake-HTTP plus shell-state tests now cover the WPF seam.
- 2026-04-17: Extended `Mes.Client.Wpf` into the first command-capable WPF shell with shared station-side command-context generation plus `start-operation` and `complete-operation`, refreshed the WPF project READMEs, and validated the repository with `dotnet test tests/Mes.Client.Wpf.Tests/Mes.Client.Wpf.Tests.csproj -v minimal`, `dotnet build Mes.slnx -v minimal`, and `dotnet test Mes.slnx -v minimal`.
- 2026-04-17: Added root-level NuGet central package management through `Directory.Packages.props`, removed per-project solution package versions into that shared file, and revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`.
- 2026-04-17: Extended `Mes.Client.Wpf` to support `record-material-consumption` over the shared BFF seam, added multi-shot-safe WPF action-token idempotency, refreshed the WPF READMEs, and validated the repository with `dotnet test tests/Mes.Client.Wpf.Tests/Mes.Client.Wpf.Tests.csproj -v minimal`, `dotnet build Mes.slnx -v minimal`, and `dotnet test Mes.slnx -v minimal`.
- 2026-04-17: Projected the authoritative operation quantity unit into the shared station queue contract, made `Mes.Client.Wpf` consume that queue-projected unit as the read-only `complete-operation` unit, refreshed the affected docs, and validated the repository with `dotnet test tests/Mes.Application.Tests/Mes.Application.Tests.csproj -v minimal`, `dotnet test tests/Mes.Client.Wpf.Tests/Mes.Client.Wpf.Tests.csproj -v minimal`, `dotnet build Mes.slnx -v minimal`, and `dotnet test Mes.slnx -v minimal`.
- 2026-04-17: Added `example/Mes.MockStation.Example` as a deterministic SQLite-backed mock station demo with seeded manifest output, runnable PowerShell launch/smoke scripts, and `Mes.ExperienceApi.Tests` smoke coverage so the current operator-execution path can be exercised without real equipment; validated with `powershell -File example/Mes.MockStation.Example/Test-MockStationDemo.ps1`, `dotnet test tests/Mes.ExperienceApi.Tests/Mes.ExperienceApi.Tests.csproj -v minimal`, `dotnet build Mes.slnx -v minimal`, and `dotnet test Mes.slnx -v minimal`.
- 2026-04-17: Revalidated the current plan against the repository state, recognized that pilot-line selection remains the highest-priority but externally blocked step, and closed the nearest safe internal follow-up by aligning `docs/mes/architecture-blueprint.md`, `docs/mes/command-event-catalog.md`, `docs/mes/pilot-slice-01-operator-execution.md`, and `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md` to the executable code vocabulary; validated with `dotnet test Mes.slnx -v minimal`.
