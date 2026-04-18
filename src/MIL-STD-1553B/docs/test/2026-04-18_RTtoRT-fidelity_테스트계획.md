# 2026-04-18_RTtoRT-fidelity_테스트계획

## 1. 테스트 대상

- `src/native/Application/BusControllerService`
- `src/native/Domain/TransferTypes`
- `tests/native/NativeTests.cpp`
- `tests/dotnet/MilStd1553.Host.Tests/NativeHarnessClientEndToEndTests.cs`

## 2. 테스트 시나리오

### 정상 흐름

1. BC -> RT receive telemetry가 request payload를 그대로 기록한다.
2. RT ↔ RT source 단계가 `RT↔RT 소스 RT transmit 단계` description으로 남는다.
3. RT ↔ RT destination 단계가 `RT↔RT 목적지 RT receive 단계` description으로 남는다.
4. RT ↔ RT destination `MessageFrame.dataWords`가 source payload와 동일하다.
5. Host end-to-end receive scenario가 payload를 잃지 않는다.

### 비정상 흐름

1. word count가 맞지 않는 RT ↔ RT 요청은 거부된다.
2. Mode Code RT ↔ RT 요청은 거부된다.

## 3. 타임아웃 / 재시도

- 이번 슬라이스의 직접 변경 대상은 아니므로 기존 timeout / retry 회귀가 깨지지 않는지만 전체 native 회귀로 확인한다.

## 4. Bus A/B 전환

- 변경 대상은 아니지만 전체 native / Host 회귀에서 기존 `BusSwitch` 경로가 유지되는지 함께 확인한다.

## 5. BC -> RT

- receive payload가 telemetry에 남는지를 새 테스트로 확인한다.

## 6. RT -> BC

- 기존 `SimulatorAdapterSupportsRtToBcTransfer` 회귀가 유지되는지 전체 native 회귀로 확인한다.

## 7. RT ↔ RT

- source / destination description, destination payload, time-tag 순서를 확인한다.

## 8. Mode Code

- Mode Code RT ↔ RT 요청 거부를 별도 테스트로 고정한다.

## 9. 로그 확인 포인트

- native:
  - `BusControllerPublishesReceivePayloadInMessageFrame`
  - `SimulatorAdapterSupportsRtToRtTransfer`
  - `SimulatorAdapterRejectsModeCodeRtToRtTransfer`
- .NET:
  - `NativeHarnessClient_WithReceiveScenario_PreservesPayloadInTelemetry`

## 10. 결과 요약 기준

- 네이티브 전체 테스트가 모두 통과해야 한다.
- .NET 전체 테스트가 모두 통과해야 한다.
- receive payload가 native telemetry와 Host DTO에서 모두 확인돼야 한다.
