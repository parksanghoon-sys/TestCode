# TODOS

## Current TODOs

- [ ] `P2_LATER` explicit runtime loader가 적재한 native library handle의 cache / dispose 정책을 문서와 테스트로 정리한다.

## Deferred Backlog

### `P1_SOON` 벤더 SDK 어댑터 계약 구체화
- 남은 작업: 실제 장치 벤더 SDK의 초기화, 채널 열기, 송수신, 이벤트 수집 흐름을 조사하고 `IBusAdapter` 계약을 구체화한다.
- 왜 defer 되었는지: 현재 저장소에는 실제 장비와 SDK 정보가 없고, MVP는 시뮬레이터 우선 경로로 충분히 진행 가능하다.
- objective: 이후 Vendor Adapter를 무리 없이 붙일 수 있는 안정적인 네이티브 계약을 확보한다.
- relevant context: 2026-04-17 설계 문서에서 `IBusAdapter`를 추상 경계로 두고 벤더 종속 구현은 후순위로 미뤘다.
- 관련 범위: `src/native/Infrastructure`, `src/native/Interop`, `DECISIONS.md`
- 현재 상태: 추상화 방향만 정의됐고 벤더별 초기화 시퀀스와 오류 코드 매핑은 미정이다.
- known blockers: 어떤 벤더 카드를 사용할지, .NET SDK 제공 여부, DMA / interrupt 제약이 있는지 아직 모른다.
- next step: 장비 정보가 확보되면 SDK 샘플을 분석하고 `IBusAdapter` 메서드 시그니처와 오류 매핑을 구체화한다.

### `P2_LATER` 자동 failover 정책 구체화
- 남은 작업: timeout, retry, line fault를 기준으로 자동 Bus A/B 전환과 복구 정책을 정의한다.
- 왜 defer 되었는지: MVP는 수동 전환과 health 모델만으로도 주요 경계를 검증할 수 있다.
- objective: 이후 자동 failover를 넣어도 Host와 네이티브 책임 경계가 흐트러지지 않도록 정책 객체와 이벤트 모델을 준비한다.
- relevant context: 현재 `HealthSnapshot`은 active bus, timeout, retry를 담고 있고 수동 전환까지만 구현됐다.
- 관련 범위: `src/native/Application`, `tests/native`, `docs/design/2026-04-17_전체하네스MVP_설계검토.md`
- 현재 상태: 자동 전환의 기준값, cooldown, 운영자 override 정책은 정해지지 않았다.
- known blockers: 복구 시점, recovery 측정 기준, 운영자 개입 우선순위가 미정이다.
- next step: 수동 전환과 BM 로그가 안정화된 뒤 자동 failover 설계 문서를 별도로 추가한다.

### `P2_LATER` Mode Code 우선순위 확장
- 남은 작업: MVP 이후 실제로 지원할 Mode Code 목록과 no-data / with-data 처리 규칙을 확정한다.
- 왜 defer 되었는지: 현재는 대표 2종만으로 구조와 테스트 흐름을 고정하는 것이 더 중요하다.
- objective: 이후 Mode Code 확장 시 테스트와 문서가 체계적으로 늘어나도록 우선순위를 정한다.
- relevant context: 현재 구현은 `Transmit Status Word`, `Transmit BIT Word`만 포함하고 나머지는 `UnsupportedModeCode`로 처리한다.
- 관련 범위: `src/native/Domain`, `tests/native`, `docs/test/*`
- 현재 상태: 구조와 테스트 템플릿은 준비됐지만 전체 목록과 결과 비트 매핑은 미정이다.
- known blockers: 실제 운영 시나리오에서 어떤 Mode Code가 중요한지 정보가 없다.
- next step: 시뮬레이터 기본 경로가 안정화된 뒤 우선순위가 높은 Mode Code 1~2개씩 추가한다.

### `P2_LATER` RT 대 RT 물리 시퀀스 정합성 향상
- 남은 작업: 현재 `ExecuteRtToRtTransfer`가 논리 데이터 전달 중심으로 동작하는 부분을 실제 1553 명령 / 응답 시퀀스에 가깝게 모델링한다.
- 왜 defer 되었는지: 현재 MVP 목표는 RT 대 RT 데이터 흐름과 결과 검증 경계를 확보하는 데 있다.
- objective: BM trace fidelity와 물리 시퀀스 검증이 필요해질 때 더 정확한 시퀀스로 발전시킨다.
- relevant context: 현재 구현은 source RT transmit 결과를 받은 뒤 destination RT receive를 수행하는 단순 모델이다.
- 관련 범위: `src/native/Application`, `src/native/Infrastructure`, `tests/native`, `docs/design/2026-04-17_전체하네스MVP_설계검토.md`
- 현재 상태: 논리 흐름과 결과 검증은 통과하지만 실제 버스 시퀀스 모델은 단순하다.
- known blockers: intermessage gap, 응답 타이밍, BM trace fidelity 요구 수준이 아직 정해지지 않았다.
- next step: BM replay 요구가 구체화되면 RT 대 RT 시퀀스를 별도 슬라이스로 설계한다.

### `P2_LATER` 네이티브 라이브러리 handle cache / dispose 정책 정리
- 남은 작업: explicit runtime loader가 적재한 네이티브 라이브러리 handle을 재사용할지, 해제할지, hot-swap을 허용할지 정책을 정한다.
- 왜 defer 되었는지: 현재 슬라이스는 로더 경로와 packaging 출력을 고정하는 것이 우선이었고, 라이브러리 수명 정책은 운영 요구가 더 필요하다.
- objective: 이후 publish, hot-swap, 장비 교체, 장시간 운영 시 handle 관리가 예측 가능하도록 한다.
- relevant context: 현재 `NativeSessionLibrary`는 경로별 적재와 엔트리 포인트 해석에 집중하고 있고, `MilStd1553.Interop.csproj`는 build/publish 1회로 managed DLL 2종과 native DLL 1종을 함께 출력한다.
- 관련 범위: `src/dotnet/MilStd1553.Interop/NativeMethods`, `tests/dotnet`, `DECISIONS.md`
- 현재 상태: 테스트는 통과하지만 라이브러리 재적재와 명시적 unload 정책은 없다.
- known blockers: 최종 Host 프로세스 수명, hot-reload 필요성, 장비 재초기화 정책이 미정이다.
- next step: handle lifecycle 정책을 설계 문서와 테스트로 먼저 고정한 뒤 필요하면 구현을 조정한다.

### `P3_NICE` BM replay 및 리포트 형식 표준화
- 남은 작업: BM 로그 저장 형식, replay 입력 형식, JSON / CSV / Markdown 리포트 출력을 표준화한다.
- 왜 defer 되었는지: 현재는 event append, telemetry query, JSONL export만으로도 MVP 검증이 가능하다.
- objective: 이후 로그 분석과 리포트 자동화를 붙일 수 있는 공통 조회 / 출력 형식을 마련한다.
- relevant context: BM event stream과 영구 저장은 분리돼 있고, report export는 Host 책임으로 남아 있다.
- 관련 범위: `src/native/Infrastructure`, `src/dotnet/MilStd1553.Host`, `docs/test/*`
- 현재 상태: JSONL append와 summary report까지만 구현됐다.
- known blockers: 보고 대상, 조회 포맷, replay 우선인지 보고서 우선인지가 정해지지 않았다.
- next step: 실제 로그 샘플이 모이면 JSONL 유지 여부와 CSV / Markdown 확장 방향을 결정한다.

## Completed

- [x] 2026-04-17 전체 하네스 MVP 설계 검토 문서를 `docs/design/2026-04-17_전체하네스MVP_설계검토.md`로 작성했다.
- [x] 2026-04-17 연속 작업용 상태 문서 `AGENT_RULES.md`, `CURRENT_PLAN.md`, `CHANGELOG_AGENT.md`, `DECISIONS.md`를 초기화했다.
- [x] 2026-04-17 `src/native/Domain`, `src/native/Application`, `tests/native`에 네이티브 기초 슬라이스를 추가했다.
- [x] 2026-04-17 `tests/native/run_tests.ps1` 기반 네이티브 테스트 경로를 만들고 7건을 통과시켰다.
- [x] 2026-04-17 `src/dotnet/MilStd1553.Host`, `src/dotnet/MilStd1553.Interop`, `tests/dotnet/MilStd1553.Host.Tests`를 추가했다.
- [x] 2026-04-17 Host / Interop 테스트 경로를 만들고 6건을 통과시켰다.
- [x] 2026-04-17 `src/native/Infrastructure/SimulatorBusAdapter`와 `BusControllerService::ExecuteRtToRtTransfer`를 추가했다.
- [x] 2026-04-17 네이티브 메시지 확장 슬라이스로 `RT -> BC`, `RT <-> RT`, 대표 Mode Code 2종을 추가하고 11건을 통과시켰다.
- [x] 2026-04-17 BM JSONL 저장, Host telemetry query, JSONL report export를 추가하고 네이티브 12건 / .NET 10건을 통과시켰다.
- [x] 2026-04-17 세션 중심 C ABI와 `NativeHarnessClient` JSON marshalling 초안을 추가하고 네이티브 13건 / .NET 13건을 통과시켰다.
- [x] 2026-04-17 실제 네이티브 DLL smoke test를 만들고 .NET 14건을 통과시켰다.
- [x] 2026-04-17 `requiredCapacity` out parameter와 caller buffer 재시도 정책을 구현해 네이티브 15건 / .NET 17건을 통과시켰다.
- [x] 2026-04-17 explicit runtime layout 로더, `NativeLibraryLoadException`, placeholder DLL 빌드 경로를 추가하고 네이티브 15건 / .NET 20건을 통과시켰다.
- [x] 2026-04-17 `MilStd1553.Interop.csproj`에 one-shot packaging target을 추가해 `dotnet build/publish` 1회 output에 managed DLL 2종과 native DLL 1종이 함께 나오도록 만들고 네이티브 15건 / .NET 22건을 통과시켰다.
- [x] 2026-04-17 루트 `README.md` 기반 사용 / 빌드 / publish / 테스트 퀵 가이드를 추가하고 문서에 적은 명령을 다시 실행해 검증했다.
