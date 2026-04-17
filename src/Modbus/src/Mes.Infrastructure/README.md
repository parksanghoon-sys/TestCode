# Mes.Infrastructure

## Purpose

`Mes.Infrastructure` implements the concrete technology-facing adapters behind the
`Mes.Application` ports for the current operator-execution slice.

The project currently keeps three adapter paths side by side:

- `InMemory/` for fast boundary verification and focused tests
- `FileStore/` for a simple durable snapshot bridge
- `Sqlite/` for the first relational durable adapter

## Responsibility Boundary

This project owns:

- concrete implementations of `IOperatorExecutionCommandPort`
- concrete implementations of `IStationWorkQueueSourcePort`
- persistence mapping and aggregate/entity reconstruction
- durable capture of `command_receipt`, `production_actuals_batch`, and domain outbox rows
- the thin `OperatorExecutionBffEndpointAdapter`

## Canonical Save Boundary

For an accepted operator-execution command, the logical pilot write-set is:

- touched aggregate or entity snapshots for `ProductionOrder`, `OperationExecution`,
  `WipUnit`, `MaterialLot`, and `QualityRecord`
- `command_receipt`
- `production_actuals_batch` for accepted `complete-operation`
- `domain_outbox`

Provider-specific metadata or helper rows may exist, but they must not widen this
application-visible contract. Replay of an existing receipt should therefore not
open a new logical save boundary or create additional persisted side effects.

This project does not own:

- domain rules
- HTTP route mapping
- client-facing contracts

## Folder Structure

```text
OperatorExecution/
  OperatorExecutionBffEndpointAdapter.cs
  FileStore/
    FileOperatorExecutionStoreOptions.cs
    FileOperatorExecutionPersistenceModels.cs
    FileOperatorExecutionStore.cs
    FileOperatorExecutionCommandAdapter.cs
    FileStationWorkQueueSourceAdapter.cs
  InMemory/
    InMemoryOperatorExecutionStore.cs
    InMemoryOperatorExecutionCommandAdapter.cs
    InMemoryStationWorkQueueSourceAdapter.cs
  Sqlite/
    SqliteOperatorExecutionStoreOptions.cs
    SqliteOperatorExecutionPersistenceModels.cs
    SqliteOperatorExecutionStore.cs
    SqliteOperatorExecutionCommandAdapter.cs
    SqliteStationWorkQueueSourceAdapter.cs
```

## Current Durable Strategy

- The application-layer save boundary stays unchanged across providers.
- `Sqlite/` is the current default durable runtime selected by `Mes.ExperienceApi`.
- `FileStore/` remains available only as a comparison-oriented durable path while
  SQLite is the active runtime.
- Future relational providers such as PostgreSQL should plug in at the same port boundary instead of changing `Mes.Application`.

## Key Files

### `OperatorExecution/OperatorExecutionBffEndpointAdapter.cs`

The host-facing facade that keeps route handlers thin and delegates orchestration to
`OperatorExecutionApplicationService`.

### `OperatorExecution/Sqlite/SqliteOperatorExecutionStore.cs`

The current relational durable store. It owns SQLite schema creation, state loading,
transactional commit of the current write-set, and inspection helpers used by tests.

### `OperatorExecution/FileStore/FileOperatorExecutionStore.cs`

The snapshot-based durable bridge used to prove detached restore behavior and to keep
a low-friction comparison and recovery-inspection path while relational persistence evolves.

## Dependencies

This project references:

- `Mes.Domain`
- `Mes.Application`
- `Mes.Application.Contracts`

This project is referenced by:

- `Mes.ExperienceApi`
- `Mes.Application.Tests`
- `Mes.ExperienceApi.Tests`

## Validation Flow

- Adapter-level acceptance is covered in `Mes.Application.Tests`.
- `InMemory`, `FileStore`, and `Sqlite` should continue to prove the same
  application-facing behavior until the team intentionally retires a provider.

## Current Limitations

- `Sqlite/` currently persists the executable slice write-set, not every table from
  `docs/mes/persistence-schema-slice-01.sql`.
- `material_consumption` and `override_request` are still documented relational
  targets, but they are not yet part of the executable slice persistence path.
- A future PostgreSQL adapter does not exist yet; the host only keeps a reserved
  configuration slot for that provider.
