# 2026-04-18_ModeCode-tranche1_테스트계획

## 1. 테스트 대상

- `src/native/Domain/BusTypes.h`
- `src/native/Infrastructure/SimulatorBusAdapter.h`
- `src/native/Infrastructure/SimulatorBusAdapter.cpp`
- `tests/native/NativeTests.cpp`

## 2. 테스트 시나리오

1. 직전 성공 커맨드가 있을 때 `Transmit Last Command Word`가 이전 커맨드 raw 값을 반환한다.
2. 직전 성공 커맨드가 없을 때 `Transmit Last Command Word`가 `InvalidWord`로 실패한다.
3. `Inhibit Terminal Flag` 이후 `Transmit Status Word` 응답에서 terminal flag 비트가 숨겨진다.
4. `Override Inhibit Terminal Flag` 이후 terminal flag 비트가 다시 노출된다.
5. `Reset Remote Terminal` 이후 RT 상태가 기본값으로 돌아간다.
6. 2차 묶음의 `Dynamic Bus Control`은 계속 `UnsupportedModeCode`로 거부된다.

## 3. 정상 흐름

- 일반 command 수락 후 `Transmit Last Command Word`가 직전 raw command를 돌려준다.
- `Inhibit Terminal Flag`와 `Override Inhibit Terminal Flag`가 연속으로 성공한다.
- `Reset Remote Terminal` 후 `Transmit Status Word`가 기본 상태 워드를 반환한다.

## 4. 비정상 흐름

- 초기 상태에서 `Transmit Last Command Word`는 실패한다.
- 2차 묶음 Mode Code는 여전히 미지원 오류를 반환한다.

## 5. 타임아웃 / 재시도

- 이번 슬라이스는 타임아웃 정책을 바꾸지 않는다.
- 기존 timeout / retry / failover 테스트가 그대로 통과해야 한다.

## 6. Bus A/B 전환

- 이번 슬라이스는 버스 전환 정책을 바꾸지 않는다.
- 기존 line fault / failover 테스트가 그대로 통과해야 한다.

## 7. BC -> RT

- `Reset Remote Terminal`, `Inhibit Terminal Flag`, `Override Inhibit Terminal Flag`는 BC -> RT receive 성격의 Mode Code로 검증한다.

## 8. RT -> BC

- `Transmit Last Command Word`는 RT -> BC transmit 성격의 Mode Code로 검증한다.
- 기존 `Transmit Status Word`, `Transmit BIT Word`, `Transmit Vector Word` 회귀도 유지한다.

## 9. RT <-> RT

- Mode Code RT ↔ RT 거부 규칙은 계속 유지되어야 한다.

## 10. Mode Code

- 대표 5종 회귀
- 1차 묶음 4종 신규 지원
- `Dynamic Bus Control` 미지원 유지

## 11. 로그 확인 포인트

- 실패 시 `ErrorCode::InvalidWord` 또는 `ErrorCode::UnsupportedModeCode`
- 성공 시 상태 워드의 terminal flag 비트 변화
- reset 이후 RT 내부 데이터 초기화 효과

## 12. 결과 요약

- 이번 테스트는 시뮬레이터 RT 상태 모델이 1차 Mode Code 묶음을 수용하는지 검증한다.
- Host / CLI 경계는 변경하지 않고 네이티브 회귀 중심으로 확인한다.
