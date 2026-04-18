# 2026-04-18_ModeCode-tranche1_테스트결과

## 1. 테스트 대상

- `src/native/Domain/BusTypes.h`
- `src/native/Infrastructure/SimulatorBusAdapter.h`
- `src/native/Infrastructure/SimulatorBusAdapter.cpp`
- `tests/native/NativeTests.cpp`

## 2. 테스트 환경

- 운영체제: Windows
- 네이티브 컴파일러: Visual Studio 2022 MSVC (`cl.exe`)
- 네이티브 실행 명령:
  `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1`
- Host 영향 범위 smoke 명령:
  `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests" -v normal`

## 3. 테스트 시나리오

1. 직전 성공 커맨드가 있을 때 `Transmit Last Command Word`가 이전 커맨드 raw 값을 반환한다.
2. 직전 성공 커맨드가 없을 때 `Transmit Last Command Word`가 `InvalidWord`로 실패한다.
3. `Inhibit Terminal Flag` 이후 `Transmit Status Word` 응답에서 terminal flag 비트가 숨겨진다.
4. `Override Inhibit Terminal Flag` 이후 terminal flag 비트가 다시 노출된다.
5. `Reset Remote Terminal` 이후 RT 상태가 기본값으로 돌아간다.
6. `Dynamic Bus Control`은 계속 `UnsupportedModeCode`로 거부된다.
7. 기존 대표 Mode Code 5종, RT ↔ RT, failover, C ABI lifecycle이 regress 되지 않는다.

## 4. 기대 결과

- 1차 Mode Code 묶음 4종이 simulator adapter에서 정상 처리된다.
- terminal flag inhibit는 저장된 상태 워드를 덮어쓰지 않고 노출 규칙만 바꾼다.
- `Transmit Last Command Word`는 현재 요청이 아니라 직전 성공 커맨드를 반환한다.
- `Reset Remote Terminal`은 서브어드레스 데이터, synchronize word, status bit를 기본 상태로 되돌린다.
- 2차 묶음인 `Dynamic Bus Control`은 여전히 미지원 상태를 유지한다.

## 5. 실제 결과

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 실행 결과 네이티브 테스트 34건이 모두 통과했다.
- 신규 테스트 5건이 모두 통과했다.
  - `SimulatorAdapterSupportsTransmitLastCommandWordModeCode`
  - `SimulatorAdapterRejectsTransmitLastCommandWordWithoutHistory`
  - `SimulatorAdapterSupportsInhibitAndOverrideTerminalFlagModeCodes`
  - `SimulatorAdapterResetsRemoteTerminalState`
  - `SimulatorAdapterKeepsDynamicBusControlUnsupported`
- `dotnet build-server shutdown` 후
  `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests" -v normal`
  실행 결과 영향 범위 .NET 5건이 통과했다.
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 전체 스위트는 현재 환경에서 장시간 실행으로 타임아웃이 발생해 이번 사이클에서는 영향 범위 smoke만 결과로 채택했다.

## 6. 로그 확인 포인트

- 네이티브 테스트 요약:
  `[PASS]` 34건, 최종 메시지 `전체 네이티브 테스트 통과: 34건`
- 영향 범위 .NET 테스트 요약:
  통과 `5`, 실패 `0`

## 7. 실패 케이스

- Red 단계에서는 `ModeCode` enum과 simulator mode handler에 새 분기가 없어 컴파일이 실패했다.
- 이후 1차 Mode Code 4종 enum 추가, RT 상태 모델 확장, effective status helper와 마지막 커맨드 기록 규칙을 구현한 뒤 Green으로 전환했다.

## 8. 리스크

- 이번 구현은 simulator semantics 기준선이며, 실제 하드웨어 SDK가 동일한 내부 상태 모델을 제공한다는 뜻은 아니다.
- full .NET suite는 이번 사이클에서 시간 제한 때문에 다시 끝까지 확인하지 못했다.

## 9. 후속 조치

- 다음 실행 단위는 Host `schema v1` JSONL exporter / replay reader 구현이다.
- 2차 Mode Code 묶음과 RT ↔ RT planner / policy는 deferred backlog로 유지한다.
