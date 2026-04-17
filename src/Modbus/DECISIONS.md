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

## 2026-04-16 ADR-019: Prove the first concrete adapter boundary with a separate in-memory infrastructure layer

Decision:
Introduce a dedicated `Mes.Infrastructure` project as the first concrete implementation of the current operator-execution application ports, and use an in-memory reference adapter plus a thin BFF endpoint adapter to validate the load/save contract before choosing durable persistence technology.

Why:
The application boundary was now stable enough to justify a real adapter, but the project still had not chosen SQL access strategy, transaction plumbing, or an actual HTTP host. Jumping straight to a durable adapter would have mixed persistence-technology decisions with boundary-validation work. A separate in-memory infrastructure layer keeps the contract testable and makes the minimum atomic write set explicit without forcing those later choices yet.

Implications:
- `Mes.Infrastructure` becomes the concrete home for adapter code, leaving `Mes.Application` focused on orchestration and business policy.
- The first reference adapter now proves one write-set boundary for receipts, prepared actuals, and outbox capture, while still relying on store-owned aggregate references rather than detached persistence snapshots.
- The next infrastructure step should either add a thin HTTP host over the endpoint adapter or replace the in-memory store with a durable operational adapter, without changing the current application contracts.

## 2026-04-16 ADR-020: Keep the first Experience API host as a thin composition layer

Decision:
Introduce `Mes.ExperienceApi` as a thin shared BFF host that maps documented operator-execution routes directly to `OperatorExecutionBffEndpointAdapter`, and keep all workflow policy, replay handling, and state orchestration below the host layer.

Why:
The architecture already committed to `Experience API / BFF` as the only authoritative business-command entry point, but there was still no concrete HTTP host proving that the current contracts and endpoint signatures can be exposed without reintroducing business logic into controllers or route handlers. A thin host validates that boundary while keeping persistence technology and richer transport concerns decoupled.

Implications:
- `Mes.ExperienceApi` should stay focused on DI composition, route mapping, and later transport-level concerns such as authentication or error normalization.
- `OperatorExecutionBffEndpointAdapter` remains the immediate host-facing boundary, so route handlers do not duplicate application-service invocation or command/query branching.
- The next major execution step is durable persistence under the existing adapter and host surfaces, followed later by explicit HTTP error mapping and cross-slice transport concerns.

## 2026-04-16 ADR-021: Every project directory must carry and maintain a local README

Decision:
Require every project directory under `src/` and `tests/` to include a local `README.md`, and treat that README as part of the same work unit whenever the project is created or its role and structure change materially.

Why:
The repository has now grown into multiple layers with distinct responsibilities: domain, contracts, application, infrastructure, host, and several test projects. Without local project documentation, future work quickly depends on rediscovery. A per-project README keeps the closest explanation next to the code and lowers onboarding and context-recovery cost.

Implications:
- New project creation is incomplete unless the project-level `README.md` is added in the same change.
- Project-level READMEs should explain at least purpose, responsibility boundary, key classes or files, folder structure, dependency direction, execution or test flow where relevant, and current limitations.
- When a project's role or structure changes materially, its local README should be updated in the same work unit rather than left to drift.

## 2026-04-16 ADR-022: Use a file-backed durable snapshot store as the first operational persistence bridge

Decision:
Keep the existing application ports and endpoint boundary unchanged, add explicit restore boundaries to the current domain types, and implement the first durable persistence adapter as a file-backed JSON snapshot store under `Mes.Infrastructure/OperatorExecution/FileStore`.

Why:
The slice had already proven its load/save contract and write-set boundary through the in-memory reference adapter, but it still lacked detached persistence and restart safety. Jumping directly to the final relational adapter would have coupled several decisions at once: SQL access strategy, transaction plumbing, aggregate reconstruction, and host composition. A file-backed bridge lets the project prove durable replay, snapshot reconstruction, and host-default persistence without widening the scope too early.

Implications:
- `Mes.Domain` now carries explicit `Restore(...)` boundaries for the aggregates and entities that the current slice persists.
- `Mes.Infrastructure` now has two adapter modes: `InMemory/` for reference comparison and `FileStore/` for the default durable operational path.
- `Mes.ExperienceApi` now composes the slice against the file-backed durable adapter by default, while the next persistence step becomes a relational adapter aligned with `docs/mes/persistence-schema-slice-01.sql`.

## 2026-04-17 ADR-023: Keep durable provider selection at the host seam and default it to SQLite

Decision:
Keep durable provider selection inside `Mes.ExperienceApi` service registration, run the current slice on `Sqlite` by default, keep `FileStore` available beside it, and reserve `Postgres` as the next relational provider slot.

Why:
The current slice now has two proven durable implementations with the same application-facing behavior: `FileStore` and `Sqlite`. The user direction is to run on SQLite now while keeping database replacement cheap later. The least disruptive place to make that choice is the host composition seam, not the application layer or route handlers.

Implications:
- `Mes.Application` and its ports stay unchanged when the durable provider changes.
- `Mes.ExperienceApi` now chooses the provider from configuration instead of hard-wiring one durable store.
- `Sqlite` is the current default runtime for the slice, while `FileStore` remains available through the same seam.
- `Postgres` and `Mes:OperatorExecutionConnectionString` are now explicit reserved extension points for a future provider, but they do not imply that a PostgreSQL adapter exists yet.

Note:
ADR-024 later narrows the operational stance further by treating `FileStore` as comparison-only while SQLite remains the active runtime.

## 2026-04-17 ADR-024: Treat FileStore as comparison-only while SQLite is the active runtime

Decision:
Keep `Sqlite` as the only active durable runtime for slice 01, and treat `FileStore` as an explicitly selected comparison and recovery-inspection path rather than an operational fallback.

Why:
The user direction is to proceed on SQLite now, while still leaving room for a future PostgreSQL provider. Leaving `FileStore` described as a general fallback keeps the runtime stance ambiguous and increases the chance that old file-store settings or ad hoc host changes quietly drift the slice away from the intended relational path.

Implications:
- Host and project documentation should describe `FileStore` as comparison-only, not as the preferred rollback target.
- Host smoke tests should prove that the default runtime stays on `Sqlite` even when legacy file-store path configuration is present.
- Future provider work should target PostgreSQL on the existing seam; `FileStore` remains useful only while the team still wants a second durable parity path beside SQLite.

## 2026-04-17 ADR-025: Pilot hardening stays inside the current slice seams

Decision:
Treat the next pilot-hardening cycle as four bounded work units on top of the existing operator-execution slice: cross-operation order completion progression, canonical save transaction definition, Experience API error normalization, and a future PostgreSQL handoff contract.

Why:
The current slice already proves the main operator-execution loop, replay safety, and SQLite durability. The highest remaining risk is not missing architecture, but missing pilot-grade rules around completion truth, commit boundaries, operable HTTP failures, and future provider replacement. Those gaps should be closed without changing the current separation of responsibilities.

Implications:
- `Mes.Application` remains the owner of cross-aggregate order progression and the logical save contract.
- `Mes.Infrastructure` remains the owner of provider-specific transaction and schema behavior under that same logical contract.
- `Mes.ExperienceApi` remains the owner of HTTP problem-details mapping and transport semantics, without reintroducing workflow logic into routes.
- Future PostgreSQL work must plug into the existing provider seam only after Work Units 6 through 8 raise the slice to a pilot-safe baseline.

## 2026-04-17 ADR-026: Derive order completion progression from MES-owned sibling-operation summary

Decision:
For `complete-operation`, keep `OperationExecution.Complete(...)` aggregate-local, but derive `ProductionOrder` progression in `Mes.Application` from a command-port-loaded `OrderCompletionProgressSnapshot` built from MES-owned `operation_execution` state.

Why:
The handler previously knew only the parent order and the current operation, which was not enough evidence to decide whether sibling operations still remained open. Loading one authoritative summary keeps the application rule explicit without widening the seam into full sibling aggregate hydration or pushing completion truth back into ERP or BFF code.

Implications:
- `CompleteOperationCommandState` now carries an `OrderCompletionProgressSnapshot`, and every concrete command port must load that summary before calling the handler.
- Release 1 currently treats only `OperationExecutionStatus.Done` as completed for order progression; all other statuses still keep the order open.
- `ProductionOrder` now moves to `PartiallyCompleted` or `Completed` inside the same accepted command path that already persists the operation completion, receipt, prepared actuals, and outbox side effects.

## 2026-04-17 ADR-027: Treat the accepted-command write-set as the provider-neutral pilot save contract

Decision:
Define the current pilot save contract as one accepted-command write-set that keeps touched aggregate state, `command_receipt`, `production_actuals_batch`, and the full `domain_outbox` set aligned across `InMemory`, `FileStore`, and `Sqlite`.

Why:
Work Unit 7 showed that the code already shared one logical save boundary, but the project still described it partly as an implementation detail. Locking that boundary in provider-facing tests and adapter documentation turns it into an explicit contract instead of a coincidence of today's adapters.

Implications:
- Provider-specific helper rows or metadata may exist, but they must not widen or replace the application-visible write-set.
- Replay of an existing receipt must not open a new logical save boundary or create new persisted side effects.
- Future slice growth, such as `material_consumption` or `override_request`, should widen this contract only through a deliberate design-and-test update rather than by adapter drift.

## 2026-04-17 ADR-028: Keep Experience API failure semantics in one host-level problem-details mapper

Decision:
Promote deterministic operator-execution failures into typed application exceptions, and let `Mes.ExperienceApi` own the stable HTTP mapping for `400`, `404`, `409`, `422`, and fallback `500` through one host-level problem-details seam.

Why:
The operator-execution slice now has enough durable and replay-safe behavior that transport failures must also become diagnosable and stable. Letting route handlers or adapters improvise HTTP responses would duplicate transport policy, while leaving raw framework exceptions in place would make the first pilot host brittle and hard to operate.

Implications:
- `Mes.Application` and `Mes.Infrastructure` should surface deterministic not-found, conflict, and validation outcomes through the typed operator-execution exception taxonomy instead of leaking provider-specific exceptions upward.
- `Mes.ExperienceApi` route handlers stay thin and delegate to the existing endpoint adapter, while one host-level mapper turns those exceptions into stable problem-details payloads with slice-specific error codes and context extensions.
- Broader cross-slice transport standardization can widen this mapper later without changing the current application or persistence seams.

## 2026-04-17 ADR-029: Treat the first pilot profile as a documented working assumption, not an implicit guess

Decision:
Until a real pilot line and representative product family are selected, treat the current executable target as a `station-based discrete-dominant hybrid` pilot with `lot-first genealogy plus serial-capable WIP identity`, and do not assume that direct Modbus or PLC handshake is mandatory for the first operator-execution rollout.

Why:
The current codebase already carries strong implicit signals about the intended pilot shape: station queue execution, WPF-first operator flow, lot-to-WIP genealogy, MES-owned in-process hold/release, and no equipment-specific completion handshake in the command contracts. Leaving those signals undocumented would make the next WPF, edge, and slice decisions drift around unstated assumptions.

Implications:
- `docs/mes/pilot-profile-working-assumptions.md` becomes the canonical place to state these assumptions until external pilot validation replaces them with plant-confirmed facts.
- Current slice work should stay optimized for station-based operator execution and lot-first genealogy unless the selected pilot line proves that serial-per-piece traceability or batch/process control is mandatory on day one.
- Modbus or PLC handshake work should remain a bounded follow-up behind pilot equipment validation rather than widening the current operator-execution baseline by default.

## 2026-04-17 ADR-030: Start the WPF station client as a thin queue-first shell over the shared BFF

Decision:
Introduce the first executable `Mes.Client.Wpf` implementation as a thin Generic Host based station shell that binds a station, reads the operator work queue through the shared operator-execution BFF, and normalizes problem-details into operator-facing messages before adding command-heavy or device-specific features.

Why:
The current backend seams are now stable enough to support a real WPF client, but the lowest-risk way to prove that client boundary is to start with one read-only queue flow. That keeps the architecture honest by exercising the same `Experience API / BFF` path the future WPF command screens must use, without prematurely widening into direct application, database, or PLC integration.

Implications:
- `Mes.Client.Wpf` should consume `Mes.Application.Contracts` plus `Mes.ExperienceApi` semantics through typed HTTP access rather than referencing domain or infrastructure behavior directly.
- Future WPF command screens must reuse the same BFF seam and add shared command-context generation instead of inventing alternate command paths.
- Peripheral, Modbus, and offline concerns remain follow-up extensions around the station shell, not reasons to bypass the shared server-side command boundary.

## 2026-04-17 ADR-031: Reset the displayed queue on station rebind and normalize client-local failures in the WPF shell

Decision:
When the WPF station shell binds a different station, clear the currently displayed queue snapshot immediately, keep the source station visible for any loaded snapshot, and convert client-local failures such as connectivity loss, empty success bodies, malformed success bodies, and unexpected async-command exceptions into explicit operator-visible error states.

Why:
The queue-first shell proved the shared BFF seam, but it still carried two pilot-grade safety risks: a newly bound station could inherit another station's displayed queue until the next refresh, and malformed or locally thrown client errors could escape as crashes or as misleading generic connection failures. A station-facing MES client should fail loudly and specifically without hiding stale work context.

Implications:
- `ShellViewModel` now treats station rebind as a context reset for the displayed queue snapshot.
- The shell panel now reflects message severity so warnings and errors are no longer visually flattened.
- `OperatorExecutionStationClient` now classifies client-local failures separately from server problem-details responses, and the WPF tests lock that behavior through fake-HTTP coverage plus shell-state tests.

## 2026-04-17 ADR-032: Centralize WPF command context around one per-operation station policy

Decision:
Generate WPF-side command metadata through one `StationCommandContextFactory`, using a configured default actor id, one per-operation correlation id of `wpf:{stationId}:{operationExecutionId}`, and one command-type-specific idempotency key of `wpf:{commandType}:{stationId}:{operationExecutionId}` for the current station mutation flows.

Why:
Once `Mes.Client.Wpf` widened from queue reads into `start-operation` and `complete-operation`, the next risk was that each screen or command path would invent its own `commandId`, `correlationId`, and `idempotencyKey` policy. The pilot already depends on stable replay semantics and audit-friendly transport metadata, so the client needed one shared rule before more command screens land.

Implications:
- Future WPF mutations should request `CommandContextContract` from the shared factory instead of constructing transport metadata ad hoc inside screens or HTTP callers.
- Retries of the same command type against the same station-bound operation now reuse the same idempotency scope while still getting a fresh `commandId` per send attempt.
- The current WPF shell still leaves `revision_refs` null and uses a configured default actor id until authentication and richer revision-aware workflows become active requirements.

## 2026-04-17 ADR-033: Manage repository NuGet package versions through one root Directory.Packages.props

Decision:
Manage NuGet versions through the root `Directory.Packages.props`, remove duplicated version numbers from child `PackageReference` items, and keep package-version differences that still matter for repo-local skill templates expressed as conditioned `PackageVersion` entries in that one central file.

Why:
The repository had started to repeat the same test and infrastructure package versions across multiple solution projects, while the repo-local skill templates carried their own package references with only a few intentional version differences. Leaving versions scattered would increase drift risk and make later upgrades noisier than necessary.

Implications:
- Solution-facing project files now keep `PackageReference` items versionless, with the authoritative version list living in `Directory.Packages.props`.
- Repo-local skill templates can remain on intentionally different package versions without reintroducing version literals into every child project file, because the central package file now carries conditioned entries for those template project names.
- Future package upgrades should start in `Directory.Packages.props`, and validation should include at least `dotnet build Mes.slnx -v minimal` plus `dotnet test Mes.slnx -v minimal`.

## 2026-04-17 ADR-034: Give WPF material-consumption retries a per-action idempotency token

Decision:
Keep `StationCommandContextFactory` as the single WPF command-context policy, but let `record-material-consumption` append one per-action token to the shared per-operation idempotency base of `wpf:{commandType}:{stationId}:{operationExecutionId}` while leaving correlation fixed at `wpf:{stationId}:{operationExecutionId}`.

Why:
The first WPF command flows used one deterministic per-operation idempotency key, which fits `start-operation` and `complete-operation` because those actions are effectively single-shot for a given operation state. `record-material-consumption` is different: the same running operation can legitimately receive multiple accepted material-consumption commands. Reusing the old per-operation idempotency key unchanged would collapse distinct operator actions into one replay scope and make a valid second material shot look like a duplicate.

Implications:
- `ShellViewModel` now keeps one material-consumption action token stable only while the operator retries the same filled form; changing the selected queue item or any material-consumption input resets that token.
- WPF retries of the same material-consumption attempt now stay replay-safe, while distinct material shots on the same station-bound operation get different idempotency keys without inventing a second command-context factory.
- The shared queue contract remains unchanged in this work unit; until a richer scan or selection flow exists, the WPF shell derives default material code and unit hints from the first required-material entry and still asks the operator for WIP, lot, and quantity input explicitly.

## 2026-04-17 ADR-035: Project completion unit from operation_execution into the shared station queue

Decision:
Treat `operation_execution.quantity_unit` as the authoritative completion unit for the operator queue, project that value into each `WorkQueueItemContract`, and let `Mes.Client.Wpf` display it as a read-only `complete-operation` unit instead of asking the operator to type the unit manually.

Why:
The current slice already made `OperationExecution` the authoritative execution unit and already validated completion quantities against that aggregate's unit. Leaving the WPF shell on a manually typed completion unit added avoidable operator friction and reopened a mismatch path that the backend would only reject after the operator had already entered a command. Projecting the unit through the existing queue read-model keeps one MES-side owner for the field and removes that avoidable UX failure from the first station shell.

Implications:
- `GetStationWorkQueue` now carries one new queue field for the operation quantity unit, sourced directly from `operation_execution.quantity_unit` in the application query layer.
- `Mes.Client.Wpf` still keeps `DefaultCompletionQuantityUnit` only as a shell fallback when no queue item is selected, but the normal `complete-operation` flow now uses the queue-projected unit and exposes it as read-only.
- Future channels such as Web should consume the same queue field rather than re-deriving or locally configuring a completion unit, so BFF payload semantics stay aligned across shells.

## 2026-04-17 ADR-036: Keep no-equipment validation as an example-layer mock station over the existing BFF seam

Decision:
Provide no-equipment validation through `example/Mes.MockStation.Example`, which seeds the current SQLite-backed operator-execution scenario and drives the existing `Mes.ExperienceApi` plus `Mes.Client.Wpf` seam, rather than adding a special fake-device execution path inside `Mes.Domain`, `Mes.Application`, or the station client itself.

Why:
The project needed one runnable path for local validation without real equipment, but embedding simulator-specific branches into the core MES layers would blur the current responsibility boundaries and create behavior that production code would never use. An example-layer mock keeps the validation executable while preserving the same thin BFF path that the real station client must use later.

Implications:
- `example/Mes.MockStation.Example` now owns the seeded manifest, SQLite scenario generation, and the PowerShell launch/smoke scripts for local validation.
- Headless no-equipment regression checks should prefer `powershell -File example/Mes.MockStation.Example/Test-MockStationDemo.ps1` and the paired `Mes.ExperienceApi.Tests` smoke coverage instead of introducing alternate mock-only command routes.
- Future equipment simulators should stay at the example or edge boundary unless a real domain capability requires a first-class simulator concept.

## 2026-04-17 ADR-037: Treat executable code spellings as the canonical machine-facing MES vocabulary

Decision:
Use the exact command names and enum spellings already executed in `src/Mes.Domain` and the shared contracts as the canonical machine-facing MES vocabulary for current architecture, slice, and implementation documents.

Why:
The current plan review showed one concrete terminology drift that would keep repeating if left unresolved: architecture and implementation notes still mixed shorthand names like `record-consumption` with the executable command `record-material-consumption`, and prose state labels such as `In Progress` or `In Inspection` with the code-level names that payload specs and persistence drafts already depend on. Leaving both forms active would make future API, read-model, and persistence work noisier and more error-prone.

Implications:
- Machine-facing docs should now prefer `record-material-consumption`, `InProgress`, `PartiallyCompleted`, `InProcess`, `InInspection`, and `Done` when they describe executable contracts or persisted state.
- Prose explanations may still use natural-language wording, but any payload, workflow, persistence, or state table should align to the executable code spelling.
- The next priority still remains pilot-line selection and external workflow validation; this ADR only removes internal vocabulary drift so later validation can focus on real plant questions instead of document/code mismatch.

## 2026-04-17 ADR-038: Repair mojibake MES docs by rewriting them as readable UTF-8, not by blind transcoding

Decision:
When a MES design document is already carrying corrupted Korean text instead of merely using the wrong save encoding, repair it by restoring readable content and saving the file as UTF-8 rather than by applying a blind encoding conversion step.

Why:
The inspection for `docs/mes/architecture-blueprint.md` and `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md` showed that the problem was not just a different file encoding setting. The stored content itself had already drifted into mojibake, so a no-op re-save or a cp949-to-utf8 style transcode would only preserve unreadable text.

Implications:
- Encoding repair for affected MES docs now includes content verification against current architecture and plan intent instead of treating the problem as a purely mechanical file-format conversion.
- Restored files should be saved as UTF-8 and checked for expected Korean byte sequences so future tool output issues are not confused with actual source corruption.
- The remaining `docs/mes/*.md` files should be audited with the same rule, and documents that still show mojibake should be queued as explicit follow-up work rather than silently left in place.

## 2026-04-17 ADR-039: Separate executable command vocabulary from planned expansion commands in MES docs

Decision:
When a MES command or event catalog spans both current slice behavior and future rollout ideas, the document must explicitly distinguish executable-now commands from planned expansion commands instead of presenting them as one undifferentiated active catalog.

Why:
The design review showed that the previous command catalog mixed currently routable operator-execution commands with future pause, scrap, override, and discrepancy commands. That made it harder to tell what is already proven in `Mes.Application.Contracts`, `Mes.ExperienceApi`, and the WPF shell versus what still depends on later release decisions.

Implications:
- `docs/mes/command-event-catalog.md` should now name the executable command set separately from planned next-slice commands.
- WPF, Web, and rollout planning should treat the executable-now set as the real delivery baseline and the planned set as deferred work until shared contracts and routes exist.
- Future command promotion work should update both the code contracts and the command catalog distinction in the same work unit.
