---
name: designing-mes-systems
description: Design Manufacturing Execution System (MES) architectures, capability maps, execution flows, and plant-system integration boundaries. Use when Codex needs to scope MES responsibilities, model site or line structures, define work-order/WIP/traceability/quality behavior, assign ownership across ERP/APS/WMS/QMS/LIMS/SCADA/PLC, or review phased MES rollout options.
---

# Design MES Systems

## Overview

Use this skill to turn a manufacturing problem statement into an MES design that is bounded, implementable, and rollout-aware.
Prefer explicit ownership, traceability, and exception handling over vague platform diagrams.

## Working Defaults

- Assume a brownfield plant unless the user clearly says greenfield.
- Assume one site first, but keep names, identifiers, and integration contracts reusable across sites.
- Place MES between business planning systems and real-time control systems.
- Prefer one clear owner for each business rule, state transition, and master-data entity.
- Prefer modular deployment over microservice sprawl unless scale, autonomy, or regulatory isolation clearly require decomposition.

## Design Workflow

### 1. Frame the manufacturing context

Capture the production mode first: discrete, batch, process, or hybrid. Identify site topology, routing rigidity, takt or batch cadence, genealogy depth, regulatory constraints, offline tolerance, and operator language or role variation.

If the request is underspecified, make an explicit working assumption and continue. Only stop to ask when the answer changes architecture or compliance posture in a material way.

### 2. Draw the MES system boundary

State what MES owns and what it does not own before proposing modules. Be specific about the boundary with ERP, APS, WMS, QMS, LIMS, SCADA, PLC, historian, and maintenance systems.

Use `references/integration-boundaries.md` when defining ownership or integration direction.

### 3. Build the capability map

Cover the minimum capability slices needed for execution:

- production order decomposition and dispatch
- operation or route execution
- WIP visibility and status
- material consumption and genealogy
- quality sampling, inspection, nonconformance, and hold or release
- equipment, tooling, and resource eligibility where MES must enforce it
- downtime, reasons, and performance visibility if the user expects OEE or Andon behavior

Use `references/mes-capability-map.md` to decide which capabilities belong in the first release and which can remain adjacent-system responsibilities.

### 4. Model canonical objects, states, and events

Define the canonical identifiers and lifecycle states for order, operation, resource, equipment, material lot or serial, genealogy unit, and quality record. Make state transitions explicit, especially for hold, rework, scrap, partial completion, and override flows.

Use `references/execution-model-checklist.md` when the request involves traceability, exception paths, or execution-state reviews.

### 5. Design the runtime architecture

Choose the simplest architecture that fits the plant reality. Call out:

- site edge versus central hosting
- store-and-forward needs for intermittent connectivity
- event-driven versus request-response integration
- transaction boundaries and idempotency points
- audit trail, e-signature, or record-retention requirements when relevant

Prefer reusable modules or bounded services around stable responsibilities rather than around every screen or table.

### 6. Plan rollout and coexistence

Design for coexistence with current tools, spreadsheets, manual stations, or legacy MES functions. Phase delivery by operational value and adoption risk, not by abstract technical layers alone.

Include data migration, cutover, training, and fallback paths for any design that changes shop-floor execution.

## Output Contract

When asked to design, review, or compare MES options, produce this structure unless the user requests another format:

1. Context and explicit assumptions
2. System boundary and ownership matrix
3. Capability map by release or scope slice
4. Canonical objects, states, and key event flows
5. Integration architecture and data ownership
6. Non-functional requirements and material failure modes
7. Rollout plan, open questions, and deferred decisions

## Review Rules

- Flag any design that duplicates routing, scheduling, or master-data authority across systems without a clear owner.
- Flag any design that tracks WIP but cannot reconstruct genealogy, hold history, or operator overrides where those are required.
- Flag any design that assumes always-online equipment connectivity in a plant that likely needs buffering or manual fallback.
- Flag any design that decomposes too early into many services without proving deployment or autonomy value.
- Call out unresolved decisions separately when they can affect compliance, integration cost, or production continuity later.

## Reference Routing

- Read `references/mes-capability-map.md` for a practical MES capability breakdown and release-scoping prompts.
- Read `references/integration-boundaries.md` for ownership boundaries, interface direction, and common anti-patterns.
- Read `references/execution-model-checklist.md` for object models, execution states, and realistic failure-mode prompts.
