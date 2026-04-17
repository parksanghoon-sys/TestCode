# Mes.ExperienceApi.Tests

## Purpose

`Mes.ExperienceApi.Tests` provides smoke coverage for the thin host layer in
`Mes.ExperienceApi`.

## Covered Scope

The project currently verifies:

- DI resolution of `OperatorExecutionBffEndpointAdapter`
- durable provider selection in the host
- the default `Sqlite` durable runtime
- explicit comparison-path selection of `FileStore`
- that legacy file-store path settings do not override the SQLite default
- route exposure for all documented endpoint signatures
- stable host-level problem-details mapping for `400`, `404`, `409`, `422`, and fallback `500`
- the example-folder mock station scenario can seed SQLite and exercise the current HTTP happy path

The project does not cover:

- full happy-path HTTP transport scenarios
- authentication and authorization
- multi-slice error-contract standardization beyond the current operator-execution seam

## File Structure

```text
MockOperatorExecutionExampleSmokeTests.cs
OperatorExecutionExperienceApiEndpointRouteBuilderTests.cs
OperatorExecutionProblemDetailsTests.cs
```

## Key Test File

### `OperatorExecutionExperienceApiEndpointRouteBuilderTests.cs`

Locks the host composition seam so the team can change the current durable provider
without leaking that choice into route handlers or the application layer.

### `OperatorExecutionProblemDetailsTests.cs`

Locks the thin-host error contract so deterministic operator-execution failures
normalize to stable problem-details payloads instead of leaking raw exception behavior.

### `MockOperatorExecutionExampleSmokeTests.cs`

Locks the `example/Mes.MockStation.Example/` scenario so the queued and running
mock station sample still works against the real thin-host HTTP path.

## Current Limitations

- `Postgres` is only a reserved provider value right now; there is no concrete
  adapter behind it yet.
- HTTP coverage currently focuses on error normalization, not the full happy-path
  request and response matrix.
