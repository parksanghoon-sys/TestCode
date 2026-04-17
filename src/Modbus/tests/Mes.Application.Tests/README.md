# Mes.Application.Tests

## Purpose

`Mes.Application.Tests` verifies the operator-execution slice above the domain level:
application policy, handlers, orchestration, and provider-facing adapter behavior.

## Covered Scope

The project currently verifies:

- quality hold coordination
- station work-queue sourcing and query composition
- command receipt idempotency
- replay-safe command handling
- application service load-handle-save orchestration
- the canonical accepted-command save boundary across provider adapters
- `InMemory` adapter parity
- `FileStore` durable parity
- `Sqlite` durable parity

The project does not cover:

- HTTP host behavior
- end-to-end transport error mapping
- UI behavior

## File Structure

```text
CommandReceiptIdempotencyPolicyTests.cs
FileOperatorExecutionAdapterTests.cs
GetStationWorkQueueQueryHandlerTests.cs
InMemoryOperatorExecutionAdapterTests.cs
OperatorExecutionApplicationServiceTests.cs
OperatorExecutionCommandHandlerTests.cs
QualityHoldGateCoordinatorTests.cs
SqliteOperatorExecutionAdapterTests.cs
StationWorkQueueReadServiceTests.cs
```

## Key Test Files

### `OperatorExecutionCommandHandlerTests.cs`

Verifies acceptance, replay behavior, and prepared actuals generation.

### `OperatorExecutionApplicationServiceTests.cs`

Locks the adapter-facing orchestration contract:
load -> handle -> save.

### `FileOperatorExecutionAdapterTests.cs`

Proves the snapshot-based durable path across store reload, including the current
canonical accepted-command write-set.

### `SqliteOperatorExecutionAdapterTests.cs`

Proves the relational durable path across store reopen for the same acceptance
scenarios used by `FileStore`, including replay-safe save-boundary parity.

## Current Limitations

- The relational tests prove the executable slice write-set, not every table from the
  SQL draft.
- HTTP-host composition and route smoke tests live in `Mes.ExperienceApi.Tests`.
