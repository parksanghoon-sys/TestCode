# 2026-04-18_RTtoRT-fidelity_테스트결과

## 1. 테스트 대상

- `src/native/Application/BusControllerService`
- `src/native/Domain/TransferTypes`
- `tests/native/NativeTests.cpp`
- `tests/dotnet/MilStd1553.Host.Tests/NativeHarnessClientEndToEndTests.cs`

## 2. 실행 명령

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1`
- `dotnet build-server shutdown`
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj`

## 3. 테스트 시나리오 결과

### 정상 흐름

1. `BusControllerPublishesReceivePayloadInMessageFrame`
   - 결과: 통과
   - 확인: BC -> RT receive telemetry가 `0x1201`, `0x1202` payload를 그대로 기록했다.
2. `SimulatorAdapterSupportsRtToRtTransfer`
   - 결과: 통과
   - 확인: source / destination 두 개의 `MessageFrame`이 남았고, description이 각각 `RT↔RT 소스 RT transmit 단계`, `RT↔RT 목적지 RT receive 단계`로 구분됐다.
   - 확인: destination frame payload가 `0xA100`, `0xA200`으로 유지됐고 time-tag도 source 이후로 증가했다.
3. `NativeHarnessClient_WithReceiveScenario_PreservesPayloadInTelemetry`
   - 결과: 통과
   - 확인: 실제 runtime layout DLL 기준 receive scenario의 첫 telemetry에서 payload `0x0102`가 Host DTO까지 유지됐다.

### 비정상 흐름

1. `SimulatorAdapterRejectsInvalidRtToRtTransfer`
   - 결과: 통과
   - 확인: word count가 맞지 않는 RT ↔ RT 요청은 거부되고 destination 단계가 시작되지 않았다.
2. `SimulatorAdapterRejectsModeCodeRtToRtTransfer`
   - 결과: 통과
   - 확인: Mode Code RT ↔ RT 요청은 `InvalidWord`로 거부되고 telemetry를 남기지 않았다.

## 4. 회귀 확인

- 네이티브 전체 테스트: 29건 통과
- .NET 전체 테스트: 34건 통과
- 기존 timeout / retry / Bus A/B 전환 / runtime loader / CLI mock example 회귀 없음

## 5. 환경 메모

- 첫 번째 `.NET` 실행은 `MilStd1553.Interop.dll` obj 파일 잠금으로 실패했다.
- `dotnet build-server shutdown` 이후 같은 명령을 더 긴 timeout으로 다시 실행해 정상 통과를 확인했다.

## 6. 결과 요약

- receive 계열 telemetry payload 손실 문제가 해소됐다.
- RT ↔ RT trace는 source / destination 단계가 더 분명해졌다.
- timing / intermessage gap 모델 고도화는 이번 범위 밖으로 남겼다.
