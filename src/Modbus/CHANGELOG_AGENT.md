# CHANGELOG_AGENT

## 2026-04-14

- Added `docs/mes/architecture-blueprint.md` with baseline MES scope, system boundary, logical modules, deployment topology, canonical objects, release plan, and open questions.
- Added `docs/mes/implementation-roadmap.md` with phased delivery guidance, workstreams, initial team shape, early actions, and risks.
- Updated the MES architecture to support both `WPF Station Client` and `Web Portal` over a shared `Experience API / BFF` boundary.
- Tightened the baseline by defining BFF command ownership, an offline authority matrix, a Release 1 WMS-lite reconciliation model, and Release 1 MES quality authority.
- Added `docs/mes/client-channel-matrix.md` to allocate pilot workflows across `WPF-first`, `Web-first`, and `Shared` channels.
- Added `docs/mes/command-event-catalog.md` to define the initial BFF command catalog, domain events, and integration events for Release 1.
- Added `CURRENT_PLAN.md`, `TODOS.md`, and `DECISIONS.md` to support the repository execution loop for the new MES project kickoff.
- Added `Mes.slnx`, `src/Mes.Domain`, and `tests/Mes.Domain.Tests` as the first executable MES domain foundation and validated them with `dotnet test Mes.slnx`.
- Added `rules/dotnet/csharp/xml-doc-comments.md` and updated `AGENTS.md` so new or modified C# classes and functions require Korean XML documentation comments.
- Applied Korean XML documentation comments to the current `Mes.Domain` and `Mes.Domain.Tests` classes and methods.
- Strengthened both `AGENTS.md` and `wpf-dev-pack/AGENTS.md` so the Korean XML documentation requirement is stated explicitly as mandatory for introduced or changed C# classes and functions.

## 2026-04-16

- Selected the first implementation-ready pilot slice as operator execution with material consumption and a quality hold gate.
- Updated `docs/mes/architecture-blueprint.md` and `docs/mes/command-event-catalog.md` to align the documented vocabulary with the current executable domain seed, including `Paused`, `Override Request`, and `quality-result-recorded`.
- Added `docs/mes/pilot-slice-01-operator-execution.md` to define the happy path, hold-gate exception path, and aggregate responsibilities for the first slice.
- Added `docs/mes/logical-data-model-slice-01.md` as the first slice-specific logical data model draft.
- Added `docs/mes/bff-payload-spec-slice-01.md` as the first slice-specific BFF payload draft.
- Added `tests/Mes.Domain.Tests/OperatorExecutionWorkflowTests.cs` to cover one slice happy path and one hold-gate exception path end to end.
- Added `src/Mes.Application.Contracts` and connected it to `Mes.slnx` so the first slice now has concrete transport contracts and endpoint signatures outside the domain layer.
- Added `docs/mes/persistence-schema-slice-01.sql` as the first SQL Server style persistence draft for the selected operator-execution slice.
- Updated the slice-specific logical model and BFF payload docs so they point to the new concrete contract and schema artifacts.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`.
- Updated both `AGENTS.md` entry points so authored methods, constructors, and public APIs should stay at five or fewer input parameters unless a framework-imposed signature requires otherwise.
- Added `docs/mes/pilot-slice-01-application-design.md` to harden the next implementation plan around hold coordination, work-queue read-model sourcing, idempotency, compact request shapes, and delayed handler scaffolding.
- Reordered the current execution plan so Work Unit 1 is now the quality hold gate coordinator instead of broad handler implementation.
- Hardened the slice-01 docs and schema draft so Release 1 blocking quality outcomes materialize into persisted hold provenance, operator queue requirements come from `operation_material_requirement` snapshots, and `command_receipt` now expects canonical request fingerprints with tighter uniqueness.
- Added `src/Mes.Application` and `tests/Mes.Application.Tests` so the first application-layer implementation now exists as a coordinator-centered boundary for Release 1 quality hold gating.
- Updated `src/Mes.Domain/Aggregates/OperationExecution.cs`, `src/Mes.Domain/Aggregates/QualityRecord.cs`, and `src/Mes.Domain/Statuses/DomainStatuses.cs` to persist hold provenance and preserve quality decision outcome separately from hold state.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now including `Mes.Application.Tests`.
- Reviewed Work Unit 2 and fixed the current executable projection anchor as operation attachment, while keeping later order-release ingestion as a reuse path for the same requirement projector.
- Added `src/Mes.Application/OperatorExecution/WorkQueue/` with `OperationMaterialRequirementProjector`, station work-queue query models, and `StationWorkQueueReadService` so `GetStationWorkQueue` can now be composed from MES-side snapshots without live upstream lookups.
- Updated the slice docs so work-queue field ownership and the `quality_record -> wip_unit -> operation_execution` gate derivation path are explicit.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with work-queue projection and query coverage added to `Mes.Application.Tests`.
- Added `src/Mes.Application/Idempotency/` with command receipt scope models, a replay/conflict policy, and a canonical fingerprint builder for the current operator-execution transport contracts.
- Updated the slice docs so `request_fingerprint` is explicitly derived from canonical business fields plus actor and station context, while transport-only metadata is excluded from replay equality.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with idempotency coverage added to `Mes.Application.Tests`.
- Updated `src/Mes.Application.Contracts/Common/BffContracts.cs` so command metadata is grouped into `CommandIdentityContract`, `CommandOriginContract`, and `CommandContextContract`, and compacted operator-execution command contracts to a `Context + Payload` shape.
- Updated `docs/mes/bff-payload-spec-slice-01.md` and `docs/mes/pilot-slice-01-application-design.md` so the documented envelope shape matches the compact request contracts.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with the compact contract shape covered by the existing idempotency tests.
- Added `src/Mes.Application/OperatorExecution/OperatorExecutionCommandHandler.cs`, `OperatorExecutionCommandHandler.Shared.cs`, `StoredCommandResponseSerializer.cs`, and `ProductionActualsPreparationService.cs` so the operator-execution slice now has application-layer command handling with replay-safe receipts and pending production-actuals preparation.
- Added `src/Mes.Application/OperatorExecution/WorkQueue/GetStationWorkQueueQueryHandler.cs` so the existing MES-side work-queue read service now has a transport-facing query composition boundary.
- Added `tests/Mes.Application.Tests/OperatorExecutionCommandHandlerTests.cs` and `GetStationWorkQueueQueryHandlerTests.cs` to validate handler acceptance, safe replay, production-actuals skeleton preparation, and work-queue contract mapping.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with Work Unit 5 coverage raising `Mes.Application.Tests` to 17 passing tests.
- Added `src/Mes.Application/OperatorExecution/OperatorExecutionApplicationPorts.cs`, `OperatorExecutionApplicationRequests.cs`, and `OperatorExecutionApplicationService.cs` so the new handlers and query path can be orchestrated through adapter-facing load/save ports instead of direct repository assumptions.
- Added `tests/Mes.Application.Tests/OperatorExecutionApplicationServiceTests.cs` to validate state loading, replay-aware save skipping, prepared-actuals persistence, and work-queue source loading through the application service boundary.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with orchestration coverage raising `Mes.Application.Tests` to 21 passing tests.
- Added `src/Mes.Infrastructure` as the first concrete operator-execution infrastructure layer, including `InMemoryOperatorExecutionStore`, concrete command/query adapters, and `OperatorExecutionBffEndpointAdapter`.
- Implemented one explicit in-memory write-set boundary that stores `command_receipt`, prepared `production_actuals_batch`, and outbox entries together while preserving the existing application-layer load/save contract.
- Added `tests/Mes.Application.Tests/InMemoryOperatorExecutionAdapterTests.cs` to validate end-to-end accepted command persistence, replay-safe outbox behavior, prepared actuals persistence, and station work-queue projection through the new infrastructure boundary.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with infrastructure adapter coverage raising `Mes.Application.Tests` to 25 passing tests.
