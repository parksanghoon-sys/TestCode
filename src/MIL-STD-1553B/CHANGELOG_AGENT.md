# CHANGELOG_AGENT

## 2026-04-17

### 설계

- `docs/design/2026-04-17_전체하네스MVP_설계검토.md`를 작성하고, 시뮬레이터 우선 MVP 범위, BM JSONL 저장, Host telemetry / report export, 세션 중심 C ABI, `BufferTooSmall` 재시도 정책을 정리했다.
- explicit runtime loader 슬라이스에서 제품 기준 DLL 배치 경로를 `runtimes/win-x64/native/MilStd1553.Native.dll`로 고정하고, `MILSTD1553_NATIVE_RUNTIME_ROOT` 기반 테스트 override를 설계 문서에 반영했다.
- packaging 슬라이스에서 `MilStd1553.Interop`를 managed output 루트로 고정하고, `dotnet build/publish` 1회 결과로 managed DLL 2종과 native DLL 1종이 함께 생성되도록 설계했다.
- 루트 `README.md`를 canonical quick guide로 추가하고, 사전 준비 / one-shot packaging / .NET test / native test 명령을 공식 진입 문서로 정리했다.
- runtime loader handle lifecycle 슬라이스에서 resolved path 기준 프로세스 공유 cache와 명시적 unload 비사용 정책을 설계 문서에 반영했다.
- 자동 failover / Mode Code / replay 슬라이스에서 `연속 timeout 2회`, `line fault 1회` 기준 자동 failover, 대표 Mode Code 5종, JSONL / CSV / Markdown export, BM JSONL replay 범위를 설계 문서와 테스트 계획에 반영했다.

### 구현

- `src/native/Domain`에 `CommandWord`, `StatusWord`, `TransferTypes`, Mode Code 관련 타입을 추가했다.
- `src/native/Application`에 `BusMonitorService`, `BusControllerService`를 추가하고 timeout / retry, 수동 Bus A/B 전환, `RT -> BC`, `RT <-> RT`, 대표 Mode Code 경로를 구현했다.
- `src/native/Infrastructure`에 `SimulatorBusAdapter`, `JsonLinesBusEventStore`를 추가했다.
- `src/native/Interop`에 `NativeSessionApi`를 추가하고 `requiredCapacity` out parameter와 `BufferTooSmall` 처리까지 확장했다.
- `tests/native/build_native_artifact.ps1`를 정리하고 `PlaceholderDll` 빌드 경로를 추가했다.
- `src/dotnet/MilStd1553.Host`에 시나리오 로더, 세션 코디네이터, telemetry query, JSONL / CSV / Markdown report export, JSONL replay reader를 추가했다.
- `src/dotnet/MilStd1553.Interop`에 `NativeHarnessClient`, `NativeLibraryPathResolver`, `NativeSessionLibrary`, `NativeLibraryLoadException`, `NativeLibraryFailureKind`를 추가해 explicit runtime loader와 실패 분류를 구현했다.
- `src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`에 MSBuild packaging target을 추가해 build/publish 시 native DLL이 runtime layout 경로로 자동 복사되도록 구현했다.
- `src/dotnet/MilStd1553.Interop/NativeMethods`에 `INativeLibraryPlatform`, `RuntimeNativeLibraryPlatform`, `NativeSessionLibraryCache`를 추가하고 `PInvokeNativeSessionApi`가 경로별 공유 cache를 사용하도록 정리했다.
- `src/native/Application`에 `AutomaticFailoverPolicy`를 추가하고 `BusControllerService`가 연속 timeout / line fault 기반 자동 failover와 RT 대 RT 유효성 검증을 수행하도록 확장했다.
- `src/native/Infrastructure/SimulatorBusAdapter`에 line fault, vector word, synchronize mode code 상태를 추가하고 대표 Mode Code 5종을 처리하도록 확장했다.

### 테스트 및 문서

- `docs/test/2026-04-17_전체하네스MVP_테스트계획.md`, `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`를 갱신했다.
- `docs/test/2026-04-17_HostInterop_테스트계획.md`, `docs/test/2026-04-17_HostInterop_테스트결과.md`를 runtime layout loader + packaging 기준으로 갱신했다.
- `docs/test/README.md`, `tests/dotnet/README.md`, `tests/native/README.md`에 실제 실행 명령과 확인 포인트를 추가했다.
- `tests/native/run_tests.ps1` 실행으로 네이티브 테스트 21건이 모두 통과했다.
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행으로 .NET 테스트 27건이 모두 통과했다.
- `dotnet build src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`와 `dotnet publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o artifacts/publish`를 다시 실행해 quick guide 명령이 실제 상태와 일치함을 확인했다.

## 2026-04-18

### 설계

- vendor adapter 슬라이스에서 `Application::IBusAdapter`는 최소 runtime 계약으로 유지하고, `IVendorChannel`, `IVendorErrorMapper`, `VendorSdkBusAdapter`로 실제 SDK 결합점을 Infrastructure 아래로 격리하는 방향을 설계 문서와 테스트 계획에 반영했다.

### 구현

- `src/native/Infrastructure`에 `VendorAdapterContracts`, `VendorSdkBusAdapter`, `DefaultVendorErrorMapper`를 추가하고, lazy open / capability cache / vendor status mapping / open 전후 bus select 전달을 구현했다.
- `tests/native/build_native_artifact.ps1`에 `VendorSdkBusAdapter.cpp`를 포함시켜 네이티브 테스트와 native DLL 빌드가 같은 소스 세트를 사용하도록 맞췄다.

### 테스트 및 문서

- `tests/native/NativeTests.cpp`에 `FakeVendorChannel` 기반 테스트 3건을 추가해 `VendorSdkBusAdapter`의 lazy open, status mapping, bus select 전달을 검증했다.
- `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`를 네이티브 24건 / Host 27건 기준으로 갱신하고 vendor-neutral adapter 범위를 반영했다.
- `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`, `tests/native/README.md`를 현재 기준으로 업데이트했다.

### 검증

- `tests/native/run_tests.ps1` 실행으로 네이티브 테스트 24건이 모두 통과했다.
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행으로 .NET 테스트 27건이 모두 통과했다.

## 2026-04-18 (추가)

### 설계

- `SimulatorBusAdapter` 위에 첫 concrete `IVendorChannel` 구현체를 얹는 `SimulatorVendorChannel` 슬라이스를 설계 문서와 테스트 계획에 반영했다.
- `SimulatorVendorChannel`의 selected bus를 channel 상태의 권위 값으로 두고, `SubmitTransfer`는 현재 selected bus를 기준으로 동작하도록 규칙을 고정했다.

### 구현

- `src/native/Infrastructure`에 `SimulatorVendorChannel`을 추가하고, open / close / transfer / select bus / simulator pass-through 구성 메서드를 구현했다.
- `SimulatorVendorChannel`은 open 전 transfer / select bus를 `ChannelNotOpen`으로 거부하고, open 시 initial bus를 적용하며, 실제 전송은 현재 selected bus로 정규화하도록 만들었다.
- `tests/native/build_native_artifact.ps1`에 `SimulatorVendorChannel.cpp`를 추가해 테스트 실행 파일과 native DLL 빌드 모두 같은 구현을 포함하도록 맞췄다.

### 테스트 및 문서

- `tests/native/NativeTests.cpp`에 `SimulatorVendorChannel` open 전 호출 거부, initial/select bus 반영, `VendorSdkBusAdapter`와의 concrete binding 성공 경로 테스트 3건을 추가했다.
- `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`를 네이티브 27건 / Host 27건 기준으로 갱신했다.
- `CURRENT_PLAN.md`, `TODOS.md`, `tests/native/README.md`를 현재 기준으로 업데이트했다.

### 검증

- `tests/native/run_tests.ps1` 실행으로 네이티브 테스트 27건이 모두 통과했다.
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행으로 .NET 테스트 27건이 모두 통과했다.

## 2026-04-18 (CLI baseline)

### 설계

- 운영자용 기본 실행 프로젝트를 `MilStd1553.Cli`로 두고, 시나리오 파일 입력, 세션 시작/종료, 선택적 Bus 전환, telemetry 출력까지만 담당하는 얇은 CLI baseline을 설계 문서와 Host/Interop 테스트 계획에 반영했다.
- packaging 기준은 `MilStd1553.Cli`를 기본 실행 루트로, `MilStd1553.Interop`를 라이브러리 packaging 루트로 병행 유지하는 방향으로 정리했다.

### 구현

- `src/dotnet/MilStd1553.Cli`에 `MilStd1553.Cli.csproj`, `HarnessCliRunner`, `ScenarioFileReader`, `Program.cs`를 추가하고 Host / Interop 서비스를 조합해 CLI 실행 경로를 구현했다.
- CLI 프로젝트에도 native runtime copy target을 추가해 build/publish 시 `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 출력되도록 맞췄다.

### 테스트 및 문서

- `tests/dotnet/MilStd1553.Host.Tests`에 `HarnessCliRunnerTests`, `CliPackagingTests`를 추가해 CLI 인자 해석, 세션 lifecycle orchestration, CLI build/publish packaging 출력을 검증했다.
- `README.md`, `src/dotnet/README.md`, `docs/test/2026-04-17_HostInterop_테스트결과.md`, `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`를 현재 기준으로 갱신했다.

### 검증

- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행으로 .NET 테스트 32건이 모두 통과했다.
- `tests/native/run_tests.ps1` 실행으로 네이티브 테스트 27건이 모두 통과했다.
- `dotnet run --project src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj -- --scenario <temp> --switch-bus B --poll-telemetry` smoke test가 성공했다.

## 2026-04-18 (runtime-backed C ABI)

### 설계

- 세션 중심 C ABI가 detached stub state에 머물지 않고 실제 네이티브 runtime session facade를 조립해야 한다는 내용을 설계 문서와 테스트 계획에 반영했다.
- `OpenSession` 시 초기 BC schedule pass를 1회 수행하고, 첫 번째 `PollTelemetry`가 그 결과 `MessageFrame`을 반환한다는 계약을 Host / native smoke test 기준선으로 고정했다.

### 구현

- `src/native/Interop/NativeSessionApi.cpp`에 시나리오 JSON 파서와 `NativeSessionRuntime` 조립 경로를 추가했다.
- `NativeSessionRuntime`는 `BusMonitorService`, `SimulatorVendorChannel`, `VendorSdkBusAdapter`, `BusControllerService`를 조합하고, 세션 생성 시 초기 schedule pass를 수행하도록 구현했다.
- `PollTelemetry`는 실제 monitor event cursor를 기준으로 pending telemetry를 반환하고, 직렬화 성공 이후에만 cursor를 전진시키도록 유지했다.
- `tests/dotnet/MilStd1553.Host.Tests/NativeSessionLibraryCacheTests.cs`는 실제 네이티브 DLL을 다시 빌드하도록 바꿔 placeholder DLL에 의한 테스트 순서 의존을 제거했다.

### 테스트 및 문서

- `tests/native/NativeTests.cpp`에서 C ABI lifecycle 테스트를 강화해 첫 번째 telemetry가 `MessageFrame`, 두 번째 telemetry가 `BusSwitch`인지 확인하도록 바꿨다.
- `tests/dotnet/MilStd1553.Host.Tests/NativeHarnessClientEndToEndTests.cs`에서 실제 runtime layout DLL smoke test를 같은 기준으로 강화했다.
- `docs/test/2026-04-17_HostInterop_테스트결과.md`, `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`, `CURRENT_PLAN.md`, `TODOS.md`를 현재 truth에 맞게 갱신했다.

### 검증

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 실행 결과 네이티브 27건 통과
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행 결과 .NET 32건 통과
- 재검증 중 VS Code `csdevkit` build host가 기본 `obj/bin`을 잠그는 경우가 있었고, build host 종료와 `dotnet build-server shutdown` 이후 표준 명령이 정상 통과했다.

## 2026-04-18 (mock CLI example)

### 설계

- 실장비 없이 현재 simulator / mock 경로를 바로 실행해 볼 수 있는 canonical example을 `examples/mock-cli`로 두기로 정했다.
- example은 단순 샘플 파일이 아니라 publish, 실제 CLI 실행, 출력 self-check까지 포함하는 self-checking script 경로로 설계했다.

### 구현

- `examples/mock-cli/mock.scenario.json`, `examples/mock-cli/run_mock_cli_example.ps1`, `examples/mock-cli/README.md`를 추가했다.
- script는 CLI를 publish한 뒤 example scenario로 실제 세션을 실행하고, `MessageFrame`, `BusSwitch`, `sessionStopped=true`까지 검증한다.
- `tests/dotnet/MilStd1553.Host.Tests/MockCliExampleSmokeTests.cs`를 추가해 example script를 자동 smoke test로 실행한다.
- `README.md`, `src/dotnet/README.md`, `tests/dotnet/README.md`를 mock example 기준으로 갱신했다.

### 테스트 및 문서

- `docs/design/2026-04-18_mock-cli-example_설계검토.md`
- `docs/test/2026-04-18_mock-cli-example_테스트계획.md`
- `docs/test/2026-04-18_mock-cli-example_테스트결과.md`
- `docs/test/2026-04-17_HostInterop_테스트결과.md`
- `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`
- `CURRENT_PLAN.md`, `TODOS.md`를 현재 truth로 갱신했다.

### 검증

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\examples\mock-cli\run_mock_cli_example.ps1` 실행 성공
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --filter "FullyQualifiedName~MockCliExampleSmokeTests"` 실행 결과 1건 통과
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행 결과 .NET 33건 통과
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 실행 결과 네이티브 27건 통과

## 2026-04-18 (RT↔RT fidelity)

### 설계

- receive 계열 `MessageFrame` telemetry는 `TransferResult.dataWords`가 아니라 실제 bus payload를 사용해야 한다는 점과, RT ↔ RT는 source / destination 단계를 구분해 기록해야 한다는 점을 집중 설계 문서와 테스트 계획에 반영했다.
- timing / intermessage gap까지의 고급 모델은 이번 범위에서 제외하고 deferred backlog로 남겼다.

### 구현

- `src/native/Application/BusControllerService`에 `ExecuteCore`, `ResolveFrameDataWords`, RT ↔ RT stage description 적용을 추가했다.
- `src/native/Domain/TransferTypes`의 `CreateMessageEvent`가 선택적 description을 받을 수 있게 확장했다.
- RT ↔ RT는 `RT↔RT 소스 RT transmit 단계`, `RT↔RT 목적지 RT receive 단계` description을 남기고, Mode Code command는 RT ↔ RT 요청으로 거부하도록 보강했다.
- `tests/dotnet/MilStd1553.Host.Tests/NativeHarnessClientEndToEndTests.cs`에 receive scenario smoke test를 추가해 Host DTO까지 payload가 유지되는지 확인하도록 확장했다.

### 테스트 및 문서

- `tests/native/NativeTests.cpp`에 receive payload 보존, RT ↔ RT 단계 telemetry, Mode Code RT ↔ RT 거부 테스트를 추가했다.
- `docs/design/2026-04-18_RTtoRT-fidelity_설계검토.md`, `docs/test/2026-04-18_RTtoRT-fidelity_테스트계획.md`, `docs/test/2026-04-18_RTtoRT-fidelity_테스트결과.md`를 추가했다.
- `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`, `docs/test/2026-04-17_HostInterop_테스트결과.md`, `docs/design/2026-04-17_전체하네스MVP_설계검토.md`, `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`를 최신 상태로 갱신했다.

### 검증

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 실행 결과 네이티브 29건 통과
- `dotnet build-server shutdown` 이후 `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행 결과 .NET 34건 통과

## 2026-04-18 (RT↔RT timing fidelity)

### 설계

- RT ↔ RT payload / stage visibility 다음 단계로, source 와 destination 사이 최소 intermessage gap을 시뮬레이터 trace 규칙으로 고정했다.
- 이번 범위는 실제 물리 규격 정밀 모사가 아니라 result / telemetry가 같은 time-tag 기준을 갖도록 만드는 최소 timing baseline으로 제한했다.

### 구현

- `src/native/Application/BusControllerService`의 성공 경로가 최종 time-tag를 한 곳에서 정규화하도록 바꿨다.
- RT ↔ RT destination 단계는 source 단계 완료 시각 이후 최소 `40us` gap을 적용해 `destinationTransfer.timeTag`와 telemetry `MessageFrame.timeTag`를 같은 값으로 맞춘다.

### 테스트 및 문서

- `docs/design/2026-04-18_RTtoRT-timing-fidelity_설계검토.md`
- `docs/test/2026-04-18_RTtoRT-timing-fidelity_테스트계획.md`
- `docs/test/2026-04-18_RTtoRT-timing-fidelity_테스트결과.md`
- `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`를 최신 상태로 갱신했다.

### 검증

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 실행 결과 네이티브 29건 통과
- `dotnet build-server shutdown`
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests" -v normal` 실행 결과 영향 범위 .NET 5건 통과

## 2026-04-18 (잔여 확장 설계 마무리)

### 설계

- 실장비 `IVendorChannel` binding, RT ↔ RT deeper physical timing, Mode Code tranche, replay / report schema versioning, rich UI 방향을 하나의 설계 기준선으로 정리했다.
- 첫 실장비 binding은 기존 `IVendorChannel` 최소 계약을 유지하고 callback 정규화는 concrete channel 내부에서 처리하기로 확정했다.
- 첫 실장비 binding 범위에는 raw BM callback 계약을 넣지 않고, 필요 시 별도 optional Infrastructure 계약으로 분리하기로 확정했다.
- 다음 내부 구현 순서는 1차 Mode Code 묶음 4종 -> JSONL schema v1 -> RT ↔ RT planner / policy 순으로 정리했다.
- canonical operator entry는 계속 CLI로 유지하고, GUI가 필요하면 첫 baseline은 WPF로 시작하기로 확정했다.

### 테스트 및 문서

- `docs/design/2026-04-18_잔여확장설계_설계검토.md`
- `docs/test/2026-04-18_잔여확장설계_테스트계획.md`
- `docs/test/2026-04-18_잔여확장설계_테스트결과.md`
- `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`를 최신 상태로 갱신했다.

### 검증

- 코드 변경이 없는 설계 정리 사이클이라 build / test 명령은 추가 실행하지 않았다.
- 설계 문서, 상태 문서, 의사결정 기록 간 정합성만 검토했다.

## 2026-04-18 (Mode Code tranche 1)

### 설계

- `docs/design/2026-04-18_ModeCode-tranche1_설계검토.md`로 1차 Mode Code 묶음 4종의 simulator semantics를 고정했다.
- `Transmit Last Command Word`는 직전 성공 커맨드 raw 값을 반환하고, `Inhibit / Override Terminal Flag`는 저장 상태와 노출 status를 분리하며, `Reset Remote Terminal`은 RT 상태를 기본값으로 되돌리는 것으로 기준을 정했다.

### 구현

- `src/native/Domain/BusTypes.h`에 1차 Mode Code 4종과 `Dynamic Bus Control` enum 값을 추가했다.
- `src/native/Infrastructure/SimulatorBusAdapter`에 `lastAcceptedCommandWordRaw`, `terminalFlagInhibited`, effective status helper, reset helper를 추가했다.
- simulator mode handler가 `Reset Remote Terminal`, `Transmit Last Command Word`, `Inhibit Terminal Flag`, `Override Inhibit Terminal Flag`를 처리하도록 확장했다.

### 테스트 및 문서

- `tests/native/NativeTests.cpp`에 신규 테스트 5건을 추가했다.
- `docs/test/2026-04-18_ModeCode-tranche1_테스트계획.md`
- `docs/test/2026-04-18_ModeCode-tranche1_테스트결과.md`
- `README.md`, `tests/native/README.md`, `docs/design/2026-04-17_전체하네스MVP_설계검토.md`, `docs/test/2026-04-17_전체하네스MVP_테스트계획.md`, `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`
- `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`를 최신 상태로 갱신했다.

### 검증

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 실행 결과 네이티브 34건 통과
- `dotnet build-server shutdown`
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests" -v normal` 실행 결과 영향 범위 .NET 5건 통과
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 전체 스위트는 현재 환경에서 장시간 실행으로 타임아웃이 발생해 이번 사이클에서는 영향 범위 smoke만 채택했다.
