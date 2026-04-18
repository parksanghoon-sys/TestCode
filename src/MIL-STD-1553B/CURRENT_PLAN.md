# CURRENT_PLAN

## 현재 목표

- 실제 세션 중심 C ABI가 네이티브 Application / Infrastructure 런타임을 직접 조립하는 MVP 기준선을 유지한다.
- 남아 있던 확장 설계는 `docs/design/2026-04-18_잔여확장설계_설계검토.md`에서 닫혔고, 다음 사이클부터는 구현 backlog를 순서대로 실행한다.

## 현재 실행 기준

- `NativeSessionApi`는 더 이상 health / pending-only stub이 아니다.
- `OpenSession`은 시나리오 JSON을 파싱한 뒤 `BusMonitorService`, `SimulatorVendorChannel`, `VendorSdkBusAdapter`, `BusControllerService`를 조립한다.
- `OpenSession` 시 초기 BC schedule pass를 1회 수행하고, 첫 번째 `PollTelemetry`는 그 결과 `MessageFrame`을 반환한다.
- `SwitchBus` 이후 다음 `PollTelemetry`는 `BusSwitch` 이벤트를 반환한다.
- `PollTelemetry`는 직렬화가 성공했을 때만 cursor를 전진시킨다.
- CLI / Host / Interop / report / replay / runtime loader / packaging MVP 기준선은 유지된다.
- `examples/mock-cli`가 실장비 없는 canonical mock example 경로로 추가되었다.
- `BusControllerService`는 receive 계열 `MessageFrame` telemetry에 실제 bus payload를 남기고, RT ↔ RT는 source / destination 단계를 설명과 순서로 구분한다.
- `BusControllerService`는 RT ↔ RT 목적지 단계에 시뮬레이터용 최소 `40us` gap을 적용해 result / telemetry time-tag를 같은 기준으로 정규화한다.
- `SimulatorBusAdapter`는 1차 Mode Code 묶음 4종(`Reset Remote Terminal`, `Transmit Last Command Word`, `Inhibit Terminal Flag`, `Override Inhibit Terminal Flag`)을 지원한다.
- 실장비 binding은 기존 `NativeSessionApi -> VendorSdkBusAdapter -> IVendorChannel` 경로를 유지하고, callback 기반 SDK는 concrete `IVendorChannel` 내부에서 정규화하기로 설계가 고정되었다.
- replay / report는 JSONL canonical source와 schema v1 envelope(`schemaVersion`, `recordType`, `sessionId`, `sequence`) 기준으로 확장하기로 설계가 고정되었다.
- canonical operator entry point는 계속 `MilStd1553.Cli`이며, rich UI가 필요하면 첫 GUI baseline은 `WPF`로 시작하기로 설계가 고정되었다.
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 기준 네이티브 34건 통과
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests"` 기준 영향 범위 .NET 5건 통과

## 다음 실행 단위

1. 외부 정보 없이 바로 진행할 다음 구현은 Host 쪽 JSONL schema v1 도입과 legacy replay compatibility 구현이다.
2. 그다음 RT ↔ RT `RtToRtSequencePlanner` 기반 richer physical sequence 설계를 구현 단위로 쪼갠다.
3. 실제 벤더 SDK 정보가 확보되면 `HardwareVendorChannel` concrete binding 1종을 구현한다.

## 후속 작업 후보

- replay / report schema v1 구현
- RT ↔ RT `RtToRtSequencePlanner` 기반 richer physical sequence 구현
- 실장비용 `IVendorChannel` binding
- 필요 시 `WPF` 운영 Shell 추가

## 검증 경로

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1`
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests"`

## 확인된 사실

- C++는 Domain / Application / Infrastructure / Interop 책임 분리를 유지한다.
- C#은 Host / Interop / CLI / report / replay 책임을 유지한다.
- 운영용 기본 실행 진입점은 `src/dotnet/MilStd1553.Cli`이다.
- 라이브러리 packaging 기준점은 `src/dotnet/MilStd1553.Interop`이다.
- `MilStd1553.Interop.csproj`와 `MilStd1553.Cli.csproj` 모두 build / publish 시 managed DLL과 native DLL을 함께 배치한다.

## 미확정 사항

- 실제로 사용할 벤더 카드와 SDK 종류
