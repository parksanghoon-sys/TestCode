# 2026-04-18_mock-cli-example_테스트계획

## 테스트 대상

- `examples/mock-cli/mock.scenario.json`
- `examples/mock-cli/run_mock_cli_example.ps1`
- `tests/dotnet/MilStd1553.Host.Tests/MockCliExampleSmokeTests.cs`

## 테스트 시나리오

1. example script가 CLI project를 publish한다.
2. publish output의 `MilStd1553.Cli.dll`과 runtime layout native DLL이 함께 생성된다.
3. example scenario로 CLI를 실행하면 세션 시작이 성공한다.
4. 초기 health는 `activeBus=A`로 출력된다.
5. `--switch-bus B` 이후 current health는 `activeBus=B`로 출력된다.
6. `--poll-telemetry` 결과로 `MessageFrame` 1건과 `BusSwitch` 1건이 출력된다.
7. 종료 시 `sessionStopped=true`가 출력되고 example script는 exit code `0`으로 끝난다.

## 정상 흐름

- PowerShell script 실행
- CLI publish 성공
- scenario 기반 session start 성공
- telemetry 2건 확인
- session stop 성공

## 비정상 흐름

- publish 실패 시 script가 예외와 함께 실패해야 한다.
- 필수 출력 패턴이 누락되면 script가 실패해야 한다.
- example smoke test는 script exit code가 `0`이 아니면 실패해야 한다.

## 타임아웃 / 재시도

- example 자체는 별도 retry를 추가하지 않는다.
- CLI / native 내부의 기존 `BufferTooSmall` / runtime loader 정책을 그대로 따른다.

## Bus A/B 전환

- example은 `activeBus=A`로 시작하고 `--switch-bus B`를 적용한다.
- 출력 로그에서 전환 전 / 후 상태를 모두 확인한다.

## BC → RT

- example session 시작 시 초기 schedule pass가 수행된다.
- 첫 telemetry로 `MessageFrame`을 확인한다.

## RT → BC

- example scenario는 `Transmit` direction을 사용해 RT -> BC poll 경로를 재사용한다.

## RT ↔ RT

- 이번 example 범위에서는 포함하지 않는다.

## Mode Code

- 이번 example 범위에서는 포함하지 않는다.

## 로그 확인 포인트

- `sessionHandle=`
- `initialHealth activeBus=A`
- `switchedBus=B`
- `currentHealth activeBus=B`
- `telemetryCount=2`
- `telemetry[0] type=MessageFrame`
- `telemetry[1] type=BusSwitch`
- `sessionStopped=true`

## 결과 요약

- script와 smoke test가 모두 성공하면 mock example 경로를 usable 상태로 본다.
