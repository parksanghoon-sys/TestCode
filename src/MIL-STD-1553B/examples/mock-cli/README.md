# Mock CLI Example

이 디렉터리는 실장비 없이 simulator / mock 경로로 MIL-STD-1553B 하네스를 바로 실행해 보는 예제를 담습니다.

## 포함 파일

- `mock.scenario.json`
  CLI smoke test에 사용하는 기본 시나리오입니다.
- `run_mock_cli_example.ps1`
  CLI를 publish한 뒤 example scenario로 실제 세션을 실행하고, 출력까지 자체 검증하는 스크립트입니다.

## 실행 방법

저장소 루트에서 아래 명령을 실행합니다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\examples\mock-cli\run_mock_cli_example.ps1
```

성공하면 다음을 확인합니다.

- `sessionHandle=...`
- `initialHealth activeBus=A`
- `switchedBus=B`
- `telemetryCount=2`
- `telemetry[0] type=MessageFrame`
- `telemetry[1] type=BusSwitch`
- `sessionStopped=true`

기본 출력 로그는 `artifacts/mock-cli-example/mock-cli-output.log`에 저장됩니다.
