# Pilot Profile Working Assumptions

## 1. Purpose

This document does not claim that the plant-side pilot profile is already confirmed.

Its purpose is to make the current executable assumption set explicit so the codebase,
slice docs, WPF station planning, and future stakeholder validation all refer to the
same working profile instead of carrying different implicit guesses.

## 2. Current Confirmed Facts In Repo

The following points are already confirmed by the current repository design and code:

- The current executable slice is `operator execution -> material consumption -> quality hold/release -> operation completion`.
- The current operator workflow is station-based and queue-driven.
- `WPF` is the preferred operator channel, while `Web` remains the preferred supervisory and review channel.
- All business commands are expected to enter through `Experience API / BFF`, not through direct device or local database paths.
- `MES` is the authority for release-1 execution truth, line-side material execution truth, and in-process quality hold/release.
- `MaterialLot` plus `WipUnit` currently materialize accepted genealogy through `genealogy_link`.
- `WipUnit` already uses a generic identity shape that can later represent serial, lot, or batch execution identities.
- The current backend runtime is SQLite, with a future PostgreSQL replacement planned through the same host seam.

## 3. Working Pilot Assumptions

The following points are still assumptions, not externally validated facts.

### 3.1 Manufacturing Mode

Current working assumption:

- The first pilot line should be treated as a `discrete-dominant hybrid` line.
- The execution rhythm is station-based rather than continuous-process control-room based.
- Operator actions such as start, scan, hold, and complete are expected to be meaningful at the station level.

Why this fits the current implementation:

- The current slice centers on `OperationExecution`, `WipUnit`, station work queue, and operator-issued task commands.
- The current WPF/Web channel policy assumes scan-heavy station work and supervisory web review rather than process-console orchestration.
- No current slice behavior depends on process-industry batch phase logic, recipe campaign sequencing, or historian-first control loops.

### 3.2 Genealogy Depth

Current working assumption:

- Release 1 should use `lot-first genealogy with serial-capable WIP identity`, not mandatory serial-per-piece traceability for every pilot product.
- Accepted material use should create authoritative `lot -> WIP` genealogy links.
- The current pilot should allow one `WipUnit` to stand for a serial, lot, or batch execution identity depending on the selected product family.

Why this fits the current implementation:

- The current executable model already persists `genealogy_link` and `MaterialLot` state without requiring a separate serial-only execution model.
- The slice docs already mark `material_consumption` as deferred while keeping `genealogy_link` executable now.
- The domain model can grow into deeper container, pallet, or serial genealogy later without invalidating the current station-based flow.

What this assumption intentionally does not claim:

- It does not claim that per-piece serial genealogy is unneeded for the real pilot line.
- It does not claim that pallet or container genealogy will never be required.
- It only claims that the current executable slice most naturally fits a lot-first pilot unless the selected product family proves otherwise.

### 3.3 Station Device Profile

Current working assumption:

- The initial pilot station is more likely to be `scanner/printer/local-device first` than `mandatory PLC handshake first`.
- Direct Modbus or PLC handshake support should be treated as optional pilot extension work, not as the baseline prerequisite for the current operator-execution slice.

Why this fits the current implementation:

- The existing code already proves the business command path without any equipment-specific handshake contract.
- The WPF station planning now treats scanner, printer, local bridge, and offline queue UX as the immediate priorities.
- The architecture already restricts `WPF -> Edge` direct paths to device-facing work only, not business authority.

What would change this assumption:

- A pilot station that cannot legally or operationally complete work without machine ack.
- A station whose operator UX depends on live equipment permissives, completion interlocks, or equipment-generated step evidence.
- A pilot line whose real device estate is actually Modbus-centric and not scanner/printer centric.

## 4. Recommended Development Interpretation

Until a real pilot line disproves these assumptions, development should proceed as if:

- the first pilot is station-based, discrete-dominant, and operator-driven;
- `lot -> WIP` genealogy is the minimum authoritative traceability depth for Release 1;
- `WipUnit` stays generic enough to map later to serial, lot, or batch execution identity;
- WPF station work should focus on queue, scan, complete, hold, and peripheral UX before generalized device control;
- Modbus/edge work should remain a bounded follow-up that starts only after the pilot equipment inventory is known.

## 5. What Would Materially Change The Architecture

Any of the following findings would materially widen or redirect the current design:

- The selected pilot product family requires mandatory serial-per-piece genealogy at Release 1.
- The pilot line is actually batch-process or process-dominant rather than station-discrete.
- A station cannot progress without authoritative machine handshake or permissive checks.
- Recipe download, equipment eligibility, or analog telemetry becomes a pilot-day-one dependency.
- Regulated e-signature or enterprise quality release becomes mandatory for in-process hold release.

If any of those become true, the next step should be to update the slice docs first and only then widen code, WPF UX, or edge integration.

## 6. External Validation Checklist

The following questions still require plant-side confirmation:

1. Which real pilot line and representative product family are in scope?
2. Does that line behave as discrete, batch, or hybrid in the operator-facing execution loop?
3. Is Release 1 genealogy sufficient at `lot -> WIP`, or is serial-per-piece traceability mandatory from day one?
4. Does any station require direct machine ack, completion permissive, or PLC handshake before operator completion can be accepted?
5. Which devices are truly required on the station: scanner, printer, scale, PLC bridge, labeler, test rig, or mixed?

## 7. Related Documents

- Architecture baseline: `docs/mes/architecture-blueprint.md`
- Rollout sequence: `docs/mes/implementation-roadmap.md`
- Channel policy: `docs/mes/client-channel-matrix.md`
- Current station and device sub-plan: `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md`
- Current execution slice design: `docs/mes/pilot-slice-01-operator-execution.md`
- Current application hardening design: `docs/mes/pilot-slice-01-application-design.md`
