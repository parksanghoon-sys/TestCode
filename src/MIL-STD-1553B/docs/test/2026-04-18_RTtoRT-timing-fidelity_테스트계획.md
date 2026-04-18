# 2026-04-18_RTtoRT-timing-fidelity_테스트계획
## 1. 테스트 대상

- `src/native/Application/BusControllerService`
- `tests/native/NativeTests.cpp`

## 2. 테스트 시나리오

### 정상 흐름

1. RT ↔ RT source 단계 결과가 성공한다.
2. RT ↔ RT destination 단계 결과가 성공한다.
3. destination `TransferResult.timeTag`가 source보다 설계상 최소 gap 이상 크다.
4. destination telemetry `MessageFrame.timeTag`가 destination `TransferResult.timeTag`와 같다.

### 비정상 흐름

1. word count가 맞지 않는 RT ↔ RT 요청은 계속 거부된다.
2. Mode Code RT ↔ RT 요청은 계속 거부된다.

## 3. 타임아웃 / 재시도

- 이번 변경은 timeout / retry 정책을 직접 바꾸지 않는다.
- 전체 native 회귀에서 기존 timeout / retry 테스트가 유지되는지만 확인한다.

## 4. Bus A/B 전환

- 이번 변경은 Bus A/B 전환 정책을 바꾸지 않는다.
- 전체 native / .NET 회귀에서 기존 BusSwitch 경로가 유지되는지만 확인한다.

## 5. BC → RT

- 이번 변경은 BC → RT 경로를 바꾸지 않는다.
- 기존 receive payload telemetry 테스트가 유지되는지 확인한다.

## 6. RT → BC

- 이번 변경은 RT → BC 경로를 바꾸지 않는다.
- 기존 `SimulatorAdapterSupportsRtToBcTransfer`가 유지되는지 확인한다.

## 7. RT ↔ RT

- `SimulatorAdapterSupportsRtToRtTransfer`
  - source / destination 단계 성공
  - 목적지 저장 payload 유지
  - source / destination description 유지
  - destination time-tag가 source + 최소 gap 이상
  - destination `TransferResult.timeTag`와 destination `MessageFrame.timeTag` 일치

## 8. Mode Code

- `SimulatorAdapterRejectsModeCodeRtToRtTransfer`
  - RT ↔ RT Mode Code 거부가 유지되는지 확인한다.

## 9. 로그 확인 포인트

- `SimulatorAdapterSupportsRtToRtTransfer`
- `BusControllerPublishesReceivePayloadInMessageFrame`
- 전체 native / .NET 회귀 결과

## 10. 결과 요약 기준

- 새 RT ↔ RT timing 테스트가 통과해야 한다.
- native 전체 테스트가 통과해야 한다.
- .NET 전체 테스트가 통과해야 한다.
