# 2026-04-18_RTtoRT-timing-fidelity_테스트결과
## 1. 테스트 대상

- `src/native/Application/BusControllerService`
- `tests/native/NativeTests.cpp`
- 영향 범위 smoke 확인용 `tests/dotnet/MilStd1553.Host.Tests/NativeHarnessClientEndToEndTests.cs`

## 2. 실행 명령

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1`
- `dotnet build-server shutdown`
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests" -v normal`

## 3. 테스트 시나리오 결과

### 정상 흐름

1. `SimulatorAdapterSupportsRtToRtTransfer`
   - 결과: 통과
   - 확인: source `TransferResult.timeTag`가 `100us`, destination `TransferResult.timeTag`가 `140us`로 보정되었다.
   - 확인: destination `MessageFrame.timeTag`도 `140us`로 맞춰져 result와 telemetry가 같은 기준을 가진다.
   - 확인: source / destination 두 단계 description과 payload 보존은 유지된다.

2. `NativeHarnessClient_WithRuntimeLayoutNativeDll_CompletesSessionLifecycle`
   - 결과: 통과
   - 확인: 네이티브 C ABI 런타임 smoke가 여전히 `MessageFrame -> BusSwitch -> []` 흐름을 유지한다.

3. `NativeHarnessClient_WithReceiveScenario_PreservesPayloadInTelemetry`
   - 결과: 통과
   - 확인: BC → RT receive payload가 Host DTO까지 그대로 유지된다.

### 비정상 흐름

1. `SimulatorAdapterRejectsInvalidRtToRtTransfer`
   - 결과: 통과
   - 확인: word count가 맞지 않는 RT ↔ RT 요청은 계속 거부된다.

2. `SimulatorAdapterRejectsModeCodeRtToRtTransfer`
   - 결과: 통과
   - 확인: Mode Code RT ↔ RT 요청은 계속 `InvalidWord`로 거부되고 telemetry를 남기지 않는다.

## 4. 회귀 결과

- 네이티브 전체 테스트: 29건 통과
- 영향 범위 `.NET` end-to-end smoke: 5건 통과

## 5. 환경 메모

- `.NET` 전체 스위트는 현재 환경에서 `MilStd1553.Interop.dll` obj 잠금과 장시간 실행 이슈가 반복됐다.
- 이번 사이클은 C# 코드 변경이 없고 네이티브 RT ↔ RT timing 보정이 핵심이므로, 영향 범위인 `NativeHarnessClientEndToEndTests`로 회귀를 제한했다.
- 잠금 해제는 `dotnet build-server shutdown`과 VS Code `csdevkit` / language server 프로세스 정리 후 진행했다.

## 6. 결과 요약

- RT ↔ RT 최소 intermessage gap baseline이 네이티브 서비스에 반영됐다.
- 목적지 단계 time-tag는 result와 telemetry frame 양쪽에서 일치한다.
- 기존 C ABI / Host smoke 경로는 유지된다.
