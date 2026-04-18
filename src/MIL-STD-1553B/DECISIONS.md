# DECISIONS

## 2026-04-17 D-001 시뮬레이터 우선 MVP 채택
- 상태: 승인
- 결정: 첫 구현 사이클은 BC / RT / BM 시뮬레이터, BM 기록, 수동 Bus A/B 전환, timeout / 단일 retry health 추적을 포함한 MVP를 우선 구현한다.
- 이유: 실제 장비와 벤더 SDK 정보가 없는 상태에서 가장 작은 안전한 경로로 설계-구현-테스트 루프를 빠르게 고정할 수 있다.
- 영향: Vendor Adapter 구현은 후순위로 미루고 Domain / Application 규칙과 시뮬레이터 경계를 먼저 고정한다.

## 2026-04-17 D-002 프로토콜 도메인과 성능 민감 로직은 C++ 네이티브가 담당한다
- 상태: 승인
- 결정: word 해석, 메시지 조합, BC / RT / BM 상태, timeout / retry, Bus 전환 상태 모델은 C++ Domain / Application에 둔다.
- 이유: 프로토콜 규칙을 Host에 중복 구현하지 않고, 성능 민감 로직과 장치 근접 로직을 한 경계에 모을 수 있다.
- 영향: C#은 세션 제어, 조회, 리포트, 테스트 오케스트레이션 중심으로 유지한다.

## 2026-04-17 D-003 C# 과 C++ 경계는 세션 중심 C ABI로 제한한다
- 상태: 승인
- 결정: Interop 경계는 raw hardware handle 노출 대신 `OpenSession`, `StopSession`, `SwitchBus`, `GetHealthSnapshot`, `PollTelemetry` 같은 세션 중심 API로 제한한다.
- 이유: Host와 네이티브 사이 결합을 줄이고 UI / CLI / 테스트 오케스트레이션 교체 비용을 낮출 수 있다.
- 영향: C# Interop는 thin client로 유지되고, Host는 DTO 단위 결과만 소비한다.

## 2026-04-17 D-004 초기 네이티브 검증은 MSVC 스크립트 기반으로 시작한다
- 상태: 승인
- 결정: 초기 네이티브 검증 경로는 `tests/native/run_tests.ps1`와 Visual Studio 2022 MSVC 조합으로 시작한다.
- 이유: 현재 환경에서 가장 단순하고 재현 가능한 Red -> Green 검증 경로다.
- 영향: 추후 빌드 시스템 고도화 전까지 스크립트가 canonical native test entry point 역할을 한다.

## 2026-04-17 D-005 Host는 시나리오 JSON을 native 호출 전에 검증한다
- 상태: 승인
- 결정: C# Host는 `ScenarioDefinitionJsonLoader`를 통해 시나리오 JSON을 먼저 검증한 뒤 통과한 경우에만 native 세션 시작을 위임한다.
- 이유: 잘못된 입력을 native까지 전달하면 규칙이 중복되고 사용자 피드백이 늦어진다.
- 영향: Host가 설정 / 시나리오 검증과 세션 오케스트레이션을 담당한다.

## 2026-04-17 D-006 초기 Mode Code 지원은 2종으로 제한한다
- 상태: 승인
- 결정: MVP 초기 Mode Code는 `Transmit Status Word`, `Transmit BIT Word` 2종으로 시작한다.
- 이유: no-data / with-data 경로를 최소 범위에서 검증하면서 구조와 테스트를 고정할 수 있다.
- 영향: 나머지 Mode Code는 `UnsupportedModeCode` 경로로 처리하고 후속 우선순위에 따라 확장한다.

## 2026-04-17 D-007 초기 BM 영구 저장 포맷은 JSONL append로 시작한다
- 상태: 승인
- 결정: BM 영구 저장의 첫 구현은 event 1건당 1줄을 append하는 JSONL 포맷으로 시작한다.
- 이유: append 비용이 낮고 Host telemetry / report DTO와 연결하기 쉽다.
- 영향: replay, CSV / Markdown export, 스키마 버전 관리는 후속 슬라이스로 미룬다.

## 2026-04-17 D-008 초기 Host report export는 JSONL summary 형식으로 시작한다
- 상태: 승인
- 결정: Host report export의 첫 구현은 `SessionExecutionReport`를 입력으로 받아 summary line 1건과 telemetry event line N건을 출력하는 JSONL exporter로 시작한다.
- 이유: BM JSONL 저장과 shape를 맞추기 쉽고 Host 계층만으로도 검증 가능하다.
- 영향: richer presentation model과 CSV / Markdown export는 후속 작업으로 남긴다.

## 2026-04-17 D-009 초기 세션 중심 C ABI payload는 UTF-16 JSON buffer로 시작한다
- 상태: 승인
- 결정: 세션 중심 C ABI의 첫 payload 전달 방식은 caller-allocated UTF-16 버퍼에 JSON 문자열을 채우는 방식으로 시작한다.
- 이유: raw struct marshalling보다 DTO shape 변경에 유연하고 Host / 문서 / 테스트와 맞추기 쉽다.
- 영향: binary marshalling 최적화는 후속 작업으로 미룬다.

## 2026-04-17 D-010 `PollTelemetry`는 성공 시에만 pending 이벤트를 비운다
- 상태: 승인
- 결정: `MilStd1553_PollTelemetry`가 성공적으로 반환될 때만 세션의 pending telemetry 이벤트를 비운다.
- 이유: Host polling 모델에서 중복 소비를 막으면서도 실패 복구가 가능해야 한다.
- 영향: cursor 기반 replay가 필요해지면 별도 cursor 정책이 추가돼야 한다.

## 2026-04-17 D-011 실제 네이티브 smoke test는 runtime layout 경로를 기준으로 검증한다
- 상태: 승인
- 결정: 실제 네이티브 smoke test는 `tests/native/build_native_artifact.ps1 -ArtifactType NativeDll`로 `MilStd1553.Native.dll`을 빌드한 뒤, `runtimes/win-x64/native/MilStd1553.Native.dll` 레이아웃으로 배치해 검증한다.
- 이유: 테스트 경로를 최종 제품 배치 경로와 최대한 같은 shape로 유지해야 로더 정책과 smoke test가 같은 언어를 사용한다.
- 영향: 더 이상 테스트 출력 폴더 루트 복사 전략을 canonical 경로로 취급하지 않는다.

## 2026-04-17 D-012 `BufferTooSmall`는 필요한 버퍼 길이를 함께 반환하고 Host가 재시도한다
- 상태: 승인
- 결정: `OpenSession`, `GetHealthSnapshot`, `PollTelemetry`는 `BufferTooSmall` 반환 시 필요한 UTF-16 버퍼 길이(널 종료 포함)를 out parameter로 함께 돌려주고, `NativeHarnessClient`는 그 길이로 버퍼를 다시 할당해 제한된 횟수 안에서 재시도한다.
- 이유: 기존 계약은 버퍼 부족 사실만 돌려줘 Host가 구조적으로 복구할 수 없었다.
- 영향: `OpenSession`은 모든 출력 버퍼가 충분할 때만 세션을 생성하고, `PollTelemetry`는 `BufferTooSmall` 시 pending telemetry를 유지한다.

## 2026-04-17 D-013 Host 로더는 explicit runtime loader와 예외 분류를 사용한다
- 상태: 승인
- 결정: Host Interop는 `DllImport` 기본 검색 대신 `NativeLibrary.Load` 기반 explicit runtime loader를 사용하고, 로더 실패를 `NativeLibraryLoadException`과 `NativeLibraryFailureKind`로 분류한다.
- 이유: DLL 누락, 잘못된 바이너리, 엔트리 포인트 누락을 운영성 관점에서 구분해 테스트하고 진단하기 위해서다.
- 영향: 테스트는 `MILSTD1553_NATIVE_RUNTIME_ROOT` override를 사용해 failure mode를 재현하고, 후속 packaging 작업은 이 runtime layout 규칙을 따라야 한다.

## 2026-04-17 D-014 `MilStd1553.Interop` build/publish를 one-shot packaging 루트로 사용한다
- 상태: 승인
- 결정: `dotnet build/publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj` 1회 결과를 canonical managed output으로 사용하고, 출력 루트에 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`를 함께 배치한다.
- 이유: 사용자가 managed DLL과 native DLL을 따로 빌드하거나 수동 복사하지 않도록 가장 작은 packaging 경로를 먼저 고정하기 위해서다.
- 영향: 현재 output 계약은 `MilStd1553.Interop` 기준이며, 최종 사용자용 실행 프로젝트가 생기면 packaging 루트 기준을 다시 검토해야 한다.

## 2026-04-17 D-015 루트 `README.md`를 사용 / 빌드 / 테스트의 canonical quick guide로 사용한다
- 상태: 승인
- 결정: 저장소 사용법, one-shot packaging 명령, `.NET` 테스트 명령, 네이티브 테스트 명령, 결과 DLL 위치는 루트 `README.md`에 가장 먼저 정리하고, 하위 README는 세부 범위를 보완하는 구조로 유지한다.
- 이유: 기존 `README_적용가이드.md`는 템플릿 적용 설명에 가깝고, 실제 저장소 사용자에게 필요한 실행 진입점은 별도 quick guide가 더 명확하다.
- 영향: 이후 CLI / UI 실행 프로젝트가 추가되면 루트 `README.md`의 시작 절차와 출력 위치를 먼저 갱신해야 한다.

## 2026-04-17 D-016 explicit runtime loader handle은 경로별 프로세스 cache로 재사용하고 성공 적재 후 즉시 unload 하지 않는다
- 상태: 승인
- 결정: `NativeSessionLibrary`는 resolved path 기준으로 프로세스 공유 cache에서 재사용하고, 같은 경로는 한 번만 적재한다. Host는 성공 적재된 네이티브 DLL을 명시적으로 unload 하지 않으며, 테스트 전용 cache dispose 또는 프로세스 종료에서만 해제를 허용한다.
- 이유: 세션 API delegate가 살아 있는 동안 runtime unload를 허용하면 안전하지 않고, MVP 기준으로는 hot-swap 요구보다 안정적인 재사용 정책이 더 중요하다.
- 영향: explicit runtime loader는 장시간 프로세스 기준의 안정성을 우선하며, 향후 hot-swap 또는 명시적 unload 요구가 생기면 별도 수명 정책과 동기화 규칙을 추가해야 한다.

## 2026-04-17 D-017 자동 failover는 `연속 timeout 2회` 또는 `line fault 1회` 기준으로 standby bus 재전송 1회를 수행한다
- 상태: 승인
- 결정: `BusControllerService`는 같은 활성 버스에서 timeout이 연속 2회 발생하거나 `LineFault`가 1회 발생하면 standby bus로 자동 전환하고, 전환 직후 같은 요청을 1회만 재전송한다.
- 이유: 시뮬레이터 기준으로 가장 작은 자동 복구 경로를 제공하면서도 무한 재시도나 과도한 정책 분기를 피할 수 있다.
- 영향: 자동 failover telemetry event와 health 누적치는 네이티브 기준으로 먼저 관리하고, 운영 환경별 임계치 조정은 후속 범위로 둔다.

## 2026-04-17 D-018 MVP 대표 Mode Code는 5종으로 확장한다
- 상태: 승인
- 결정: MVP 대표 Mode Code는 `Transmit Status Word`, `Transmit BIT Word`, `Synchronize`, `Synchronize With Data Word`, `Transmit Vector Word` 5종으로 확장한다.
- 이유: no-data / with-data, transmit / receive 성격이 다른 대표 경로를 함께 검증해야 이후 확장이 구조적으로 쉬워진다.
- 영향: 그 외 Mode Code는 계속 `UnsupportedModeCode`로 유지하고, 우선순위는 실제 운영 요구가 들어온 뒤 확정한다.

## 2026-04-17 D-019 Host 보고서와 replay는 동일한 telemetry DTO를 공유한다
- 상태: 승인
- 결정: Host는 `SessionExecutionReport`를 JSONL / CSV / Markdown으로 직렬화하고, BM JSONL replay reader는 같은 `TelemetryEventRecord` / `TelemetryMessageFrame` DTO를 사용한다.
- 이유: 포맷별 exporter와 replay reader가 서로 다른 의미 모델을 가지면 검증 비용과 유지보수 비용이 커진다.
- 영향: 편집형 replay나 richer presentation이 추가돼도 기본 telemetry DTO shape는 공통 기준으로 유지한다.

## 2026-04-18 D-020 벤더 SDK 결합점은 `IVendorChannel` 아래로 격리하고 `VendorSdkBusAdapter`가 `IBusAdapter` bridge를 담당한다
- 상태: 승인
- 결정: `Application::IBusAdapter`는 유스케이스가 의존하는 최소 runtime 계약으로 유지하고, 실제 SDK open/close/transfer/select bus/capability는 Infrastructure의 `IVendorChannel`과 `VendorSdkBusAdapter` 아래로 분리한다. 기본 오류 매핑은 `DefaultVendorErrorMapper`가 담당한다.
- 이유: 실제 벤더 SDK 함수명, 핸들 타입, callback, 이벤트 모델은 장비마다 달라도 Application과 Host는 같은 `IBusAdapter` 경계를 유지해야 구조가 안정적이다.
- 영향: 이후 실제 벤더 카드를 붙일 때는 `IVendorChannel` concrete 구현만 추가하면 되고, 현재 테스트된 lazy open / capability cache / status mapping / bus select forwarding 규칙을 공통 bridge에서 재사용한다.

## 2026-04-18 D-021 첫 concrete `IVendorChannel`은 `SimulatorVendorChannel`로 시작한다
- 상태: 승인
- 결정: 실제 벤더 SDK 정보가 없더라도 `IVendorChannel` 계약을 구현 수준으로 검증할 수 있도록, 첫 concrete 구현체는 `SimulatorBusAdapter`를 감싼 `SimulatorVendorChannel`로 시작한다. 이 channel은 open 시 initial bus를 적용하고, `SubmitTransfer`는 현재 selected bus를 권위 값으로 사용한다.
- 이유: 계약만 정의된 상태보다 concrete 구현 1종이 있어야 `VendorSdkBusAdapter`와의 조합, selected bus 의미, line fault 매핑을 구조적으로 검증할 수 있다.
- 영향: 이후 실장비용 binding은 `SimulatorVendorChannel`이 고정한 open/close/transfer/select bus 규칙을 기준 구현으로 삼고, vendor별 차이는 `IVendorChannel` 내부에만 국한한다.

## 2026-04-18 D-022 운영자용 기본 실행 프로젝트는 `MilStd1553.Cli`로 시작한다
- 상태: 승인
- 결정: 저장소의 첫 실행 프로젝트는 `src/dotnet/MilStd1553.Cli`로 두고, 이 프로젝트는 시나리오 파일 입력, 세션 시작/종료, 선택적 Bus 전환, health/telemetry 출력만 담당하는 얇은 CLI baseline으로 유지한다. `MilStd1553.Interop`는 계속 라이브러리 packaging 진입점으로 병행 유지한다.
- 이유: 실제 장비 SDK가 없는 상태에서도 사용자가 end-to-end 흐름을 직접 실행할 수 있는 운영자 진입점이 필요했고, WPF/WinUI보다 CLI가 현재 구조를 가장 적게 흔들면서 검증 가능한 기준선을 만든다.
- 영향: README와 packaging smoke test의 기본 진입점은 `MilStd1553.Cli`로 옮기고, rich UI가 필요해지면 기존 Host/Interop 위에 별도 Presentation 슬라이스로 추가한다.

## 2026-04-18 D-023 `NativeSessionApi`는 실제 runtime session facade로 유지한다
- 상태: 확정
- 결정: 세션 중심 C ABI는 health / pending-only stub state를 직접 관리하지 않는다. `OpenSession`은 시나리오 JSON을 파싱한 뒤 `BusMonitorService`, `SimulatorVendorChannel`, `VendorSdkBusAdapter`, `BusControllerService`를 조립하는 실제 runtime session을 생성한다. 세션 생성 시 초기 BC schedule pass를 1회 수행하고, 첫 번째 `PollTelemetry`는 그 결과 `MessageFrame`을 반환한다. `SwitchBus` 이후 다음 `PollTelemetry`는 `BusSwitch` 이벤트를 반환한다.
- 이유: Host / CLI / runtime layout smoke test가 실제 BC / BM 경로를 타야 설계 의도와 검증이 일치하고, 이후 실장비 `IVendorChannel` binding도 같은 facade 아래에 붙일 수 있다.
- 영향: native / Host end-to-end smoke test는 초기 telemetry 순서를 `MessageFrame -> BusSwitch -> []`로 검증한다. 향후 실장비 binding은 같은 `NativeSessionApi` facade 아래에서 `SimulatorVendorChannel`만 concrete `IVendorChannel`로 교체하면 된다.

## 2026-04-18 D-024 실장비 없는 canonical example 경로는 `examples/mock-cli`로 유지한다
- 상태: 확정
- 결정: 실장비 없이 저장소를 바로 실행해 보는 canonical example 자산은 `examples/mock-cli` 아래에 둔다. example은 시나리오 JSON만 제공하는 것이 아니라 `run_mock_cli_example.ps1`를 통해 CLI publish, 실제 세션 실행, 출력 self-check까지 포함한다.
- 이유: 기존 simulator / CLI 경로를 그대로 재사용하면서도 사용자가 “지금 바로 돌아가는가”를 한 번에 확인할 수 있는 경로가 필요하다.
- 영향: quick guide와 smoke test는 `examples/mock-cli`를 기준으로 안내하고 검증한다. 이후 실장비 binding이 생기면 별도 hardware example 경로를 추가하되 mock example은 계속 onboarding 기준선으로 유지한다.

## 2026-04-18 D-025 receive 계열 `MessageFrame` telemetry는 실제 bus payload를 기록한다
- 상태: 확정
- 결정: `BusControllerService`는 `MessageFrame` telemetry를 만들 때 transmit 계열은 `TransferResult.dataWords`를 사용하고, receive 계열은 `TransferRequest.dataWords`를 사용해 실제 bus payload를 보존한다. RT ↔ RT 경로는 `RT↔RT 소스 RT transmit 단계`, `RT↔RT 목적지 RT receive 단계` description을 남긴다.
- 이유: 기존 구현은 receive 계열 결과가 빈 `dataWords`를 반환하면 telemetry 프레임에서도 payload가 사라져 BC -> RT와 RT ↔ RT 목적지 단계의 trace fidelity가 떨어졌다.
- 영향: BM JSONL, Host telemetry query, replay / report가 receive 계열 payload를 그대로 볼 수 있다. 더 깊은 timing / intermessage gap 모델은 후속 backlog로 남긴다.

## 2026-04-18 D-026 RT ↔ RT destination 단계는 source 완료 이후 최소 `40us` gap으로 정규화한다
- 상태: 확정
- 결정: `BusControllerService`는 RT ↔ RT source 단계가 성공하면 그 완료 time-tag를 기준으로 destination 단계 성공 결과에 최소 `40us` gap을 적용한다. 이 정규화 결과는 `RtToRtTransferResult.destinationTransfer.timeTag`와 telemetry `MessageFrame.timeTag`에 같이 반영한다.
- 이유: 기존 구현은 source 와 destination의 순서만 보장했을 뿐 intermessage gap 의미가 테스트와 문서에 고정돼 있지 않아 trace 해석성이 약했다.
- 영향: 현재 시뮬레이터 baseline은 RT ↔ RT 두 단계를 더 읽기 쉬운 time-tag 차이로 보여준다. 다만 실제 물리 규격에 더 가까운 command / status 세부 timing 모델은 후속 backlog로 남긴다.

## 2026-04-18 D-027 첫 실장비 binding은 `IVendorChannel` 최소 계약을 유지하고 callback 정규화는 concrete channel 내부에서 처리한다
- 상태: 확정
- 결정: 실제 하드웨어 binding은 기존 `IVendorChannel`의 `open / close / submit transfer / select bus / query capabilities` 최소 계약을 유지한다. callback 기반 SDK라도 callback을 `VendorSdkBusAdapter` 위로 올리지 않고, concrete `IVendorChannel` 내부에서 bounded queue 또는 wait primitive로 `VendorTransferResult`로 정규화한다.
- 이유: Application / Interop / Host가 SDK별 callback 모델과 스레드 모델을 직접 알게 되면 현재 vendor-neutral 구조가 다시 흔들린다.
- 영향: 첫 실장비 binding은 `NativeSessionApi -> VendorSdkBusAdapter -> HardwareVendorChannel` 경로로 붙고, concrete channel이 벤더 handle 수명주기와 callback 정규화를 모두 소유한다.

## 2026-04-18 D-028 첫 실장비 binding 범위에는 raw BM callback 계약을 넣지 않는다
- 상태: 확정
- 결정: 첫 실장비 binding은 transfer / bus select / capability 중심으로 닫고, raw BM frame push/pull 계약은 `IVendorChannel`에 넣지 않는다. 필요 시 이는 별도 optional Infrastructure 계약으로 추가한다.
- 이유: 현재 BM 기준선은 `BusControllerService`가 생성한 telemetry를 `BusMonitorService`가 기록하는 구조이고, 이를 유지해야 첫 실장비 binding이 과도하게 커지지 않는다.
- 영향: 첫 하드웨어 integration은 현재 mock / simulator / Host telemetry 모델을 재사용한다. raw BM tap은 후속 요구가 생겼을 때 별도 설계로 다룬다.

## 2026-04-18 D-029 Mode Code 확장은 tranche 단위로 진행하고 다음 1차 묶음은 4종으로 고정한다
- 상태: 확정
- 결정: Mode Code는 개별 요청 단위가 아니라 tranche 단위로 확장한다. 외부 정보 없이 바로 진행할 1차 묶음은 `Reset Remote Terminal`, `Transmit Last Command Word`, `Inhibit Terminal Flag`, `Override Inhibit Terminal Flag` 4종으로 고정한다. `Dynamic Bus Control`과 추가 broadcast / terminal-control 계열은 2차 묶음으로 미룬다.
- 이유: 우선순위 없이 Mode Code를 추가하면 테스트와 문서가 다시 흩어지고, broadcast 성격이 강한 항목은 별도 검토가 필요하다.
- 영향: 다음 내부 구현 순서는 1차 묶음 4종이며, 정의된 tranche 밖 Mode Code는 계속 `UnsupportedModeCode`로 남는다.

## 2026-04-18 D-030 replay / report는 JSONL canonical source와 schema v1 envelope를 기준으로 확장한다
- 상태: 확정
- 결정: JSONL은 canonical persisted source로 유지하고, CSV / Markdown은 projection output으로 유지한다. 다음 확장 시 JSONL summary / event line에는 `schemaVersion`, `recordType`, `sessionId`, `sequence`를 포함하는 schema v1 envelope를 도입하고, replay reader는 `schemaVersion`이 없는 기존 line을 legacy v0로 계속 읽는다.
- 이유: 로그가 누적된 뒤 versioning을 도입하면 replay 호환성과 exporter 유지 비용이 커진다.
- 영향: 후속 Host 구현은 exporter / replay reader 레이어에서만 versioning 분기를 처리하고, telemetry DTO의 의미 모델은 유지한다.

## 2026-04-18 D-031 canonical operator entry는 CLI로 유지하고 첫 GUI baseline이 필요하면 `WPF`를 사용한다
- 상태: 확정
- 결정: 운영자용 canonical entry point는 계속 `MilStd1553.Cli`로 유지한다. richer UI 요구가 생기면 첫 GUI baseline은 `WPF`로 시작하고, `WinUI`는 첫 단계에서 사용하지 않는다.
- 이유: 현재 CLI + mock example 경로가 가장 작은 onboarding / 검증 기준선이고, Windows 운영 도구 관점에서는 WPF가 현재 구조와 tooling에 더 안정적으로 맞는다.
- 영향: README, packaging, smoke path의 기준은 계속 CLI에 둔다. GUI는 `MilStd1553.Host`와 `MilStd1553.Interop`를 재사용하는 별도 Presentation 슬라이스로만 추가한다.

## 2026-04-18 D-032 RT ↔ RT deeper physical fidelity는 전용 planner / policy로 분리한다
- 상태: 확정
- 결정: 현재의 최소 `40us` gap baseline 이후 richer RT ↔ RT physical sequence는 `BusControllerService`에 조건문을 누적하지 않고, `RtToRtSequencePlanner`와 `RtToRtTimingPolicy` 같은 전용 타입으로 분리한다. 이 모델은 destination receive command, source transmit command, source status/data, destination status 단계를 명시적으로 다룬다.
- 이유: 현재 서비스에 물리 시퀀스 로직을 계속 누적하면 orchestration 책임과 timing 모델 책임이 다시 섞인다.
- 영향: 후속 RT ↔ RT fidelity 구현은 planner / policy를 먼저 도입하고, Host DTO는 가능하면 유지한 채 richer trace를 description / metadata로 확장한다.

## 2026-04-18 D-033 1차 Mode Code 묶음은 simulator RT 상태의 `effective status + last accepted command` 모델로 처리한다
- 상태: 확정
- 결정: `Transmit Last Command Word`는 현재 요청 직전의 마지막 성공 커맨드 raw 값을 반환하고, `Inhibit Terminal Flag` / `Override Inhibit Terminal Flag`는 저장된 상태 워드를 직접 덮어쓰지 않고 노출용 `effective status`에서 terminal flag 비트만 숨기거나 복원한다. `Reset Remote Terminal`은 RT 상태를 기본값으로 초기화한 뒤, 성공 반환 후 현재 reset command를 마지막 성공 커맨드로 기록한다.
- 이유: 단순 비트 토글이나 즉시 자기 자신을 반환하는 구현은 Mode Code 의미를 흐리고 이후 실장비 binding과의 대응 관계를 약하게 만든다.
- 영향: simulator는 1차 Mode Code 4종을 지원하면서도 기존 status 구성값과 이후 확장 지점을 보존한다. `Dynamic Bus Control`을 포함한 2차 묶음은 계속 `UnsupportedModeCode`로 남는다.
