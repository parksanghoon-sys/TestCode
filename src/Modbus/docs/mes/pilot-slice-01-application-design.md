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
