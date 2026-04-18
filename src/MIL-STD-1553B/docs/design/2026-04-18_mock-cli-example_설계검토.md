# 2026-04-18_mock-cli-example_설계검토

## 배경

- 현재 저장소는 simulator / mock 경로를 통해 실장비 없이도 세션 시작, Bus 전환, telemetry 조회가 가능하다.
- 다만 사용자가 저장소를 받은 직후 바로 실행해 볼 수 있는 canonical example 자산과 self-checking 스크립트는 없었다.

## 목표

- 실장비 없이도 현재 MVP가 실제로 돌아간다는 것을 재현 가능한 mock example으로 제공한다.
- example은 문서만이 아니라 실제 CLI 실행과 출력 검증까지 포함해야 한다.

## 범위

- `examples/mock-cli` 아래 example scenario와 실행 스크립트 추가
- example 경로를 실제로 실행하는 .NET smoke test 추가
- quick guide와 테스트 문서 갱신

## 제외 범위

- 실장비용 벤더 SDK binding
- WPF / WinUI 같은 rich UI
- report / replay 기능 확장

## 용어 정의

- mock example: 실장비 없이 simulator / native mock path를 이용해 실제 CLI 프로세스를 실행하는 예제
- self-checking script: 실행만 하지 않고 필수 출력 패턴까지 스스로 검증하는 스크립트

## 요구사항

1. 사용자는 저장소 루트에서 PowerShell 스크립트 한 번으로 example을 실행할 수 있어야 한다.
2. example은 현재 canonical CLI 진입점인 `MilStd1553.Cli`를 사용해야 한다.
3. example은 실제 native DLL을 포함한 publish output으로 실행돼야 한다.
4. example 출력에는 최소한 세션 시작, 초기 health, Bus 전환, telemetry 2건, 세션 종료가 드러나야 한다.
5. example 검증은 자동 테스트로도 다시 실행 가능해야 한다.

## 책임 분리

### C++

- 추가 변경 없음
- 기존 `NativeSessionApi`와 simulator runtime path를 그대로 재사용한다.

### C#

- CLI example 실행 자산과 smoke test를 추가한다.
- 문서와 quick guide를 갱신한다.

## 레이어 영향도

- Domain: 영향 없음
- Application: 영향 없음
- Infrastructure: 영향 없음
- Presentation / Host: example 실행 경로와 테스트, 문서가 추가된다.

## 클래스 / 인터페이스 설계 초안

- 새 production class는 추가하지 않는다.
- 새 example 자산:
  - `examples/mock-cli/mock.scenario.json`
  - `examples/mock-cli/run_mock_cli_example.ps1`
  - `examples/mock-cli/README.md`
- 새 테스트:
  - `MockCliExampleSmokeTests`

## 데이터 흐름 또는 시퀀스

1. example script가 CLI project를 publish한다.
2. publish output의 `MilStd1553.Cli.dll`을 example scenario와 함께 실행한다.
3. CLI는 `SessionCoordinator -> NativeHarnessClient -> NativeSessionApi` 경로로 세션을 시작한다.
4. 초기 schedule pass 결과 `MessageFrame`과 이후 `BusSwitch` telemetry를 콘솔에 출력한다.
5. script는 출력 로그를 저장하고 필수 패턴을 검증한다.

## 테스트 전략

- Red:
  example script smoke test를 먼저 추가하고, example 자산이 없으면 실패하게 만든다.
- Green:
  scenario JSON, script, README를 추가하고 smoke test를 통과시킨다.
- Refactor:
  quick guide와 상태 문서에 canonical example 경로를 반영한다.

## 리스크 및 대응

- 리스크: nested `dotnet publish`가 느리거나 CI / 로컬 환경 차이를 만들 수 있다.
  대응: temp output을 사용하고, native runtime layout을 publish output 기준으로 고정한다.
- 리스크: 출력 포맷이 바뀌면 script 검증 패턴이 깨질 수 있다.
  대응: example이 의존하는 최소 출력 계약만 고정한다.

## 완료 기준

- `examples/mock-cli` 아래 example scenario와 실행 스크립트가 존재한다.
- script가 실제로 publish 후 CLI를 실행하고 필수 출력 패턴을 검증한다.
- example smoke test가 자동화되어 통과한다.
- README와 테스트 문서가 현재 경로를 안내한다.
