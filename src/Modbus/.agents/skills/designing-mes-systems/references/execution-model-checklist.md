# Execution Model Checklist

Use this reference when defining object lifecycles, exception handling, or reviewing whether an MES design can survive real production behavior.

## Canonical Objects

Define and name these objects early when they matter to the use case:

| Object | Minimum design questions |
|---|---|
| Production order | Who releases it, who can split or merge it, and what completion means |
| Operation or step | What prerequisites, resources, and quality gates are required to start or complete it |
| WIP unit, lot, or batch | What identifier is scanned or entered, and how it moves across stations |
| Material lot or serial | How issue, substitution, consumption, and return are recorded |
| Equipment or station | How state, capability, and eligibility are represented |
| Operator or team | What approval, certification, or accountability record is needed |
| Quality record | What creates it, who can disposition it, and what it blocks |
| Genealogy link | What parent-child relationship must be reconstructable later |

## State Modeling Prompts

For each executable object, define:

- the states that matter operationally
- the transition trigger for each state change
- who or what is allowed to perform the transition
- whether the transition must be idempotent
- what audit record must be stored
- what downstream systems must be informed

At minimum, pressure-test these exception states:

- hold
- rework
- scrap
- partial completion
- aborted start
- manual override
- duplicate event replay

## Material Failure Modes

Use these prompts during design review:

| Trigger | Impact | Detection | Mitigation |
|---|---|---|---|
| Equipment sends duplicate completion event | Double posting of production or consumption | Idempotency key mismatch, reconciliation report | Make completion handling idempotent and persist processed event keys |
| Master data version mismatch | Wrong route, recipe, or BOM used at execution time | Validation failure before start, version drift alert | Bind executable work to explicit revision and reject silent fallback |
| Network loss to site edge | Station cannot confirm progress or consume material | Queue depth alarm, offline status heartbeat | Store-and-forward queue with operator-visible offline mode |
| Manual bypass outside MES | Genealogy or quality history becomes incomplete | Audit gap, unmatched counts, missing scan history | Require supervised override flow and periodic reconciliation |
| WMS and MES disagree on line-side material | Consumption or shortage signals become unreliable | Inventory variance, pick failure, backflush mismatch | Define reconciliation ownership and event timing clearly |

## Review Questions

- Can the proposed design explain where every tracked unit is and why
- Can it reconstruct parent-child genealogy for the required retention period
- Can it separate planned quantity, good quantity, scrap, and rework clearly
- Can it survive out-of-order, late, or missing equipment messages
- Can operators recover from exceptions without bypassing the authoritative workflow
- Can another system consume MES actuals without reverse-engineering screen behavior
