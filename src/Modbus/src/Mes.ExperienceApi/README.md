# Mes.ExperienceApi

## Purpose

`Mes.ExperienceApi` is the thin shared BFF host for the operator-execution slice.
It exposes the documented HTTP routes without moving workflow policy out of
`Mes.Application`.

## Responsibility Boundary

This project owns:

- minimal API startup
- DI composition
- mapping documented endpoint signatures to concrete routes
- selecting which durable infrastructure provider the host uses
- stable host-level problem-details mapping for deterministic operator-execution failures

This project does not own:

- domain rules
- application orchestration details
- persistence implementation internals

## Folder Structure

```text
OperatorExecution/
  OperatorExecutionEndpointRouteBuilderExtensions.cs
  OperatorExecutionProblemDetailsExtensions.cs
  OperatorExecutionServiceCollectionExtensions.cs
Properties/
  launchSettings.json
Program.cs
```

## Durable Provider Selection

`AddOperatorExecutionDurableServices(...)` is now provider-selectable.

Current provider values:

- `Sqlite`: default durable runtime
- `FileStore`: comparison-only durable path for parity, inspection, or recovery drills
- `Postgres`: reserved for a future relational adapter

Current configuration keys:

- `Mes:OperatorExecutionDurableProvider`
- `Mes:OperatorExecutionSqliteDatabasePath`
- `Mes:OperatorExecutionFileStorePath`
- `Mes:OperatorExecutionConnectionString`

Notes:

- If no provider is configured, the host defaults to `Sqlite`.
- Existing file-path settings alone do not change that default; the host stays on `Sqlite`
  unless `Mes:OperatorExecutionDurableProvider=FileStore` is set explicitly.
- `Mes:OperatorExecutionConnectionString` is intentionally reserved so a future
  PostgreSQL provider can plug into the same host composition seam.

## Request Flow

1. The incoming HTTP request matches one documented endpoint signature.
2. Minimal API binds the request contract.
3. The route handler calls `OperatorExecutionBffEndpointAdapter`.
4. The adapter delegates to `OperatorExecutionApplicationService`.
5. The application layer uses whichever durable provider the host selected.
6. Host-level exception mapping normalizes deterministic failures into stable problem details.

## Key Files

### `Program.cs`

Creates the web application and wires the default durable provider through
`AddOperatorExecutionDurableServices(...)`.

### `OperatorExecution/OperatorExecutionServiceCollectionExtensions.cs`

Centralizes provider-aware DI composition. This is the host seam that allows the
team to run SQLite now and add PostgreSQL later without changing the application
boundary.

### `OperatorExecution/OperatorExecutionProblemDetailsExtensions.cs`

Owns the stable `400`, `404`, `409`, `422`, and fallback `500` problem-details
mapping so route handlers stay thin and the application layer does not learn
HTTP semantics.

### `OperatorExecution/OperatorExecutionEndpointRouteBuilderExtensions.cs`

Maps the documented operator-execution endpoint signatures to concrete minimal API
routes while keeping handlers thin.

## Dependencies

This project references:

- `Mes.Application.Contracts`
- `Mes.Application`
- `Mes.Infrastructure`

This project is referenced by:

- `Mes.ExperienceApi.Tests`

## Current Limitations

- Authentication and authorization are still pending.
- The current error contract is scoped to the operator-execution slice and the
  current deterministic failure set; broader cross-slice standardization can
  widen it later through the same host seam.
- The default provider is now `Sqlite`; file-based durable composition remains only
  as an explicit comparison and recovery-inspection path.
- PostgreSQL is not implemented yet; only the configuration seam is reserved.
