# 2026-04-18_mock-cli-example_테스트결과

## 1. 테스트 대상

- `examples/mock-cli/mock.scenario.json`
- `examples/mock-cli/run_mock_cli_example.ps1`
- `tests/dotnet/MilStd1553.Host.Tests/MockCliExampleSmokeTests.cs`

## 2. 테스트 환경

- 운영체제: Windows
- .NET SDK: `10.0.103`
- 실행 명령:
  `powershell -NoProfile -ExecutionPolicy Bypass -File .\examples\mock-cli\run_mock_cli_example.ps1`
  `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --filter "FullyQualifiedName~MockCliExampleSmokeTests"`

## 3. 테스트 시나리오

1. example script가 CLI project를 publish한다.
2. publish output의 `MilStd1553.Cli.dll`과 runtime layout native DLL이 함께 생성된다.
3. example scenario로 CLI를 실행하면 세션 시작이 성공한다.
4. 초기 health는 `activeBus=A`로 출력된다.
5. `--switch-bus B` 이후 current health는 `activeBus=B`로 출력된다.
6. `--poll-telemetry` 결과로 `MessageFrame` 1건과 `BusSwitch` 1건이 출력된다.
7. 종료 시 `sessionStopped=true`가 출력된다.

## 4. 입력 조건

- example scenario는 `channelId=0`, `activeBus=A`, `bcSchedules` 1건(`Transmit`)을 사용했다.
- example script는 CLI를 `artifacts/mock-cli-example/publish` 또는 테스트용 temp output에 publish한 뒤, publish output의 `MilStd1553.Cli.dll`을 직접 실행했다.
- script는 콘솔 출력 전체를 `mock-cli-output.log`에 저장하고 필수 패턴을 검증했다.

## 5. 기대 결과

- 실장비 없이도 CLI가 세션 시작, Bus 전환, telemetry 출력, 세션 종료를 끝까지 수행해야 한다.
- 출력에는 `MessageFrame`과 `BusSwitch`가 모두 포함되어야 한다.
- example script와 smoke test는 모두 exit code `0`으로 끝나야 한다.

## 6. 실제 결과

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\examples\mock-cli\run_mock_cli_example.ps1` 실행이 성공했다.
- example 콘솔 출력에는 아래가 모두 포함됐다.
  - `sessionHandle=session-0001`
  - `initialHealth activeBus=A`
  - `switchedBus=B`
  - `currentHealth activeBus=B`
  - `telemetryCount=2`
  - `telemetry[0] type=MessageFrame`
  - `telemetry[1] type=BusSwitch`
  - `sessionStopped=true`
- 로그 파일 `artifacts/mock-cli-example/mock-cli-output.log`가 생성됐다.
- `MockCliExampleSmokeTests` 1건이 통과했다.

## 7. 로그 확인 포인트

- 첫 telemetry:
  `telemetry[0] type=MessageFrame activeBus=A`
- 두 번째 telemetry:
  `telemetry[1] type=BusSwitch activeBus=B`
- 종료 확인:
  `sessionStopped=true`

## 8. 실패 케이스

- Red 단계에서는 example 자산과 script가 없으므로 smoke test가 실패해야 하는 상태였다.
- Green 단계에서 scenario, script, README를 추가한 뒤 smoke test와 실제 script 실행이 모두 통과했다.

## 9. 리스크

- example은 simulator / mock 경로만 다룬다.
- 실장비용 `IVendorChannel` binding이 생기면 별도 hardware example path를 추가해야 한다.

## 10. 후속 조치

- 실장비 SDK가 확보되면 `examples/hardware-*` 계열 example을 별도로 추가한다.
- 필요 시 mock example 출력에 report export나 replay step을 확장한다.
