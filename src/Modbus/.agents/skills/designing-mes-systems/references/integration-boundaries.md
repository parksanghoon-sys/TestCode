# Integration Boundaries

Use this reference when assigning ownership between MES and adjacent systems.

## Practical System Ownership

| System | Usually owns | Usually should not own |
|---|---|---|
| ERP | Order creation, master business data, costing, inventory valuation, shipment and finance events | Real-time station execution, detailed genealogy, machine handshake |
| APS | Finite scheduling, optimization, medium-term sequencing | Shop-floor start and complete confirmations, operator interaction |
| MES | Dispatch, execution state, WIP truth, genealogy, line-side material control, execution exceptions | Enterprise finance, long-term planning, raw control loops |
| WMS | Warehouse inventory, put-away, picking, replenishment logistics | Station-level production state, in-process genealogy logic |
| QMS | CAPA, complaint, controlled document workflows, enterprise quality process | Real-time line interlocks unless intentionally delegated |
| LIMS | Lab sample workflow and lab results | Production dispatch and line execution decisions outside lab release gates |
| SCADA or HMI | Supervisory visualization, alarms, equipment data aggregation | Business workflow, order decomposition, enterprise traceability policy |
| PLC or DCS | Deterministic control logic and machine sequencing | Work-order state, operator approvals, cross-line business rules |
| Historian | High-frequency time-series storage and replay | Authoritative execution state or workflow decisions |

## Direction of Information Flow

| Boundary | Downstream to MES | Upstream from MES |
|---|---|---|
| ERP <-> MES | Released orders, item master, BOM, routing, work calendar | Production confirmations, consumption, scrap, completion, genealogy summary |
| APS <-> MES | Schedule or sequence intent | Actual progress, constraints, execution feedback |
| WMS <-> MES | Material availability, container identity, replenishment status | Consumption request, issue confirmation, return request |
| QMS or LIMS <-> MES | Inspection definitions, release criteria, lab disposition | Sample event, result capture, NCR trigger, hold or release event |
| SCADA or PLC <-> MES | Equipment state, counters, alarms, process measurements | Recipe selection, work context, setpoint envelope only when architecture allows |

## Integration Patterns

- Prefer event-driven messaging for equipment events, production confirmations, alarms, holds, and quality transitions.
- Prefer API or message-based synchronization for release of executable work and posting of actuals.
- Prefer batch or scheduled sync for slowly changing reference data unless the plant truly needs real-time propagation.
- Make idempotency explicit for order completion, consumption posting, and duplicate equipment events.
- Design store-and-forward for site-edge integrations that cannot assume reliable network connectivity.

## Anti-Patterns

- Letting both MES and ERP decide operation completion independently
- Letting SCADA own traceability rules that operators and enterprise systems also need to see
- Sending raw high-frequency telemetry into MES when a historian or edge aggregator should absorb it first
- Treating WMS bin stock and MES line-side consumption as the same state without reconciliation rules
- Publishing interfaces without versioning master-data contracts and identifiers
