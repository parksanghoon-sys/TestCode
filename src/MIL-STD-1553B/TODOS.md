# TODOS

## Current TODOs

- BM replay / report schema v1 구현
  JSONL summary / event line에 `schemaVersion`, `recordType`, `sessionId`, `sequence`를 추가하고, replay reader가 legacy `v0`와 `v1`을 함께 읽도록 `src/dotnet/MilStd1553.Host`와 관련 테스트 / 문서를 갱신한다.

## Deferred Backlog

### `P1_SOON` 실장비용 벤더 SDK `IVendorChannel` binding

- 무엇이 남았는지: 실제 벤더 SDK의 `open / close / transfer / select bus / BM event acquisition`을 `IVendorChannel` concrete 구현 1종으로 매핑한다.
- 왜 defer 되었는지: 현재 저장소에는 실제 카드 / SDK 헤더 / 샘플이 없다.
- objective: 현재 `VendorSdkBusAdapter`와 Host / CLI 경계를 그대로 유지한 채 실장비 경로를 추가한다.
- relevant context: `NativeSessionApi`는 이제 `BusMonitorService`, `SimulatorVendorChannel`, `VendorSdkBusAdapter`, `BusControllerService`를 조립한다. 실제 하드웨어 binding은 `SimulatorVendorChannel` 자리에 들어올 concrete `IVendorChannel`만 바꾸면 된다. callback 기반 SDK는 concrete channel 내부에서 정규화하고, 첫 binding 범위에는 raw BM callback 수집 계약을 넣지 않기로 설계가 닫혔다.
- 관련 범위: `src/native/Infrastructure`, `src/native/Interop`, `tests/native`, `tests/dotnet`, `DECISIONS.md`
- 현재 상태: vendor-neutral 계약과 simulator concrete binding은 검증 완료 상태다.
- known blockers 또는 open questions: 실제 벤더 SDK 함수 시그니처, 초기화 절차, status code 체계가 없다.
- 가장 자연스러운 next step: SDK 헤더 / 샘플 확보 후 `HardwareVendorChannel`과 smoke test를 추가한다.

### `P2_LATER` Mode Code 2차 묶음 확장

- 무엇이 남았는지: 1차 Mode Code 묶음 이후 `Dynamic Bus Control`과 추가 broadcast / terminal-control 계열 Mode Code를 2차 묶음으로 확장한다.
- 왜 defer 되었는지: 1차 묶음이 더 작고 외부 정보 없이 진행 가능하며, 2차 묶음은 운영 제어 성격이 더 강하다.
- objective: 설계에서 정한 tranche 단위를 유지한 채 Mode Code를 점진적으로 넓힌다.
- relevant context: 현재 구현은 `Transmit Status Word`, `Transmit BIT Word`, `Synchronize`, `Synchronize With Data Word`, `Transmit Vector Word`를 지원한다. 다음 즉시 실행 항목은 1차 묶음 4종이며, 이 backlog는 그 이후 2차 묶음에 해당한다.
- 관련 범위: `src/native/Domain`, `src/native/Application`, `tests/native`, `docs/test/*`
- 현재 상태: 대표 5종 지원과 1차 묶음 설계 기준이 고정되었다.
- known blockers 또는 open questions: 2차 묶음 중 broadcast 민감 항목을 simulator에서 어디까지 재현할지 미정이다.
- 가장 자연스러운 next step: 1차 묶음 구현 완료 후 `Dynamic Bus Control`을 2차 첫 항목으로 평가한다.

### `P2_LATER` RT ↔ RT 물리 시퀀스 fidelity 향상

- 무엇이 남았는지: 현재 `ExecuteRtToRtTransfer`의 최소 gap 보정 모델을 `RtToRtSequencePlanner` / `RtToRtTimingPolicy` 기반 command / status 세부 timing, richer trace 모델로 확장한다.
- 왜 defer 되었는지: 현재 MVP 목표는 RT ↔ RT 데이터 흐름과 결과 검증 경계를 고정하는 데 있다.
- objective: BM trace fidelity와 물리 시퀀스 검증이 필요할 때 더 정교한 모델로 발전시킨다.
- relevant context: 지금까지 receive 계열 telemetry payload 보존, RT ↔ RT source / destination 단계 설명, Mode Code RT ↔ RT 거부, 목적지 단계 최소 `40us` gap 보정까지는 반영했다. 설계상 다음 단계는 destination receive command, source transmit command, source status/data, destination status를 명시적으로 다루는 별도 planner / policy 도입이다.
- 관련 범위: `src/native/Application`, `src/native/Infrastructure`, `tests/native`, `docs/design/2026-04-17_전체하네스MVP_설계검토.md`
- 현재 상태: 방향 / word count / receive payload / RT ↔ RT stage description / 최소 gap 검증 테스트는 통과하지만, command / status sequence와 더 깊은 응답 지연 모델은 단순화되어 있다.
- known blockers 또는 open questions: 필요한 BM trace fidelity 수준과 timing 요구가 아직 확정되지 않았다.
- 가장 자연스러운 next step: replay / richer trace 요구가 실제로 필요해지면 planner / policy 타입을 먼저 도입한다.

### `P3_NICE` WPF 운영 Shell

- 무엇이 남았는지: CLI 위에 richer operator workflow가 필요해질 경우 `MilStd1553.Host`와 `MilStd1553.Interop`를 재사용하는 WPF shell을 추가한다.
- 왜 defer 되었는지: 현재 CLI와 mock example이 canonical onboarding 경로로 충분하고, GUI는 운영 요구가 아직 없다.
- objective: CLI를 깨지 않고도 richer dashboard / viewer를 추가할 수 있는 Presentation 확장점을 남긴다.
- relevant context: 설계상 first GUI baseline은 WPF로 고정했고, WinUI는 첫 단계에서 제외했다.
- 관련 범위: `src/dotnet`, `README.md`, `docs/design/*`
- 현재 상태: CLI baseline이 canonical operator entry point다.
- known blockers 또는 open questions: 실제 운영자가 GUI에서 반드시 봐야 하는 view와 command surface가 아직 없다.
- 가장 자연스러운 next step: GUI 요구가 생기면 별도 `MilStd1553.WpfShell` 프로젝트를 추가한다.

## Completed

- [x] 2026-04-17 전체 하네스 MVP 설계 검토 문서를 작성했다.
- [x] 2026-04-17 네이티브 Domain / Application 기본 슬라이스와 테스트 7건을 구축했다.
- [x] 2026-04-17 Host / Interop / telemetry / report export / runtime loader / packaging MVP를 구축했다.
- [x] 2026-04-17 실제 DLL smoke test, `BufferTooSmall`, explicit runtime loader, one-shot packaging, runtime loader cache / dispose 정책을 검증했다.
- [x] 2026-04-17 자동 failover, line fault, 대표 Mode Code 5종, JSONL / CSV / Markdown report, JSONL replay reader를 구축했다.
- [x] 2026-04-18 vendor-neutral adapter 계약(`IVendorChannel`, `VendorSdkBusAdapter`)과 `SimulatorVendorChannel` concrete binding을 구축했다.
- [x] 2026-04-18 `MilStd1553.Cli` baseline 실행 경로를 구축했다.
- [x] 2026-04-18 `NativeSessionApi`를 실제 세션 런타임 facade로 전환하고, 초기 schedule pass의 `MessageFrame` telemetry까지 end-to-end로 검증했다.
- [x] 2026-04-18 `examples/mock-cli` canonical mock example과 self-checking script, smoke test를 추가해 실장비 없이도 CLI example을 재현 가능하게 만들었다.
- [x] 2026-04-18 receive 계열 telemetry payload 보존과 RT ↔ RT 단계 설명 / 검증을 보강했다.
- [x] 2026-04-18 RT ↔ RT 목적지 단계 최소 `40us` gap 보정과 destination time-tag 검증을 추가했다.
- [x] 2026-04-18 남은 확장 설계를 `docs/design/2026-04-18_잔여확장설계_설계검토.md`로 정리하고, 실장비 binding / Mode Code tranche / schema v1 / WPF 방향을 문서와 상태 파일에 반영했다.
- [x] 2026-04-18 1차 Mode Code 묶음 4종(`Reset Remote Terminal`, `Transmit Last Command Word`, `Inhibit Terminal Flag`, `Override Inhibit Terminal Flag`)을 simulator adapter와 네이티브 테스트에 반영했다.
