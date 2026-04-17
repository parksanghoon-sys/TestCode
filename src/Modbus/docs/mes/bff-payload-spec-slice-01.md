# MES BFF Payload Draft: Slice 01 Operator Execution

## 1. Scope

This draft defines the first task-oriented BFF payloads for the selected pilot slice.

- `start-operation`
- `record-material-consumption`
- `place-hold`
- `release-hold`
- `record-quality-result`
- `complete-operation`

The concrete contract types and endpoint signatures for this draft now live under `src/Mes.Application.Contracts/OperatorExecution/`.

## 2. Shared command envelope

Every command in this slice now uses a compact `Request(Context, Payload)` contract shape in code.

| Field | Type | Required | Notes |
|---|---|---|---|
| `context.identity.command_id` | string | Yes | globally unique command identifier |
| `context.identity.correlation_id` | string | Yes | ties related commands inside one execution conversation |
| `context.identity.idempotency_key` | string | Yes | used to deduplicate retries and offline replay |
| `context.origin.actor_id` | string | Yes | authenticated user or system actor |
| `context.origin.channel` | string | Yes | `wpf`, `web`, `integration`, or `edge` |
| `context.origin.station_id` | string | Conditional | required for station-bound WPF execution commands |
| `context.client_timestamp` | string | Yes | ISO 8601 timestamp from the caller |
| `context.revision_refs` | object | Conditional | item, route, BOM, or spec revision snapshot when relevant |
| `payload` | object | Yes | command-specific body |

The concrete contract code now groups retry identity, actor/channel origin, and client-time metadata into `CommandContextContract`, while the business meaning of each command stays unchanged across WPF and Web.

For Work Unit 3 idempotency, receipt scope uses `channel + command_type + idempotency_key`, while `request_fingerprint` is derived from canonical business fields plus actor and station context. `command_id`, `correlation_id`, `client_timestamp`, and `revision_refs` are transport metadata and should not affect replay equality.

## 3. Command payloads

### 3.1 `start-operation`

**Payload**

| Field | Type | Required | Notes |
|---|---|---|---|
| `production_order_id` | string | Yes | target order |
| `operation_execution_id` | string | Yes | execution instance to start |
| `operation_sequence` | integer | Yes | routed sequence number |
| `quantity_unit` | string | No | defaults to `EA` when omitted |

**Success response**

| Field | Type | Notes |
|---|---|---|
| `accepted` | boolean | `true` when the command is accepted |
| `operation_execution_id` | string | authoritative execution identifier |
| `status` | string | expected `Running` |
| `started_at` | string | server-normalized timestamp |

### 3.2 `record-material-consumption`

**Payload**

| Field | Type | Required | Notes |
|---|---|---|---|
| `operation_execution_id` | string | Yes | active execution context |
| `wip_unit_id` | string | Yes | target WIP unit |
| `material_lot_id` | string | Yes | accepted material lot |
| `material_code` | string | Yes | for operator feedback and reconciliation |
| `quantity` | object | Yes | `{ "value": decimal, "unit": string }` |

**Success response**

| Field | Type | Notes |
|---|---|---|
| `accepted` | boolean | command acceptance result |
| `material_lot_id` | string | authoritative lot identifier |
| `remaining_quantity` | object | lot balance after consumption |
| `genealogy_link_created` | boolean | should be `true` for accepted consumption |

### 3.3 `place-hold`

**Payload**

| Field | Type | Required | Notes |
|---|---|---|---|
| `subject_type` | string | Yes | `operation-execution`, `wip-unit`, or `quality-record` |
| `subject_id` | string | Yes | target record identifier |
| `reason` | string | Yes | hold reason shown to users and audit |

**Success response**

| Field | Type | Notes |
|---|---|---|
| `accepted` | boolean | command acceptance result |
| `subject_type` | string | authoritative subject type |
| `subject_id` | string | authoritative subject id |
| `status` | string | expected `Hold` |

### 3.4 `release-hold`

**Payload**

| Field | Type | Required | Notes |
|---|---|---|---|
| `subject_type` | string | Yes | `operation-execution` or `quality-record` for the first slice |
| `subject_id` | string | Yes | held record identifier |
| `note` | string | Yes | release justification |

**Success response**

| Field | Type | Notes |
|---|---|---|
| `accepted` | boolean | command acceptance result |
| `subject_type` | string | authoritative subject type |
| `subject_id` | string | authoritative subject id |
| `status` | string | restored execution status or `Released` for quality records |

### 3.5 `record-quality-result`

**Payload**

| Field | Type | Required | Notes |
|---|---|---|---|
| `quality_record_id` | string | Yes | target quality record |
| `wip_unit_id` | string | Yes | inspected unit |
| `inspection_code` | string | Yes | inspection or checkpoint identifier |
| `decision` | string | Yes | `passed` or `failed` |
| `note` | string | Yes | inspector note |

**Success response**

| Field | Type | Notes |
|---|---|---|
| `accepted` | boolean | command acceptance result |
| `quality_record_id` | string | authoritative record identifier |
| `status` | string | expected `Passed` for a pass result, or `Hold` when a blocking quality outcome is materialized into the release-1 gate |
| `quality_gate_open` | boolean | `true` only when downstream execution can proceed |

### 3.6 `complete-operation`

**Payload**

| Field | Type | Required | Notes |
|---|---|---|---|
| `operation_execution_id` | string | Yes | execution being completed |
| `good_quantity` | object | Yes | `{ "value": decimal, "unit": string }` |
| `scrap_quantity` | object | No | only required when scrap is being consolidated in the same UI action |
| `completion_mode` | string | No | `manual` or `equipment-assisted` |

**Success response**

| Field | Type | Notes |
|---|---|---|
| `accepted` | boolean | command acceptance result |
| `operation_execution_id` | string | authoritative execution identifier |
| `status` | string | expected `Done` |
| `completed_at` | string | server-normalized timestamp |
| `production_actuals_status` | string | `pending-projection` for the first slice |

## 4. Query and notification payloads

### 4.1 Work queue item

| Field | Type | Notes |
|---|---|---|
| `production_order_id` | string | order identifier |
| `operation_execution_id` | string | current execution record |
| `operation_sequence` | integer | routed sequence |
| `station_id` | string | bound station |
| `status` | string | current execution state |
| `operation_quantity_unit` | string | authoritative completion unit projected from `operation_execution.quantity_unit` |
| `required_materials` | array | material summary projected from the MES-side `operation_material_requirement` snapshot |
| `quality_gate_state` | string | `open` or `hold` for the release-1 operator queue; `review-required` is reserved for a later supervisory slice |

Authoritative field sources for the release-1 operator queue:

- `production_order_id` comes from `operation_execution.production_order_id`.
- `operation_execution_id` comes from `operation_execution.operation_execution_id`.
- `operation_sequence` comes from `operation_execution.operation_sequence`.
- `station_id` comes from `operation_execution.station_id`.
- `status` comes from `operation_execution.status`.
- `operation_quantity_unit` comes from `operation_execution.quantity_unit`.
- `required_materials` comes from `operation_material_requirement`, ordered by `sequence_no`, with the current executable projection anchored at operation attachment.
- `quality_gate_state` is derived in the application query layer from `operation_execution.status` plus any held `quality_record` linked through `wip_unit.current_operation_execution_id`.

### 4.2 Operation state changed notification

| Field | Type | Notes |
|---|---|---|
| `event_type` | string | domain or workflow event name |
| `operation_execution_id` | string | affected execution |
| `production_order_id` | string | parent order |
| `status` | string | updated state |
| `occurred_at` | string | server timestamp |
| `correlation_id` | string | links the update back to the command conversation |

## 5. Validation rules that stay server-side

- `start-operation` must reject commands when the execution is not `Ready` or `Queued`.
- `record-material-consumption` must reject over-consumption, blocked lots, depleted lots, and unit mismatches.
- `record-quality-result` must reject decisions when the quality record is not `Pending` or `InInspection`.
- `complete-operation` must reject completion while an operation hold gate is active.
- `release-hold` and override approvals remain authoritative only after server-side permission and audit checks.
- A blocking quality result in Release 1 must materialize as `QualityRecord` hold plus gated `OperationExecution` hold before the command response is finalized.

## 6. Current slice boundary notes

- `record-material-scan` is still treated as a BFF or application validation workflow before `record-material-consumption`.
- `production-actuals-ready` is still an application or integration projection result after domain completion, not a direct aggregate event.
- A workflow coordinator still needs to keep `QualityRecord` hold decisions and `OperationExecution` hold gates consistent when one should block the other.
- `required_materials` should be read from an MES-side requirement snapshot created from order-release ingestion or operation-attachment projection, not from live upstream master-data calls in the BFF.
- `review-required` remains reserved in the shared contract vocabulary, but the first operator work queue should not emit it.
