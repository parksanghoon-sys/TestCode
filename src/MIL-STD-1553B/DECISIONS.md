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
