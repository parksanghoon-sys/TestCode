# Mes.Client.Wpf

## Purpose

`Mes.Client.Wpf` is the first executable station-focused client for the MES pilot.
It now proves a queue-plus-command operator shell over the shared
operator-execution BFF without introducing direct domain, database, or PLC
access paths.

## Responsibility Boundary

This project owns:

- station binding and session display
- operator work-queue reads through `Mes.ExperienceApi`
- shared WPF-side command-context generation for operator mutations
- `start-operation`, `record-material-consumption`, and `complete-operation` command submission over the shared BFF
- operator-facing normalization of server and client-side failures
- severity-aware shell messaging for info, warning, and error states
- WPF shell startup through Generic Host and DI

This project does not own:

- direct business workflow authority
- durable persistence
- PLC or Modbus control paths
- offline command replay storage

## Folder Structure

- [App.xaml](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/App.xaml)
- [App.xaml.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/App.xaml.cs)
- [MainWindow.xaml](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/MainWindow.xaml)
- [Configuration](</D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/Configuration>)
- [OperatorExecution](</D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/OperatorExecution>)
- [Shell](</D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/Shell>)
- [Support](</D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/Support>)

## Key Files

- [App.xaml.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/App.xaml.cs): Generic Host, DI, and typed `HttpClient` composition.
- [OperatorExecutionStationClient.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/OperatorExecution/OperatorExecutionStationClient.cs): shared BFF GET and POST calls plus client-side normalization for connectivity, empty-body, and invalid-body failures.
- [StationCommandContextFactory.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/OperatorExecution/StationCommandContextFactory.cs): central WPF policy for `commandId`, `correlationId`, `idempotencyKey`, actor, and client timestamp generation.
- [OperatorExecutionProblemDisplayPolicy.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/OperatorExecution/OperatorExecutionProblemDisplayPolicy.cs): maps server and client failures into operator-facing messages and severity.
- [ShellViewModel.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/Shell/ShellViewModel.cs): manages station binding, queue refresh, selected-operation state, `start-operation`, `record-material-consumption`, `complete-operation`, and severity-aware shell messaging.
- [WorkQueueItemViewModel.cs](/D:/01_MyStudy/03.TEST/src/Modbus/src/Mes.Client.Wpf/Shell/WorkQueueItemViewModel.cs): adapts shared queue contracts into UI-facing row models and derives default material code, material unit, and completion unit hints from MES-owned queue data.

## Dependencies

- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.Http`
- `Mes.Application.Contracts`

## Execution Flow

1. Startup loads `appsettings.json` for the BFF base address and default station ID.
2. `ShellViewModel` initializes the queue-first station shell state.
3. The operator binds a station session.
4. The shell clears any previously displayed snapshot on rebind so one station cannot inherit another station's queue view.
5. Queue refresh calls `GetStationWorkQueue` through the shared BFF contract.
6. The shell selects one queue row and generates a shared WPF command context through `StationCommandContextFactory`.
7. `start-operation`, `record-material-consumption`, and `complete-operation` post the shared command contracts to `Mes.ExperienceApi`.
8. `record-material-consumption` uses the same shared WPF command-context factory, but adds one per-action token so repeated material shots on the same operation do not collapse into one idempotency scope.
9. `complete-operation` now reads the authoritative quantity unit from the selected queue item instead of asking the operator to type the unit manually.
10. Accepted commands clear the stale queue snapshot, refresh the current station queue again, and surface the result as operator-facing shell messages.
11. Failures are normalized into severity-aware operator messages.

## Current Limitations

- Device integration still stops at HTTP communication with the shared BFF.
- Severity is now reflected in the shell panel, but there is still no broader alarm center, toast system, or acknowledgement workflow.
- `record-material-consumption` still depends on operator-entered `WIP`, material lot, and quantity input because the current queue contract does not project a scan-first material selection flow.
- `OperatorExecutionStationClientOptions.DefaultCompletionQuantityUnit` now acts only as a fallback shell default when no queue item is selected; the normal completion flow uses the queue-projected unit.
