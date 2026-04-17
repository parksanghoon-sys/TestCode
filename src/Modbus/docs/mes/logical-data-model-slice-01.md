# MES Logical Data Model Draft: Slice 01 Operator Execution

## 1. Scope

This draft covers the first implementation-ready data model for the selected operator execution slice:

- order release ingestion
- operation start
- material consumption and genealogy
- quality result and hold gate
- operation completion
- command idempotency and outbox publication support

## 2. Core logical entities

| Entity | Key fields | Purpose |
|---|---|---|
| `production_order` | `production_order_id`, `item_code`, `route_revision`, `status`, `released_at` | authoritative order header for execution |
| `operation_execution` | `operation_execution_id`, `production_order_id`, `operation_sequence`, `quantity_unit`, `status`, `status_before_hold`, `station_id`, `hold_reason`, `hold_source_type`, `hold_source_id`, `started_at`, `completed_at`, `good_quantity`, `scrap_quantity` | authoritative execution unit for one routed step, including release-1 hold provenance |
| `operation_material_requirement` | `operation_material_requirement_id`, `operation_execution_id`, `material_code`, `required_quantity_value`, `required_quantity_unit`, `source_revision_ref`, `sequence_no` | MES-side requirement snapshot that feeds `GetStationWorkQueue` without live upstream lookups |
| `wip_unit` | `wip_unit_id`, `product_code`, `status`, `current_operation_execution_id`, `hold_reason`, `status_before_hold` | current unit under execution and hold gate, including release-1 hold restoration state |
| `material_lot` | `material_lot_id`, `material_code`, `status`, `available_quantity_value`, `available_quantity_unit`, `consumed_quantity_value`, `returned_quantity_value`, `block_reason` | line-side material truth for the slice |
| `material_consumption` | `material_consumption_id`, `material_lot_id`, `wip_unit_id`, `operation_execution_id`, `quantity_value`, `quantity_unit`, `occurred_at` | immutable movement record behind genealogy and posting |
| `genealogy_link` | `genealogy_link_id`, `material_lot_id`, `child_wip_unit_id`, `linked_at` | parent-child traceability link created by accepted consumption |
| `quality_record` | `quality_record_id`, `wip_unit_id`, `inspection_code`, `status`, `decision_status`, `hold_reason`, `decision_note`, `decision_recorded_at` | in-process inspection result, last decision outcome, and quality-side hold state |
| `override_request` | `override_request_id`, `operation_execution_id`, `requested_by`, `reason`, `requested_at`, `status`, `reviewed_by`, `review_note`, `reviewed_at` | approval flow for exceptional continuation paths |
| `command_receipt` | `command_id`, `command_type`, `actor_id`, `channel`, `station_id`, `correlation_id`, `idempotency_key`, `request_fingerprint`, `aggregate_type`, `aggregate_id`, `accepted_at`, `result_code`, `response_json` | BFF idempotency, replay control, and deterministic response lookup |
| `domain_outbox` | `outbox_event_id`, `aggregate_type`, `aggregate_id`, `event_type`, `occurred_at`, `payload_json`, `published_at`, `publish_attempt_count` | publishable event record for integration and projections |
| `production_actuals_batch` | `actuals_batch_id`, `production_order_id`, `operation_execution_id`, `good_quantity`, `scrap_quantity`, `status`, `prepared_at`, `posted_at` | application-level consolidation unit for ERP posting |

## 3. Relationships

| From | To | Cardinality | Notes |
|---|---|---|---|
| `production_order` | `operation_execution` | 1:N | one order can dispatch many operation executions |
| `operation_execution` | `operation_material_requirement` | 1:N | one execution owns one material-requirement snapshot set for queue rendering |
| `operation_execution` | `wip_unit` | 1:N | one execution may process multiple WIP units over time |
| `material_lot` | `material_consumption` | 1:N | each accepted movement becomes an immutable record |
| `wip_unit` | `material_consumption` | 1:N | connects consumed material to the processed unit |
| `material_lot` | `genealogy_link` | 1:N | one lot may produce many genealogy links |
| `wip_unit` | `genealogy_link` | 1:N | one unit may collect many consumed-material links |
| `wip_unit` | `quality_record` | 1:N | later rollout may need multiple inspections per unit |
| `operation_execution` | `override_request` | 1:N | approval history must stay auditable |
| `operation_execution` | `production_actuals_batch` | 1:N | supports retries or partial-posting strategies later |

## 4. State-bearing records

These records must preserve authoritative business state:

- `production_order`
- `operation_execution`
- `operation_material_requirement`
- `wip_unit`
- `material_lot`
- `quality_record`
- `override_request`

These records should be append-only or append-mostly:

- `material_consumption`
- `genealogy_link`
- `command_receipt`
- `domain_outbox`
- `production_actuals_batch`

## 5. Suggested uniqueness and index rules

| Entity | Rule |
|---|---|
| `production_order` | unique on `production_order_id` |
| `operation_execution` | unique on `operation_execution_id`; index on `production_order_id`, `status`, `station_id`; index on `hold_source_type`, `hold_source_id`, `status` |
| `operation_material_requirement` | unique on `operation_material_requirement_id`; alternate uniqueness on `operation_execution_id + sequence_no`; index on `operation_execution_id`, `material_code` |
| `wip_unit` | unique on `wip_unit_id`; index on `current_operation_execution_id`, `status` |
| `material_lot` | unique on `material_lot_id`; index on `material_code`, `status` |
| `material_consumption` | unique on `material_consumption_id`; index on `material_lot_id`, `wip_unit_id`, `operation_execution_id`, `occurred_at` |
| `genealogy_link` | unique on `genealogy_link_id`; alternate uniqueness on `material_lot_id + child_wip_unit_id + linked_at` |
| `quality_record` | unique on `quality_record_id`; index on `wip_unit_id`, `status` |
| `override_request` | unique on `override_request_id`; index on `operation_execution_id`, `status` |
| `command_receipt` | unique on `command_id`; alternate uniqueness on `channel + command_type + idempotency_key`; index on `aggregate_type`, `aggregate_id`, `accepted_at` |
| `domain_outbox` | unique on `outbox_event_id`; index on `published_at`, `event_type` |
| `production_actuals_batch` | unique on `actuals_batch_id`; index on `operation_execution_id`, `status` |

## 6. Modeling notes for the current domain seed

- `MeasuredQuantity` is currently modeled as value plus unit in code, so logical persistence keeps paired value and unit columns instead of introducing a separate unit table for the first slice.
- `GenealogyLink` in the current code seed only represents successful link creation. Reversal or finalization state is deferred until a later traceability expansion.
- `QualityRecord.ReleaseHold` currently results in a released state rather than returning to `InInspection`, so the persistence model should keep a direct released terminal state.
- Release-1 blocking quality outcomes should persist the decision outcome separately from the current `QualityRecord` gate state so a failed inspection remains visible after the record enters `Hold`.
- `OperationExecution` and `QualityRecord` both carry hold-related fields. A separate shared hold table is not required for the first slice as long as one active hold source per `OperationExecution` is enough.
- `WipUnit` also needs `status_before_hold` in executable persistence because the current release-1 restore boundary preserves WIP hold restoration state directly in code.
- `required_materials` for the station queue should come from `operation_material_requirement`, which is projected at order-release ingestion or operation-attachment time rather than from live BFF joins.
- The current executable anchor for that projection is operation attachment. Future order-release ingestion should call the same projector so the queue source stays MES-owned.
- `quality_gate_state` should be derived from `operation_execution.status` plus any held `quality_record` joined through `wip_unit.current_operation_execution_id`.
- `command_receipt.request_fingerprint` should be built from canonical business fields plus actor and station context, while `command_id`, `correlation_id`, `client_timestamp`, and `revision_refs` stay outside the fingerprint so later contract compaction does not change replay semantics.
- `review-required` remains part of the shared vocabulary, but the first operator queue should emit only `open` or `hold`.

## 7. Current executable SQLite coverage

The current slice now runs on `Sqlite` by default through `Mes.ExperienceApi`.
That runtime intentionally implements the executable subset of the provider-neutral
draft rather than forcing every later relational target into slice 01 immediately.

| Record or table | Current SQLite status | Notes |
|---|---|---|
| `production_order` | implemented now | current runtime does not yet use the draft-only `closed_at` lifecycle column |
| `operation_execution` | implemented now | includes release-1 hold provenance and quantity state |
| `operation_material_requirement` | implemented now | authoritative MES-side source for station queue `required_materials` |
| `wip_unit` | implemented now | runtime already persists `status_before_hold`; the provider-neutral draft now needs to keep that field visible |
| `material_lot` | implemented now | current runtime persists executable lot state and genealogy links without a separate movement table yet |
| `genealogy_link` | implemented now | accepted material use currently materializes traceability here |
| `quality_record` | implemented now | runtime does not yet use the draft-only `decision_recorded_at` column |
| `command_receipt` | implemented now | matches the current replay and conflict rules |
| `domain_outbox` | implemented now | runtime also stores `persisted_at` as an adapter-local operational timestamp |
| `production_actuals_batch` | implemented now | runtime does not yet use the draft-only `posted_at` lifecycle column |
| `material_consumption` | deferred for now | accepted material use is still represented through aggregate mutation plus `genealogy_link`; the immutable movement table remains a later relational target |
| `override_request` | deferred for now | supervisory exception approval is not yet an executable slice-01 workflow |
| `store_metadata` | adapter-local | SQLite runtime uses this internal table for outbox sequence bookkeeping; it is not part of the provider-neutral MES draft |

## 8. Open modeling choices after the first physical schema draft

The first persistence draft now exists in `docs/mes/persistence-schema-slice-01.sql`.

- Whether `wip_unit` should stay generic enough for serial, lot, and batch execution in one table or split by production mode later.
- Whether material scan attempts should be persisted separately from accepted `material_consumption` records.
- Whether `production_actuals_batch` should be rebuilt entirely from outbox projections or stored as its own operational record.
- Whether command receipts and outbox events live in the same operational store or in a separate integration-focused boundary.
