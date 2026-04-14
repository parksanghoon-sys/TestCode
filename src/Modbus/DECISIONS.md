# DECISIONS

## 2026-04-14 ADR-001: MES owns execution truth

Decision:
MES will be treated as the authoritative system for execution state, WIP truth, genealogy, line-side material consumption, and execution-time quality events.

Why:
Without a single owner for execution truth, ERP, WMS, SCADA, and spreadsheet processes will drift and make genealogy and exception recovery unreliable.

Implications:
- ERP remains upstream for order release and business master data.
- MES must persist auditable execution events and support idempotent replay handling.
- WMS and QMS integrations must be designed around MES execution events rather than parallel local logic.

## 2026-04-14 ADR-002: Start with central MES plus site edge

Decision:
Use a central MES application boundary with a separately deployed site-edge gateway for equipment connectivity and offline buffering.

Why:
Brownfield manufacturing sites often need intermittent network tolerance and protocol adaptation close to equipment, but the business workflow should still remain centralized.

Implications:
- Edge buffering and store-and-forward are first-class requirements.
- Device protocol decisions can stay flexible without forcing the core domain to change.
- Observability must cover both central services and site-edge queues.

## 2026-04-14 ADR-003: Prefer modular monolith before microservice split

Decision:
Implement the initial MES core as a modular monolith with asynchronous integration patterns rather than decomposing immediately into many microservices.

Why:
Early MES projects discover and adjust domain boundaries frequently. Premature distribution would increase operational complexity before the domain model stabilizes.

Implications:
- Module boundaries still need to be explicit in code and contracts.
- Reporting, integration, and some quality capabilities can be split later if scale or autonomy proves the need.
- The first implementation should optimize for correctness, traceability, and operability over service count.

## 2026-04-14 ADR-004: Keep the client layer channel-neutral

Decision:
Design the MES client layer so both `WPF` and `Web` can operate on the same MES core through a shared `Experience API / BFF` boundary.

Why:
Shop-floor execution often benefits from WPF because of Windows device integration, kiosk control, and richer offline behavior, while supervisory and administrative use cases benefit from web reach and easier deployment.

Implications:
- Domain behavior, audit rules, and state transitions must stay server-side.
- WPF and Web may differ in presentation and local UX behavior, but they must not diverge in command semantics.
- Operator-critical flows can be WPF-first in release 1 while web expands for monitoring and management workflows.

## 2026-04-14 ADR-005: Business commands always enter through the BFF

Decision:
Use `Experience API / BFF` as the only authoritative entry point for MES business commands across both `WPF` and `Web` clients.

Why:
If WPF and Web use different command paths, state transitions, authorization, idempotency, and audit behavior will drift by channel and become difficult to govern.

Implications:
- `Edge` is limited to device-facing concerns such as protocol translation, buffering, and equipment event ingestion.
- `WPF -> Edge` direct communication may exist for device bridge purposes, but it must not become an alternate business command path.
- Command semantics, audit shape, and permission checks must stay identical regardless of client channel.

## 2026-04-14 ADR-006: Release 1 uses WMS-lite reconciliation and MES in-process quality authority

Decision:
For Release 1, keep warehouse stock authority in `WMS`, keep line-side and in-process material execution truth in `MES`, and let `MES` own in-process quality hold/release authority.

Why:
The pilot needs operationally usable material and quality control before full enterprise integration is mature, but it still cannot afford duplicated authority or ad hoc local rules.

Implications:
- Release 1 must define at least a minimal reconciliation loop between WMS and MES.
- Release 1 hold/release decisions that gate next-operation execution must be authoritative in MES.
- Enterprise quality workflows, CAPA, and laboratory processes may remain in QMS/LIMS and be refined later.

## 2026-04-14 ADR-007: Require Korean XML documentation comments in authored C# code

Decision:
Newly created or modified C# types and methods in this repository must include Korean XML documentation comments.

Why:
The repository now mixes MES domain modeling, WPF/Web-facing work, and reusable .NET guidance. Korean XML comments reduce rediscovery cost and keep intent visible directly in the code for future implementation cycles.

Implications:
- `class`, `record`, `struct`, `interface`, `enum`, `method`, and `constructor` changes should carry XML documentation comments in Korean.
- Test code follows the same documentation rule so executable examples stay self-explanatory.
- The canonical guidance lives in `AGENTS.md` and `rules/dotnet/csharp/xml-doc-comments.md`.
