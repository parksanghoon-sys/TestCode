# DECISIONS

## 2026-04-14 ADR-001: MES owns execution truth

Decision:
MES will be treated as the authoritative system for execution state, WIP truth, genealogy, line-side material consumption, and execution-time quality events.

Why:
Without a single owner for execution truth, ERP, WMS, SCADA, and spreadsheet processes will drift and make genealogy and exception recovery unreliable.

Implications:
- ERP remains upstream for order release and business master data.
- MES must persist auditable execution events and support idempotent replay handling.
- WMS and QMS integrations must be designed around MES execution events rather than parallel local logic.

## 2026-04-14 ADR-002: Start with central MES plus site edge

Decision:
Use a central MES application boundary with a separately deployed site-edge gateway for equipment connectivity and offline buffering.

Why:
Brownfield manufacturing sites often need intermittent network tolerance and protocol adaptation close to equipment, but the business workflow should still remain centralized.

Implications:
- Edge buffering and store-and-forward are first-class requirements.
- Device protocol decisions can stay flexible without forcing the core domain to change.
- Observability must cover both central services and site-edge queues.

## 2026-04-14 ADR-003: Prefer modular monolith before microservice split

Decision:
Implement the initial MES core as a modular monolith with asynchronous integration patterns rather than decomposing immediately into many microservices.

Why:
Early MES projects discover and adjust domain boundaries frequently. Premature distribution would increase operational complexity before the domain model stabilizes.

Implications:
- Module boundaries still need to be explicit in code and contracts.
- Reporting, integration, and some quality capabilities can be split later if scale or autonomy proves the need.
- The first implementation should optimize for correctness, traceability, and operability over service count.

## 2026-04-14 ADR-004: Keep the client layer channel-neutral

Decision:
Design the MES client layer so both `WPF` and `Web` can operate on the same MES core through a shared `Experience API / BFF` boundary.

Why:
Shop-floor execution often benefits from WPF because of Windows device integration, kiosk control, and richer offline behavior, while supervisory and administrative use cases benefit from web reach and easier deployment.

Implications:
- Domain behavior, audit rules, and state transitions must stay server-side.
- WPF and Web may differ in presentation and local UX behavior, but they must not diverge in command semantics.
- Operator-critical flows can be WPF-first in release 1 while web expands for monitoring and management workflows.

## 2026-04-14 ADR-005: Business commands always enter through the BFF

Decision:
Use `Experience API / BFF` as the only authoritative entry point for MES business commands across both `WPF` and `Web` clients.

Why:
If WPF and Web use different command paths, state transitions, authorization, idempotency, and audit behavior will drift by channel and become difficult to govern.

Implications:
- `Edge` is limited to device-facing concerns such as protocol translation, buffering, and equipment event ingestion.
- `WPF -> Edge` direct communication may exist for device bridge purposes, but it must not become an alternate business command path.
- Command semantics, audit shape, and permission checks must stay identical regardless of client channel.

## 2026-04-14 ADR-006: Release 1 uses WMS-lite reconciliation and MES in-process quality authority

Decision:
For Release 1, keep warehouse stock authority in `WMS`, keep line-side and in-process material execution truth in `MES`, and let `MES` own in-process quality hold/release authority.

Why:
The pilot needs operationally usable material and quality control before full enterprise integration is mature, but it still cannot afford duplicated authority or ad hoc local rules.

Implications:
- Release 1 must define at least a minimal reconciliation loop between WMS and MES.
- Release 1 hold/release decisions that gate next-operation execution must be authoritative in MES.
- Enterprise quality workflows, CAPA, and laboratory processes may remain in QMS/LIMS and be refined later.

## 2026-04-14 ADR-007: Require Korean XML documentation comments in authored C# code

Decision:
Newly created or modified C# types and methods in this repository must include Korean XML documentation comments.

Why:
The repository now mixes MES domain modeling, WPF/Web-facing work, and reusable .NET guidance. Korean XML comments reduce rediscovery cost and keep intent visible directly in the code for future implementation cycles.

Implications:
- `class`, `record`, `struct`, `interface`, `enum`, `method`, and `constructor` changes should carry XML documentation comments in Korean.
- Test code follows the same documentation rule so executable examples stay self-explanatory.
- The canonical guidance lives in `AGENTS.md` and `rules/dotnet/csharp/xml-doc-comments.md`.

## 2026-04-16 ADR-008: Anchor the next implementation work on one operator-execution pilot slice

Decision:
Use `operator execution -> material consumption -> quality gate -> operation completion` as the first implementation-ready pilot slice, and treat material scan validation plus production-actuals preparation as application-layer workflow outputs for that slice rather than immediate `Mes.Domain` aggregate events.

Why:
This slice exercises the most operator-critical MES behavior with the aggregates that already exist in code. It also lets the project derive the first logical data model and BFF payload contracts without forcing broader warehouse or enterprise-quality decisions too early.

Implications:
- The current code vocabulary becomes the canonical baseline for this slice, including `Paused`, `InProcess`, `OverrideRequest`, and `quality-result-recorded`.
- `material-scanned`, `material-validation-passed/failed`, and `production-actuals-ready` stay documented as application or integration workflow outputs until a later cycle proves they belong directly in `Mes.Domain`.
- Next implementation work should translate this slice into concrete application contracts, persistence schema drafts, and workflow coordination logic.

## 2026-04-16 ADR-009: Keep BFF transport contracts outside the domain layer

Decision:
Define the first operator-execution slice payloads and endpoint signatures in a dedicated `Mes.Application.Contracts` project rather than placing transport types inside `Mes.Domain`.

Why:
The current slice now has enough stability to require concrete command and query contracts, but those contracts still represent channel-facing transport concerns such as envelope metadata, endpoint routes, and response shapes. Keeping them outside the domain preserves the domain model as the owner of business state and transitions instead of HTTP or BFF semantics.

Implications:
- `Mes.Domain` remains focused on aggregates, value objects, statuses, and domain events.
- `Mes.Application.Contracts` becomes the canonical home for WPF or Web shared BFF payloads, notification contracts, and endpoint-signature metadata.
- Future application handlers and coordinators should translate between transport contracts and domain objects rather than leaking channel payload structures into domain types.

## 2026-04-16 ADR-010: Prefer concise authored signatures

Decision:
Prefer authored methods, constructors, and public APIs with five or fewer input parameters, and switch to parameter objects, request records, or value objects when more inputs are otherwise needed.

Why:
Long parameter lists are harder to scan, easier to misuse, and create unnecessary friction when reading and maintaining code. A five-parameter preference keeps signatures easier to understand without forbidding framework-driven exceptions.

Implications:
- New application-layer handlers, coordinators, factories, and service methods should avoid long primitive-heavy signatures.
- When an authored signature would exceed five inputs, the default move is to group related fields into a dedicated request or parameter type.
- External framework callbacks, serializer contracts, or library-mandated signatures may exceed this limit when the shape is not under project control.

## 2026-04-16 ADR-011: Harden slice-01 application design before handler scaffolding

Decision:
Before scaffolding the first operator-execution application handlers, first resolve four design gaps in order: the authoritative quality hold coordinator, the MES-side work-queue read-model source, command idempotency semantics, and compact request-envelope shapes.

Why:
The current domain seed, contracts, and persistence draft are strong enough to expose the remaining implementation risk clearly. If handler work starts before those four gaps are closed, the project is likely to grow temporary BFF logic, unstable handler signatures, and inconsistent retry behavior.

Implications:
- Work Unit 1 becomes the quality hold gate coordinator for `QualityRecord` and `OperationExecution`.
- `GetStationWorkQueue` should not depend on ad hoc live upstream master-data lookups; the application design must first define one MES-side source for required-material data.
- Idempotent replay behavior must be explicit before command handlers are treated as production-ready.
- Handler and query scaffolding intentionally moves after the design hardening units rather than happening in parallel.

## 2026-04-16 ADR-012: Make release-1 quality gating and queue sourcing explicit in persisted design

Decision:
For the first operator-execution slice, Release 1 blocking quality outcomes must be materialized into explicit persisted hold state, the operator queue must read required materials from an MES-side requirement snapshot, and command receipts must use a canonical idempotency fingerprint with tighter uniqueness.

Why:
The previous draft left three critical behaviors underspecified: how hold release restores pre-hold execution state after reload, where `GetStationWorkQueue.required_materials` comes from without live upstream joins, and how replay-safe receipts distinguish safe retries from conflicting duplicates.

Implications:
- `operation_execution` persistence now needs `status_before_hold`, `hold_source_type`, and `hold_source_id`.
- `quality_record` persistence now needs to preserve the last decision outcome separately from the current hold state.
- `operation_material_requirement` becomes the MES-side authoritative source for queue material requirements and should be projected from order-release ingestion or operation attachment, not operator commands.
- The Release 1 operator queue should emit `quality_gate_state` as `open` or `hold`, while `review-required` stays reserved for a later supervisory slice.
- `command_receipt` should treat `(channel, command_type, idempotency_key)` as the natural uniqueness scope and store a canonical request fingerprint plus deterministic response payload for replay.

## 2026-04-16 ADR-013: Start the application layer with a coordinator-first MES boundary

Decision:
Implement the first `Mes.Application` code as a workflow coordinator boundary centered on `QualityHoldGateCoordinator`, and keep the Release 1 quality gate rule as an explicit cross-aggregate policy rather than burying it inside future handlers or transport code.

Why:
The current pilot slice already had enough stability to encode the authoritative hold propagation rule, but not enough stability yet to justify full handler or query scaffolding. Starting with a coordinator keeps the ownership of cross-aggregate behavior clear while preserving room to add transport handlers and persistence later without duplicating quality gate logic.

Implications:
- `Mes.Domain` now exposes hold provenance and quality decision outcome as aggregate state so persistence can restore coordinator behavior after reload.
- `Mes.Application` owns the cross-aggregate policy that materializes blocking quality results into `QualityRecord` plus `OperationExecution` hold state and only releases operation hold when provenance matches.
- Future handler and query layers should call the coordinator rather than re-implementing quality gate branching in endpoint-specific code.

## 2026-04-16 ADR-014: Use operation attachment as the first executable anchor for work-queue requirement snapshots

Decision:
Implement the first MES-side `operation_material_requirement` projection from operation attachment in `Mes.Application`, and treat future order-release ingestion as a caller of the same projector rather than a separate requirement-building path.

Why:
The current code seed already has `ProductionOrder.AttachOperation` and `OperationExecution` objects, but it does not yet have an executable order-release ingestion pipeline. Using operation attachment as the first projection anchor keeps Work Unit 2 implementable now without creating a second competing source for `required_materials`.

Implications:
- `GetStationWorkQueue.required_materials` now has one executable MES-owned source in code instead of a placeholder dependency on future ingestion work.
- Future order-release ingestion should reuse the same projector so queue requirements are built once and only once per execution context.
- `quality_gate_state` derivation in the queue should join held `QualityRecord` entries through `WipUnit.current_operation_execution_id` rather than inventing a direct quality-to-operation link for Release 1.

## 2026-04-16 ADR-015: Build command receipt fingerprints from canonical business fields, not transport metadata

Decision:
For the operator-execution slice, keep receipt uniqueness at `channel + command_type + idempotency_key`, and derive `request_fingerprint` from canonical business fields plus actor and station context rather than from the full transport constructor shape.

Why:
The current transport contracts still include metadata that should not redefine business equality, such as `command_id`, `correlation_id`, `client_timestamp`, and optional `revision_refs`. If those fields enter the fingerprint, safe retries and future contract compaction would produce false conflicts even when the business command is unchanged.

Implications:
- Safe retries with the same scope and the same business meaning now replay the stored response even if transport-only metadata changes.
- Same scope with a different business payload now returns an idempotency conflict instead of silently replaying the wrong result.
- Work Unit 4 can compact the contract shape without changing replay semantics, as long as the same canonical business fields remain available.

## 2026-04-16 ADR-016: Compact command contracts with grouped context metadata

Decision:
For the operator-execution slice, represent command transport requests as `Request(Context, Payload)` and shape `CommandContextContract` itself as grouped metadata over `CommandIdentityContract`, `CommandOriginContract`, `ClientTimestamp`, and `RevisionRefs`.

Why:
The previous request contracts repeated nine transport parameters per command constructor, which directly violated the repository preference for authored signatures of five inputs or fewer and made the next handler layer harder to read. Grouping metadata once keeps the contract surface compact without changing command meaning or the canonical fingerprint inputs fixed by ADR-015.

Implications:
- Operator-execution command request types now expose a two-argument constructor of `Context + Payload` instead of repeating flat envelope metadata on every contract.
- Transport metadata remains available through `BffCommandEnvelope` convenience properties so current application code can keep reading `CommandId`, `ActorId`, `Channel`, `StationId`, `CorrelationId`, `IdempotencyKey`, `ClientTimestamp`, and `RevisionRefs` without handler-specific remapping.
- Future handler and endpoint code should accept the compact request contracts as-is rather than reconstructing flat parameter lists.

## 2026-04-16 ADR-017: Keep Work Unit 5 handlers pure over preloaded state bundles

Decision:
Implement the first operator-execution command handlers and work-queue query handler as pure application services over preloaded state bundles and source snapshots, returning replay-ready receipts and production-actuals artifacts rather than directly reaching into repositories or endpoint adapters.

Why:
Work Unit 5 needed an executable application boundary, but the project still has not chosen its persistence or endpoint-adapter shape. Letting handlers depend on repositories now would force infrastructure decisions too early and would risk pushing business branching back into BFF code while the persistence boundary is still unsettled.

Implications:
- `OperatorExecutionCommandHandler` now owns command-level validation, idempotency replay, coordinator invocation, and production-actuals preparation once the relevant aggregates have already been loaded.
- `GetStationWorkQueueQueryHandler` now owns contract mapping over an already assembled MES-side source snapshot instead of reaching outward to live upstream systems.
- The next work unit should add explicit application ports that load handler state bundles, persist receipts, and save post-command changes, while keeping the existing command and query policies unchanged.

## 2026-04-16 ADR-018: Use one application service to orchestrate load, handler invocation, and save through ports

Decision:
Introduce `OperatorExecutionApplicationService` as the adapter-facing orchestration boundary for the current slice, and expose persistence needs through `IOperatorExecutionCommandPort` and `IStationWorkQueueSourcePort` instead of letting endpoint adapters or repositories call handlers directly.

Why:
Once Work Unit 5 made command handlers executable, the next risk was that transport or infrastructure code would start recreating the same load-receipt-handle-save sequence in multiple places. A dedicated application service keeps that sequence explicit, replay-aware, and testable while still deferring concrete persistence technology.

Implications:
- Endpoint adapters should call `OperatorExecutionApplicationService` rather than interacting with `OperatorExecutionCommandHandler` or `GetStationWorkQueueQueryHandler` directly.
- Concrete persistence adapters now have one narrow contract to satisfy: load the typed state bundle or work-queue source, return any existing receipt, and persist the accepted result through one `SaveAsync` call.
- The remaining open design choice is the atomic write boundary inside that `SaveAsync` call, especially once `domain_outbox` publication is wired in.
