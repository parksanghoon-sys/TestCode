# Pilot Slice 01: Application Design Hardening

## 1. Context and explicit assumptions

This document tightens the next implementation plan for the first operator-execution slice after reviewing the current domain seed, BFF contracts, and persistence draft.

- The slice scope remains `operator execution -> material consumption -> quality gate -> operation completion`.
- `Mes.Domain` remains the owner of aggregate-local state transitions.
- The application layer is the owner of cross-aggregate coordination, command idempotency, query composition, and production-actuals preparation.
- The current pilot assumptions still target a discrete or hybrid station-based line, but pilot-line validation remains an external follow-up.
- Release 1 allows only one active operation-level hold source per `OperationExecution`.
- Release 1 blocking quality outcomes must be materialized into an explicit quality-gate hold inside the same application command boundary rather than relying on an asynchronous follow-up.
- `review-required` stays reserved for a later supervisory flow and is not emitted as an operator-blocking state in the first operator queue implementation.

## 2. Review-driven risks to resolve before handler coding

| Risk | Why it matters | Minimum safe response |
|---|---|---|
| No authoritative owner for quality hold propagation | `QualityRecord` and `OperationExecution` can drift apart | define one application workflow coordinator first |
| No concrete source for `required_materials` in `GetStationWorkQueue` | BFF could fall back to ad hoc joins or upstream lookups | define one MES-side requirement snapshot source first |
| Idempotency scope is underspecified | retries and replay can conflict or return inconsistent outcomes | define receipt uniqueness, fingerprinting, and replay behavior first |
| Transport contracts still use long authored signatures | the next application layer would repeat the same readability problem | compact the envelope shape before handlers spread it further |

## 3. Boundary and ownership matrix

| Concern | Authoritative owner | Notes |
|---|---|---|
| Aggregate-local state changes | `Mes.Domain` aggregates | `ProductionOrder`, `OperationExecution`, `MaterialLot`, `QualityRecord`, `OverrideRequest`, `WipUnit` |
| Quality hold and release propagation | application workflow coordinator | reacts across `QualityRecord` and `OperationExecution` |
| Station work queue query shape | application query layer | composes MES-side execution, quality, and material requirement data |
| Material requirement source for queue UI | MES-side requirement snapshot | avoids live BFF dependency on upstream master-data lookups |
| Idempotency receipt and replay | application command pipeline | owns dedupe, conflict detection, and stored result lookup |
| Production actuals preparation | application projection or integration workflow | derived after authoritative operation completion |

## 4. Recommended work-unit sequence

### Work Unit 1: Quality Hold Gate Coordinator

Objective:
Define and implement the first authoritative application workflow coordinator for quality-gated operation execution.

Scope:
- `place-hold` on `QualityRecord`
- `release-hold` on `QualityRecord`
- `record-quality-result`
- `complete-operation` pre-check behavior

Coordinator rules:
- If `record-quality-result` records a blocking outcome for Release 1, the coordinator must materialize that outcome into a `QualityRecord` hold and a gated `OperationExecution` hold before the command completes.
- If `QualityRecord` enters `Hold`, the coordinator must place `OperationExecution` on hold when the operation is still active.
- If `QualityRecord` is released, the coordinator must release `OperationExecution` only when the persisted hold source points back to that `QualityRecord`.
- Work Unit 1 must persist `status_before_hold`, `hold_source_type`, and `hold_source_id` for `OperationExecution` so `ReleaseHold` can restore the correct state after reload or retry.
- Work Unit 1 must persist the last blocking or non-blocking quality decision separately from the current `QualityRecord` status so a failed decision is not lost when the record enters `Hold`.
- `complete-operation` remains blocked unless the operation is no longer held.

Acceptance:
- One coordinator-focused happy-path test.
- One replay or duplicate-command test.
- One mismatch test where quality state and operation state are intentionally out of sync and the coordinator reconciliation rule is clear.
- One schema or logical-model delta that persists hold provenance and the last quality decision outcome.

### Work Unit 2: Station Work Queue Read Model Source

Objective:
Make `GetStationWorkQueue` implementable without ad hoc upstream lookups.

Recommended minimum design:
- Add one MES-side `operation_material_requirement` snapshot source keyed by `operation_execution_id`.
- Populate it from order-release ingestion or operation-attachment projection, not from operator command envelopes.
- In the current code seed, use operation-attachment projection as the first executable anchor and let later order-release ingestion reuse the same projector.
- Build `required_materials` for the queue from this snapshot, not from live BFF joins to external master-data systems.

Recommended server-side `quality_gate_state` derivation:
- `hold` when `OperationExecution` is held or when a held `QualityRecord` is linked to the operation through `WipUnit.current_operation_execution_id`.
- `open` otherwise for Release 1 operator execution.
- `review-required` stays reserved for a future supervisory or exception-review slice and is not emitted by the first operator queue implementation.

Acceptance:
- One query contract note that names the authoritative source for every returned field.
- One schema delta or logical-model delta for the requirement snapshot.

### Work Unit 3: Idempotent Command Pipeline

Objective:
Define replay-safe command handling before endpoint handlers exist.

Recommended minimum design:
- Tighten `command_receipt` uniqueness from `channel + idempotency_key` to `channel + command_type + idempotency_key`.
- Add a stored request fingerprint built from canonical business fields rather than the current transport constructor shape so Work Unit 4 can compact contracts without invalidating replay semantics.
- Store enough normalized response data to return the same accepted result on safe retries.

Replay rules:
- Same key and same fingerprint returns the stored result.
- Same key and different fingerprint returns an idempotency conflict.
- Failed validation before aggregate mutation may still be recorded as a deterministic receipt when the project wants replay visibility.

Recommended receipt lifecycle table:

| Existing receipt | Incoming scope | Incoming fingerprint | Outcome |
|---|---|---|---|
| none | new | new | accept and persist new receipt |
| same scope | same as stored | same as stored | replay stored response |
| same scope | same as stored | different from stored | return idempotency conflict |

Acceptance:
- One receipt lifecycle diagram or table.
- One conflict test and one replay test.

### Work Unit 4: Compact Contract Shapes

Objective:
Bring `Mes.Application.Contracts` back in line with the repository rule that authored signatures should stay at five or fewer inputs.

Recommended minimum design:
- Introduce one `CommandContextContract` for envelope metadata.
- Shape that context as `CommandContextContract(Identity, Origin, ClientTimestamp, RevisionRefs)` so the context type itself also stays within the repository signature rule.
- Change each command request into a compact request shape such as `Request(Context, Payload)`.
- Keep endpoint semantics unchanged so WPF and Web still share the same business command meaning.

Acceptance:
- The next authored public contract constructors stay within the repository guidance.
- Existing payload semantics remain unchanged.

### Work Unit 5: Handler and Query Skeletons

Objective:
Only after the first four work units are stable, scaffold the application-layer handlers and station query path.

Scope:
- command handler skeletons
- coordinator invocation points
- query handler or read service for `GetStationWorkQueue`
- production-actuals preparation skeleton

Acceptance:
- buildable application project boundary
- coordinator tests and query tests
- no BFF-only business logic leakage

## 5. Material failure modes to keep visible

| Trigger | Impact | Mitigation |
|---|---|---|
| Quality hold is persisted but operation hold is not | completion may be wrongly allowed | Work Unit 1 must define authoritative reconciliation |
| Work queue relies on live upstream material lookup | operators see inconsistent requirements | Work Unit 2 must define MES-side requirement snapshot ownership |
| Duplicate idempotency keys arrive with different payloads | retries become unsafe and audit is ambiguous | Work Unit 3 must define fingerprint conflict handling |
| Long request constructors spread through handlers | application code becomes harder to scan and easier to misuse | Work Unit 4 must compact the request shape first |
| A failed quality result is overwritten by later hold state | the operator gate blocks correctly but the inspection outcome becomes ambiguous | Work Unit 1 must persist decision outcome separately from current hold state |

## 6. Non-blocking external validations that still remain

- pilot manufacturing mode and genealogy depth
- pilot line and representative product family
- upstream ownership of item, BOM, routing, resource, and quality master data
- final WPF versus Web workflow allocation

These validations still matter, but they do not need to block Work Units 1 through 4 unless they materially change the first slice boundary.

## 7. Current executable persistence boundary

- `Mes.Application` now owns the operator-execution workflow policy, replay semantics, and query composition.
- `Mes.Infrastructure` now carries three concrete adapter paths for the slice.
- `InMemory/` remains the reference adapter for boundary comparison and focused tests.
- `FileStore/` remains available only as a comparison-oriented durable path for reload-safe parity checks and recovery inspection.
- `Sqlite/` is now the current default durable runtime for slice 01.
- `Mes.Domain` now exposes explicit restore boundaries so detached persistence snapshots can reconstruct `ProductionOrder`, `OperationExecution`, `MaterialLot`, `QualityRecord`, and `WipUnit` without replaying business commands.
- `Mes.ExperienceApi` now selects the durable provider at the host seam while keeping the same thin endpoint-adapter boundary and the same application ports.
- The current `Sqlite` runtime proves the executable relational subset of slice 01, while `material_consumption` and `override_request` remain explicitly deferred relational targets for a later slice expansion.
- `Postgres` is now reserved as the next relational provider slot, but no PostgreSQL adapter exists yet.

## 8. Pilot hardening work units

This section closes the remaining gap between an executable reference slice and a pilot-ready Execution MVP.
The goal is to harden the current slice without widening its system boundary or replacing the existing application seam.

### Work Unit 6: Order completion progression rule

Objective:
Promote `complete-operation` from an operation-local completion event into a pilot-safe order progression rule that can update `ProductionOrder` without guessing from partial state.

System boundary and ownership:

| Concern | Owner | Why |
|---|---|---|
| `OperationExecution.Complete(...)` local state transition | `Mes.Domain` | good quantity, completion timestamp, and local completion event remain aggregate-local |
| `ProductionOrder` progression after one operation completes | `Mes.Application` | it depends on sibling-operation visibility beyond the current aggregate |
| sibling-operation completion summary source | `IOperatorExecutionCommandPort` | infrastructure must load the minimum authoritative summary for the application layer |

Recommended minimum authoritative source:

- Keep the current `CompleteOperationCommandState` load boundary, but extend it with one order-level progression summary rather than loading every sibling aggregate in full.
- Introduce an application-facing summary such as `OrderCompletionProgressSnapshot` with at least:
  - `production_order_id`
  - `total_operation_count`
  - `remaining_open_operation_count_excluding_current`
  - `completed_operation_count_including_current_after_accept`
- The source of truth for that summary should be the same persisted `operation_execution` state already owned by MES, not an ERP-side order status or a BFF-side derived count.

Canonical progression rule for Release 1:

1. `complete-operation` first validates the current operation and quality gate exactly as it does today.
2. After the current `OperationExecution` is accepted as completed, the application evaluates remaining attached operations for the same `ProductionOrder`.
3. If `remaining_open_operation_count_excluding_current > 0`, set `ProductionOrder` to `PartiallyCompleted`.
4. If `remaining_open_operation_count_excluding_current == 0`, set `ProductionOrder` to `Completed`.
5. `ProductionOrder` does not gain a new intermediate state for this slice; it continues to use `Dispatched -> InProgress -> PartiallyCompleted -> Completed`.
6. The next started sibling operation may still move the order from `PartiallyCompleted` back into `InProgress`, which is acceptable for Release 1 because `PartiallyCompleted` means "some operations completed, order still not finished", not "no active work remains forever."
7. Release 1 treats only `OperationExecutionStatus.Done` as completed for this summary; `Queued`, `Running`, `Paused`, `Hold`, `Rework`, and `Aborted` all keep the order open until a later slice defines a richer terminal-state rule.

Recommended acceptance coverage:

- single-operation order completion promotes the order to `Completed`
- first completion in a multi-operation order promotes the order to `PartiallyCompleted`
- final sibling completion promotes the order from `PartiallyCompleted` to `Completed`
- replay of the same `complete-operation` command does not double-advance the order

Material failure modes:

| Trigger | Impact | Mitigation |
|---|---|---|
| completion handler only sees the current operation | order may be marked `Completed` too early | load one authoritative sibling-progress summary before deciding order status |
| order status is derived from ERP release data instead of MES execution state | MES and runtime truth can drift | derive progression only from MES-owned `operation_execution` state |
| final completion updates `operation_execution` but not `production_order` | operator flow passes while ERP actual posting sees stale order state | keep order progression inside the same accepted command save boundary |

### Work Unit 7: Canonical save transaction boundary

Objective:
Turn the currently proven write-set into the explicit pilot save contract so all durable providers commit the same business boundary.

System boundary and ownership:

| Concern | Owner | Why |
|---|---|---|
| logical save-set definition | `Mes.Application` design contract | this is part of slice behavior, not a provider detail |
| physical database or file transaction | `Mes.Infrastructure` provider | SQLite, FileStore, and future PostgreSQL each enforce the same boundary differently |
| post-commit publication | outbox consumer or integration workflow | publication timing must not widen the command transaction |

Canonical atomic write-set for an accepted mutating command:

| Record | Required when | Why it stays in the same commit |
|---|---|---|
| touched aggregate snapshots (`ProductionOrder`, `OperationExecution`, `WipUnit`, `MaterialLot`, `QualityRecord`) | whenever the command mutates them | state truth must commit or roll back together |
| `command_receipt` | whenever the command is accepted | replay safety must match the committed state |
| `production_actuals_batch` | `complete-operation` only | prepared actuals must describe the exact completed state that was accepted |
| `domain_outbox` rows | whenever aggregates raised domain events | publication candidates must reflect the same accepted command result |

Explicit exclusions for the current slice:

- external ERP actual posting
- outbox publication dispatch
- `material_consumption` as a separate relational table
- `override_request` as a separate relational table

These remain outside the pilot save boundary until the application layer starts mutating them as first-class persisted side effects.

Provider rules:

1. Replayed commands do not open a new save transaction.
2. Idempotency conflicts do not persist any new state.
3. An accepted command either commits the full write-set above or commits none of it.
4. Provider-specific helper rows, such as SQLite metadata tables, may exist, but they cannot change the logical acceptance contract exposed to the application layer.

Material failure modes:

| Trigger | Impact | Mitigation |
|---|---|---|
| aggregate state commits without `command_receipt` | retries can re-execute accepted work | keep receipt in the same atomic boundary |
| `command_receipt` commits without aggregate state | future retries falsely replay an outcome that never happened | never split receipt persistence from state persistence |
| outbox commits outside the accepted state transaction | downstream systems can see ghost events or miss real ones | persist outbox rows in the same transaction and publish later |
| prepared actuals commit separately from completion | actual posting can drift from MES execution truth | keep `production_actuals_batch` in the same boundary as completion |

Recommended acceptance coverage:

- one provider-neutral documentation note naming the canonical write-set
- one adapter test that proves no new save occurs on replay
- one adapter test that proves `command_receipt`, prepared actuals, and outbox stay aligned for accepted completion

### Work Unit 8: Experience API error normalization

Objective:
Keep the thin host thin, but make deterministic operator-facing failures return stable HTTP semantics instead of generic framework exceptions.

System boundary and ownership:

| Concern | Owner | Why |
|---|---|---|
| business validation and state failure detection | `Mes.Application` | the application layer already knows why a command cannot proceed |
| transport-level HTTP mapping | `Mes.ExperienceApi` | route handlers and host middleware own HTTP semantics |
| persistence lookup misses | infrastructure translated into typed application exceptions | not-found should not leak as raw `KeyNotFoundException` |

Recommended exception taxonomy:

- `OperatorExecutionNotFoundException`
  - authoritative aggregate or query target does not exist
- `OperatorExecutionConflictException`
  - idempotency conflict
  - illegal current state transition
  - quality gate currently blocks completion
- `OperatorExecutionValidationException`
  - payload is structurally present but semantically invalid against authoritative MES state
  - examples: unit mismatch, material code mismatch, inspection code mismatch, subject-state mismatch

Recommended HTTP mapping:

| Failure kind | HTTP status | Stable error code |
|---|---|---|
| malformed JSON, missing required transport field, route or query binding failure | `400 Bad Request` | `transport.invalid_request` |
| authoritative aggregate or query target not found | `404 Not Found` | `operator_execution.not_found` |
| idempotency conflict or current-state conflict | `409 Conflict` | `operator_execution.conflict` |
| semantic business validation failure against authoritative state | `422 Unprocessable Entity` | `operator_execution.validation_failed` |
| unexpected exception | `500 Internal Server Error` | `system.unexpected_error` |

Recommended `ProblemDetails` extension fields:

- `errorCode`
- `traceId`
- `commandId` when present
- `aggregateType` when known
- `aggregateId` when known
- `idempotencyKey` when present and safe to echo

Implementation seam:

- Keep route handlers unchanged except for using one shared host-level exception mapping policy.
- Do not parse exception messages at the HTTP layer.
- Convert deterministic infrastructure misses and idempotency conflicts into typed exceptions before they reach minimal API.
- Preserve the same error codes for both WPF and Web so channel UX diverges without forking business semantics.

Material failure modes:

| Trigger | Impact | Mitigation |
|---|---|---|
| raw `DomainException` and `KeyNotFoundException` leak through minimal API | operator clients get unstable or host-default responses | introduce typed exceptions and one host-level mapper |
| idempotency conflict returns the same shape as validation failure | retry behavior becomes ambiguous | keep `409` and a dedicated `operator_execution.conflict` code |
| not-found and validation are both surfaced as `500` | diagnostics and operator guidance degrade | map deterministic failures before generic exception handling |

Recommended acceptance coverage:

- one route test for `404`
- one route test for `409`
- one route test for `422`
- one route test for fallback `500` problem details

### Work Unit 9: Future PostgreSQL provider handoff contract

Objective:
Define the minimum contract that lets the team replace SQLite later without changing application orchestration, route handlers, or command semantics.

System boundary and ownership:

| Concern | Owner | Why |
|---|---|---|
| provider selection | `Mes.ExperienceApi` host seam | runtime choice belongs to composition, not application logic |
| provider behavior | `Mes.Infrastructure/OperatorExecution/Postgres/` | all PostgreSQL-specific SQL, mapping, and migration behavior stays here |
| logical slice behavior | `Mes.Application` | the provider swap must not change command or query semantics |

Recommended handoff contract:

1. Configuration contract
   - `Mes:OperatorExecutionDurableProvider=Postgres`
   - `Mes:OperatorExecutionConnectionString=<postgres connection string>`
   - existing SQLite and FileStore settings remain valid for their own providers only
2. Port contract
   - implement `IOperatorExecutionCommandPort`
   - implement `IStationWorkQueueSourcePort`
   - keep `OperatorExecutionApplicationService` unchanged
3. Persistence contract
   - support the same executable table set currently proven by SQLite:
     - `production_order`
     - `operation_execution`
     - `operation_material_requirement`
     - `wip_unit`
     - `material_lot`
     - `genealogy_link`
     - `quality_record`
     - `command_receipt`
     - `domain_outbox`
     - `production_actuals_batch`
   - `material_consumption` and `override_request` remain deferred until the application layer starts persisting them
4. Transaction contract
   - preserve the same canonical atomic write-set defined in Work Unit 7
   - enforce receipt natural uniqueness in PostgreSQL, not in application memory
   - keep publication out of the transaction and rely on persisted outbox rows
5. Verification contract
   - a PostgreSQL provider is incomplete unless it passes the same durable acceptance scenarios already proven for SQLite
   - host smoke coverage must verify that `Postgres` resolves through the existing provider seam once implemented

Recommended implementation posture:

- Prefer `Npgsql` plus explicit SQL and mapping code inside `Mes.Infrastructure` so the PostgreSQL provider mirrors the current SQLite provider style instead of introducing a second persistence abstraction.
- Keep schema bootstrap or migrations provider-local. The host may trigger provider initialization, but schema ownership stays in the provider package.
- Treat `FileStore` as comparison-only once PostgreSQL exists; do not make provider choice cascade back into the application layer.

Open questions to defer until PostgreSQL becomes active work:

- whether migrations should use hand-authored SQL scripts only or a lightweight runner
- what concurrency model is required beyond receipt uniqueness for the pilot line
- whether provider-specific operational telemetry should be added in `Mes.Infrastructure` or at host level

Recommended acceptance coverage:

- reuse the durable parity scenarios already proven for SQLite
- add one provider-selection smoke test in `Mes.ExperienceApi.Tests`
- add one migration or bootstrap test that proves an empty PostgreSQL database can initialize the required executable schema

## 9. Recommended execution order from here

The lowest-risk pilot-hardening sequence is:

1. implement Work Unit 6 order completion progression, because it closes a business-state gap already visible in the current handler
2. codify Work Unit 7 save-boundary language in provider tests and adapter comments, because the current behavior is already close to the desired contract
3. implement Work Unit 8 Experience API error normalization, because it improves pilot operability without changing domain semantics
4. keep Work Unit 9 as a documentation and seam-readiness step until PostgreSQL becomes an actual delivery requirement
