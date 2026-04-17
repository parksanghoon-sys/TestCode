# Mes.MockStation.Example

## Purpose

`Mes.MockStation.Example` creates a runnable mock station scenario under
`example/` so the current operator-execution WPF shell and Experience API can be
verified without real equipment, PLC, or Modbus hardware.

## Responsibility Boundary

This project does not add a new business path.

- It seeds the existing `Mes.Infrastructure/Sqlite` runtime with fixed sample data.
- It writes a manifest JSON that example scripts and smoke tests can reuse.
- It keeps the mock at the example edge instead of introducing a fake device path
  into `Mes.Domain`, `Mes.Application`, `Mes.ExperienceApi`, or `Mes.Client.Wpf`.

## Folder Structure

```text
Program.cs
MockOperatorExecutionContracts.cs
MockOperatorExecutionScenarioSeeder.cs
Run-MockStationDemo.ps1
Test-MockStationDemo.ps1
README.md
```

## Main Files

- `Program.cs`: console entry point that generates `.runtime/operator-execution-example.db`
  and `.runtime/mock-station-scenario.json`.
- `MockOperatorExecutionScenarioSeeder.cs`: builds the fixed queued/running scenario,
  seeds SQLite, and writes the manifest.
- `Run-MockStationDemo.ps1`: prepares the scenario and opens the Experience API in a
  new PowerShell window. Add `-LaunchWpf` to also open the WPF shell with env-var overrides.
- `Test-MockStationDemo.ps1`: headless smoke script that seeds the example, starts
  the API, then exercises queue fetch, `start-operation`, `record-material-consumption`,
  and `complete-operation`.

## How To Run

Generate the example assets only:

```powershell
dotnet run --project example/Mes.MockStation.Example/Mes.MockStation.Example.csproj -- --output-root example/Mes.MockStation.Example/.runtime
```

Start the mock API:

```powershell
pwsh -File example/Mes.MockStation.Example/Run-MockStationDemo.ps1
```

Start the mock API and the WPF shell together:

```powershell
pwsh -File example/Mes.MockStation.Example/Run-MockStationDemo.ps1 -LaunchWpf
```

Run the headless smoke test:

```powershell
pwsh -File example/Mes.MockStation.Example/Test-MockStationDemo.ps1
```

## Example Inputs

The generated manifest keeps the fixed sample IDs that are useful when you test
the WPF shell manually.

- Station: `ST-EXAMPLE-01`
- Actor: `operator.mock.example`
- Queued operation for `start-operation`: `OP-EXAMPLE-START`
- Running operation for `material-consumption` and `complete-operation`: `OP-EXAMPLE-RUN`
- Running WIP: `WIP-EXAMPLE-01`
- Running material lot: `LOT-EXAMPLE-01`

## Current Limitations

- This example proves the current business-command path only; it does not simulate
  a real `Edge` or Modbus handshake.
- The running-operation branch uses manual IDs for WIP and material lot because
  the current WPF shell still expects those identifiers as direct operator input.
