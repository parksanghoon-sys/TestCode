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
- Added `src/Mes.ExperienceApi` as the first thin shared `Experience API / BFF` host for the operator-execution slice, using minimal API route mapping over the existing endpoint adapter and in-memory reference services.
- Added `tests/Mes.ExperienceApi.Tests` to verify host DI resolution and that the documented operator-execution endpoint signatures are exposed as concrete routes.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with `Mes.ExperienceApi.Tests` adding 2 passing host smoke tests.
- Added project-level `README.md` files under every current `src/*` and `tests/*` project so each project now documents its role, structure, key classes, dependency direction, and current limitations locally.
- Updated both `AGENTS.md` entry points so future project creation must include a local `README.md`, and material project-structure changes must keep that README in sync.
- Added explicit `Restore(...)` boundaries in `Mes.Domain` for `ProductionOrder`, `OperationExecution`, `MaterialLot`, `QualityRecord`, and `WipUnit` so detached persistence snapshots can reconstruct the current slice without replaying business commands.
- Added `src/Mes.Infrastructure/OperatorExecution/FileStore/` with a JSON snapshot-backed durable store, durable command/query adapters, reload-safe outbox persistence, and file-backed adapter tests in `Mes.Application.Tests`.
- Updated `Mes.ExperienceApi` so the default host composition now uses the file-backed durable adapter, while keeping the in-memory adapter available as a reference path.
- Refreshed the affected project-level `README.md` files so `Mes.Domain`, `Mes.Infrastructure`, `Mes.ExperienceApi`, `Mes.Application.Tests`, and `Mes.ExperienceApi.Tests` all describe the new durable adapter and restore boundaries.
- Revalidated the repository with `dotnet build Mes.slnx -v minimal` and `dotnet test Mes.slnx -v minimal`, now with durable file-store coverage added to `Mes.Application.Tests`.

## 2026-04-17

- Reconciled the project memory files with the actual worktree and confirmed that a first SQLite relational adapter candidate already exists under `src/Mes.Infrastructure/OperatorExecution/Sqlite/`.
- Narrowed the next execution point from "start a relational adapter" to "promote the existing SQLite path through parity tests, host composition, and explicit SQL-draft gap tracking."
- Revalidated the current worktree with `dotnet test Mes.slnx -v minimal` before updating the plan, so the revised next-step design is anchored to a passing baseline.
- Added `tests/Mes.Application.Tests/SqliteOperatorExecutionAdapterTests.cs` to prove that the SQLite relational adapter candidate preserves receipt replay, outbox durability, prepared actuals persistence, and station work-queue projection across store reopen.
- Updated `src/Mes.ExperienceApi/OperatorExecution/OperatorExecutionServiceCollectionExtensions.cs` so durable provider selection is now host-configurable, defaults to `Sqlite`, still supports `FileStore`, and reserves `Postgres` plus `Mes:OperatorExecutionConnectionString` for a future relational adapter.
- Updated `tests/Mes.ExperienceApi.Tests/OperatorExecutionExperienceApiEndpointRouteBuilderTests.cs` so host smoke coverage now verifies the default SQLite runtime, explicit `FileStore` selection, and the reserved-provider guard for `Postgres`.
- Refreshed the local `README.md` files under `src/Mes.Infrastructure`, `src/Mes.ExperienceApi`, `tests/Mes.Application.Tests`, and `tests/Mes.ExperienceApi.Tests` so the provider-selectable durable path is documented next to the code.
- Updated `docs/mes/persistence-schema-slice-01.sql`, `docs/mes/logical-data-model-slice-01.md`, and `docs/mes/pilot-slice-01-application-design.md` so SQLite is explicitly documented as the current executable runtime, `material_consumption` plus `override_request` are marked as deferred relational targets, and `wip_unit.status_before_hold` is aligned with executable persistence.
- Revalidated the repository with `dotnet test tests/Mes.Application.Tests/Mes.Application.Tests.csproj -v minimal --filter "FullyQualifiedName~SqliteOperatorExecutionAdapterTests"`, `dotnet test tests/Mes.ExperienceApi.Tests/Mes.ExperienceApi.Tests.csproj -v minimal`, and `dotnet test Mes.slnx -v minimal`.
- Refined the durable-provider stance so SQLite stays the only active runtime, `FileStore` is documented as comparison-only, and host smoke coverage now proves that legacy file-store path settings do not silently displace the SQLite default.
- Extended `docs/mes/pilot-slice-01-application-design.md` with Work Units 6 through 9 so the next pilot-hardening sequence is now explicitly designed: order completion progression, canonical save boundary, Experience API problem-details mapping, and the first PostgreSQL provider handoff contract.
- Implemented Work Unit 6 by extending `CompleteOperationCommandState` with an authoritative order-completion progress summary, applying `ProductionOrder` progression inside `OperatorExecutionCommandHandler`, and teaching the in-memory, file-backed, and SQLite command ports to load that summary from MES-owned execution state.
- Revalidated the new progression rule with `dotnet test tests/Mes.Application.Tests/Mes.Application.Tests.csproj -v minimal` and `dotnet test Mes.slnx -v minimal`, now including handler, application-service, and durable reload coverage for `ProductionOrderStatus.PartiallyCompleted` and `ProductionOrderStatus.Completed`.
- Implemented Work Unit 7 by extending the `InMemory`, `FileStore`, and `Sqlite` adapter tests so accepted `complete-operation` now locks the full pilot save boundary: touched aggregate state, `command_receipt`, prepared `production_actuals_batch`, and the full outbox set stay aligned and replay-safe across providers.
- Updated `src/Mes.Infrastructure/README.md` and `tests/Mes.Application.Tests/README.md` so the canonical accepted-command write-set and replay-safe provider parity are documented next to the executable adapter code.
- Revalidated Work Unit 7 with `dotnet test tests/Mes.Application.Tests/Mes.Application.Tests.csproj -v minimal` and `dotnet test Mes.slnx -v minimal`.
- Implemented Work Unit 8 by adding a typed operator-execution exception taxonomy in `Mes.Application`, translating deterministic store misses and semantic conflicts into that taxonomy, and normalizing host-visible failures in `Mes.ExperienceApi` through one problem-details mapping seam.
- Added `tests/Mes.ExperienceApi.Tests/OperatorExecutionProblemDetailsTests.cs` to lock `400`, `404`, `409`, `422`, and fallback `500` responses, while keeping route handlers thin over the existing endpoint adapter.
- Refreshed the local `README.md` files under `src/Mes.Application`, `src/Mes.ExperienceApi`, and `tests/Mes.ExperienceApi.Tests` so the new host-facing exception taxonomy and problem-details coverage are documented next to the code.
- Revalidated Work Unit 8 with `dotnet test tests/Mes.Application.Tests/Mes.Application.Tests.csproj -v minimal`, `dotnet test tests/Mes.ExperienceApi.Tests/Mes.ExperienceApi.Tests.csproj -v minimal`, and `dotnet test Mes.slnx -v minimal`.
