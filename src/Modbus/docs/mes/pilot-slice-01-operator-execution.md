# Pilot Slice 01: Operator Execution With Material Consumption And Quality Gate

## 1. Scope

The first implementation-ready pilot slice is the operator-critical execution flow below.

1. ERP release is ingested into MES as an executable production order.
2. A station starts one operation execution for one production order.
3. The operator consumes one validated material lot for one WIP unit.
4. Quality records either a pass result or places a hold gate that blocks completion.
5. The operation completes and the application layer prepares production actuals for ERP posting.

## 2. Working assumptions

- The pilot line is treated as a discrete or hybrid line with station-based execution.
- One operation execution is the authoritative execution unit for the selected slice.
- One WIP unit is enough to define the first command and data contracts even if later rollout needs batch-oriented execution.
- Material scan validation stays in the BFF or application layer before domain consumption is committed.
- Production actuals consolidation stays in the application or integration layer after authoritative domain completion is recorded.

## 3. Why this slice comes first

- It uses the aggregates that already exist in `Mes.Domain`.
- It exercises the minimum cross-cutting MES responsibilities: order, execution, WIP, material, quality, hold, and completion.
- It is the most operator-critical flow, so it is the best anchor for the first WPF-first command contract.
- It creates a stable base for the first logical data model and BFF payload definitions without forcing broader warehouse or enterprise-quality decisions too early.

## 4. Happy path

| Step | Command or action | Authoritative object | Expected state or event |
|---|---|---|---|
| 1 | `ingest-order-release` | `ProductionOrder` | `Released`, `order-released-ingested` |
| 2 | attach executable operation | `ProductionOrder` | `Dispatched`, then `InProgress` when work begins |
| 3 | `start-operation` | `OperationExecution` | `Running`, `operation-started` |
| 4 | `record-material-consumption` | `MaterialLot` | available quantity reduced, `material-consumption-recorded`, `genealogy-link-created` |
| 5 | `record-quality-result` with pass | `QualityRecord` | `Passed`, `quality-result-recorded` |
| 6 | `complete-operation` | `OperationExecution` | `Done`, `operation-completed` |
| 7 | application consolidation | integration workflow | `production-actuals-ready`, later ERP posting |

## 5. Exception path: quality hold gate

| Step | Command or action | Authoritative object | Expected state or event |
|---|---|---|---|
| 1 | blocking `record-quality-result` or explicit `place-hold` | `QualityRecord` and gated `OperationExecution` | blocking quality outcome is persisted, then both records enter `Hold`, `hold-placed` |
| 2 | `complete-operation` attempted while hold is active | `OperationExecution` | rejected by domain guard |
| 3 | `release-hold` after review | `QualityRecord` and gated `OperationExecution` | `Released` or prior execution state restored, `hold-released` |
| 4 | `complete-operation` retry | `OperationExecution` | `Done`, `operation-completed` |

## 6. Terminology alignment for the current executable seed

| Concept | Canonical name for this slice | Current executable representation | Note |
|---|---|---|---|
| Order working state | `InProgress` | `ProductionOrderStatus.InProgress` | use code spelling as canonical from here |
| Order partial completion state | `PartiallyCompleted` | `ProductionOrderStatus.PartiallyCompleted` | keep exact enum spelling in payload and architecture docs |
| Operation temporary stop | `Paused` | `OperationExecutionStatus.Paused` | add to docs wherever operation states are listed |
| Operation terminal completion state | `Done` | `OperationExecutionStatus.Done` | distinguish operation execution completion from order `Completed` |
| WIP active state | `InProcess` | `WipUnitStatus.InProcess` | prefer exact enum spelling for payload and persistence drafts |
| Quality active inspection state | `InInspection` | `QualityRecordStatus.InInspection` | prefer exact enum spelling instead of prose `In Inspection` |
| Quality decision event | `quality-result-recorded` | `QualityResultRecordedDomainEvent` | domain event exists and should be listed in docs |
| Exception approval | `override-requested/approved/rejected` | `OverrideRequest` aggregate and related events | treat `OverrideRequest` as canonical object name |
| Material usage command | `record-material-consumption` | shared BFF command name | do not shorten to `record-consumption` in client or architecture docs |
| Material scan validation | application workflow event | not a `Mes.Domain` event yet | keep in BFF or application layer for the first slice |
| Production actuals preparation | integration workflow event | not a `Mes.Domain` event yet | derive after command handling and outbox projection |

## 7. Aggregate responsibilities in the slice

| Aggregate or entity | Responsibility inside this slice |
|---|---|
| `ProductionOrder` | owns released and in-progress order lifecycle and executable operation attachment |
| `OperationExecution` | owns station-bound execution state, pause or resume, hold gate, scrap, and completion quantities |
| `WipUnit` | represents the currently processed unit and its execution hold or completion state |
| `MaterialLot` | owns available, consumed, returned, and genealogy-link state |
| `QualityRecord` | owns inspection progress, pass or fail decision, and quality-side hold or release state |
| `OverrideRequest` | handles gated exception approval outside the main happy path |

## 8. Application-layer gaps that remain after this slice definition

The detailed next-step hardening plan for these gaps now lives in `docs/mes/pilot-slice-01-application-design.md`.

- A workflow coordinator still needs to orchestrate cross-aggregate reactions such as applying an operation hold when quality blocks completion.
- BFF request idempotency, command receipts, and notification fan-out still need concrete implementation.
- `material-scanned` and validation feedback still need application-level modeling before the WPF station workflow is complete.
- `production-actuals-ready` still needs an application or integration projection that consolidates completion, scrap, and material movement into an ERP posting unit.
- Release 1 now also needs persisted hold provenance plus pre-hold execution state so quality-driven hold and release remains correct after reload or retry.
