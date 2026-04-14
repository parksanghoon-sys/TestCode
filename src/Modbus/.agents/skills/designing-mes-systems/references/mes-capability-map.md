# MES Capability Map

Use this reference when deciding what the MES should own in the current scope and what should remain in adjacent systems.

## Core Capability Slices

| Capability slice | MES responsibility | Typical core entities | Scope notes |
|---|---|---|---|
| Production context | Interpret site, area, line, work center, station, and resource structure for execution use | Site, area, line, work center, station, resource | Keep identifiers stable across integrations and future sites |
| Order dispatch | Convert released orders into executable work at the right operation or station | Production order, operation, dispatch list, priority | ERP may release orders, but MES should own dispatch-time execution state |
| Route execution | Guide start, pause, complete, and rework behavior on the shop floor | Route, operation, work instruction, recipe version | Make versioning and override rules explicit |
| WIP visibility | Track current unit, lot, batch, or pallet position and status | WIP unit, container, status, queue | Avoid reporting-only WIP that cannot drive execution decisions |
| Material execution | Validate issue, backflush, consumption, substitution, and return rules | BOM item, material lot, consumption record, substitution approval | MES should own line-side consumption truth when traceability matters |
| Traceability and genealogy | Reconstruct what was built, from what, where, when, and by whom | Serial, lot, genealogy link, equipment, operator, timestamp | Decide early between serial, lot, and hybrid tracking depth |
| Quality execution | Trigger checks, record results, manage nonconformance, and hold or release flow | Inspection plan, result, defect, NCR, hold, deviation | Keep final quality authority clear when QMS or LIMS also exists |
| Resource enforcement | Enforce equipment, tooling, labor certification, and recipe eligibility where needed | Equipment, tool, skill, certification, recipe approval | Only pull in labor or tooling depth that materially changes execution control |
| Performance visibility | Capture downtime, cycle counts, and reason codes for OEE or Andon | Event, downtime reason, cycle, alarm summary | Decide whether MES consumes raw signals or summarized events |

## Manufacturing Mode Differences

### Discrete manufacturing

Prioritize serial or unit genealogy, route enforcement, rework routing, operator guidance, and line-side material validation.

### Batch manufacturing

Prioritize recipe versioning, lot genealogy, weigh and dispense controls, quality holds, and equipment cleaning or changeover constraints.

### Process manufacturing

Prioritize continuous data capture boundaries, material balance, quality correlation, and clear boundaries between MES, historian, and process control.

## Release Scoping Prompts

Use these prompts to decide what belongs in release one:

- Which capability closes the most expensive manual loop today
- Which capability is required for traceability, compliance, or shipment release
- Which capability depends on stable master data that is not ready yet
- Which capability can stay in ERP, WMS, or QMS without breaking execution integrity
- Which capability requires new shop-floor devices, labels, or scanning behavior

## Common Over-Scoping Traps

- Starting with full scheduling optimization when dispatching discipline is still weak
- Pulling preventive maintenance into MES when CMMS ownership is already clear
- Treating dashboards as the first deliverable instead of fixing execution data capture
- Mixing genealogy depth requirements across product families without explicit policy
