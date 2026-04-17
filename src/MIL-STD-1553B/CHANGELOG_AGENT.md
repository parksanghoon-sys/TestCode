# CHANGELOG_AGENT

## 2026-04-17

### 설계

- `docs/design/2026-04-17_전체하네스MVP_설계검토.md`를 작성하고, 시뮬레이터 우선 MVP 범위, BM JSONL 저장, Host telemetry / report export, 세션 중심 C ABI, `BufferTooSmall` 재시도 정책을 정리했다.
- explicit runtime loader 슬라이스에서 제품 기준 DLL 배치 경로를 `runtimes/win-x64/native/MilStd1553.Native.dll`로 고정하고, `MILSTD1553_NATIVE_RUNTIME_ROOT` 기반 테스트 override 전략을 설계 문서에 반영했다.
- packaging 슬라이스에서 `MilStd1553.Interop`를 managed output 루트로 고정하고, `dotnet build/publish` 1회 결과에 managed DLL 2종과 native DLL 1종이 함께 생성되도록 설계를 갱신했다.
- 루트 `README.md`를 canonical quick guide로 추가하고, 사전 준비 / one-shot packaging / .NET test / native test 명령을 공식 진입 문서로 정리했다.

### 구현

- `src/native/Domain`에 `CommandWord`, `StatusWord`, `TransferTypes`, Mode Code 관련 도메인 타입을 추가했다.
- `src/native/Application`에 `BusMonitorService`, `BusControllerService`를 추가하고 timeout / retry, 수동 Bus A/B 전환, `RT -> BC`, `RT <-> RT`, 대표 Mode Code 2종 경로를 구현했다.
- `src/native/Infrastructure`에 `SimulatorBusAdapter`, `JsonLinesBusEventStore`를 추가했다.
- `src/native/Interop`에 `NativeSessionApi`를 추가하고 `requiredCapacity` out parameter와 `BufferTooSmall` 처리까지 확장했다.
- `tests/native/build_native_artifact.ps1`를 정리하고 `PlaceholderDll` 빌드 경로를 추가했다.
- `src/dotnet/MilStd1553.Host`에 시나리오 로더, 세션 코디네이터, telemetry query, JSONL report export 경계를 추가했다.
- `src/dotnet/MilStd1553.Interop`에 `NativeHarnessClient`, `NativeLibraryPathResolver`, `NativeSessionLibrary`, `NativeLibraryLoadException`, `NativeLibraryFailureKind`를 추가해 explicit runtime loader와 실패 분류를 구현했다.
- `src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`에 MSBuild packaging target을 추가해 build/publish 시 native DLL을 runtime layout 경로로 자동 복사하도록 구현했다.
- `tests/dotnet/MilStd1553.Host.Tests/NativeHarnessClientEndToEndTests.cs`를 runtime layout smoke test + DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락 검증으로 확장했다.
- `tests/dotnet/MilStd1553.Host.Tests/InteropPackagingTests.cs`를 추가해 one-shot packaging output을 검증했다.

### 테스트 문서

- `docs/test/2026-04-17_전체하네스MVP_테스트계획.md`, `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`를 갱신했다.
- `docs/test/2026-04-17_HostInterop_테스트계획.md`, `docs/test/2026-04-17_HostInterop_테스트결과.md`를 runtime layout loader + packaging 기준으로 갱신했다.
- `docs/test/README.md`, `tests/dotnet/README.md`, `tests/native/README.md`에 실제 실행 명령과 확인 포인트를 추가했다.

### 상태 문서

- `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`를 현재 기준으로 업데이트했다.
- 다음 실행 단위를 native library handle cache / dispose 정책 정리로 재설정했다.

### 검증

- `tests/native/run_tests.ps1` 실행으로 네이티브 테스트 15건이 모두 통과했다.
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행으로 .NET 테스트 22건이 모두 통과했다.
- `dotnet build src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`와 `dotnet publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o artifacts/publish`를 다시 실행해 quick guide 명령이 실제 상태와 일치함을 확인했다.
