# Mes.Client.Wpf.Tests

## Purpose

`Mes.Client.Wpf.Tests` validates the WPF station shell's non-visual behavior.
The project now covers pure shell policies, shared command-context generation,
and focused fake-HTTP client behavior so the WPF station client can widen
safely without regressing the shared BFF seam.

## Responsibility Boundary

This project owns:

- station session normalization tests
- operator-facing problem display policy tests
- shared WPF-side command-context policy tests
- fake-HTTP tests for `OperatorExecutionStationClient`
- shell-state tests for rebind clearing, severity propagation, async-command failure handling, queue-projected completion units, and start/material-consumption/complete command flows

This project does not own:

- full WPF rendering tests
- STA window automation
- end-to-end Experience API host integration

## Main Files

- [StationSessionStateTests.cs](/D:/01_MyStudy/03.TEST/src/Modbus/tests/Mes.Client.Wpf.Tests/StationSessionStateTests.cs)
- [OperatorExecutionProblemDisplayPolicyTests.cs](/D:/01_MyStudy/03.TEST/src/Modbus/tests/Mes.Client.Wpf.Tests/OperatorExecutionProblemDisplayPolicyTests.cs)
- [OperatorExecutionStationClientTests.cs](/D:/01_MyStudy/03.TEST/src/Modbus/tests/Mes.Client.Wpf.Tests/OperatorExecutionStationClientTests.cs)
- [ShellViewModelTests.cs](/D:/01_MyStudy/03.TEST/src/Modbus/tests/Mes.Client.Wpf.Tests/ShellViewModelTests.cs)

## Dependencies

- `xUnit`
- `Mes.Client.Wpf`

## Test Flow

1. Reference the WPF shell project directly.
2. Create shell objects or fake `HttpMessageHandler` instances in-process.
3. Validate queue refresh, command-context generation, mutation flows, and failure normalization without starting a real WPF window or HTTP server.

## Current Limitations

- There is still no full STA window automation coverage.
- The tests validate typed client and shell behavior, not the complete Experience API host pipeline.
- Future mutation flows beyond the current `start-operation`, `record-material-consumption`, and `complete-operation` surface will still need additional coverage.
