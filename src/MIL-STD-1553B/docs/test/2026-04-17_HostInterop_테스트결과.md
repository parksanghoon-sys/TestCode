# 2026-04-17_HostInterop_테스트결과

## 1. 테스트 대상

- `src/dotnet/MilStd1553.Host/Services/ScenarioDefinitionJsonLoader`
- `src/dotnet/MilStd1553.Host/Services/SessionCoordinator`
- `src/dotnet/MilStd1553.Host/Services/TelemetryQueryService`
- `src/dotnet/MilStd1553.Host/Services/JsonLinesSessionReportExporter`
- `src/dotnet/MilStd1553.Host/Services/CsvSessionReportExporter`
- `src/dotnet/MilStd1553.Host/Services/MarkdownSessionReportExporter`
- `src/dotnet/MilStd1553.Host/Services/JsonLinesTelemetryReplayReader`
- `src/dotnet/MilStd1553.Interop/Services/NativeHarnessClient`
- `src/dotnet/MilStd1553.Interop/NativeMethods/PInvokeNativeSessionApi`
- `src/dotnet/MilStd1553.Interop/NativeMethods/NativeSessionLibrary`
- `src/dotnet/MilStd1553.Interop/NativeMethods/NativeSessionLibraryCache`
- `src/dotnet/MilStd1553.Interop/NativeMethods/RuntimeNativeLibraryPlatform`
- `src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`
- `src/dotnet/MilStd1553.Cli/Services/HarnessCliRunner`
- `src/dotnet/MilStd1553.Cli/Services/ScenarioFileReader`
- `src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj`
- `tests/native/build_native_artifact.ps1`

## 2. 테스트 환경

- 운영체제: Windows
- .NET SDK: `10.0.103`
- 테스트 프레임워크: xUnit
- 실행 명령:
  `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj`
- 실제 네이티브 DLL smoke test와 cache test는 PowerShell + Visual Studio 2022 C++ 도구를 사용해 `tests/native/build_native_artifact.ps1`로 `MilStd1553.Native.dll`을 생성했다.

## 3. 테스트 시나리오

1. 잘못된 시나리오 JSON 거부
2. 정상 시나리오 JSON 로딩
3. 정상 시작과 중복 시작 차단
4. 세션 없는 Bus A/B 전환 차단
5. 세션 중지 후 상태 정리
6. fake native client를 통한 Host-Interop 세션 경계 검증
7. 활성 세션 없는 telemetry 조회 차단
8. 활성 세션 이후 telemetry DTO 반환
9. JSONL report summary line 생성
10. 빈 telemetry에 대한 summary-only export
11. scenario JSON 직렬화와 session start result 매핑
12. native health JSON / telemetry JSON payload 매핑
13. native 실패 status code의 `NativeInteropException` 변환
14. `OpenSessionAsync`의 `BufferTooSmall` 재시도
15. `GetHealthSnapshotAsync`의 `BufferTooSmall` 재시도
16. `PollTelemetryAsync`의 `BufferTooSmall` 재시도
17. 실제 runtime layout DLL 적재 기준 `NativeHarnessClient` smoke test
18. runtime DLL 누락 시 `NativeLibraryLoadException(LibraryNotFound)` 변환
19. 잘못된 runtime DLL 적재 시 `NativeLibraryLoadException(InvalidBinary)` 변환
20. 엔트리 포인트가 없는 DLL 적재 시 `NativeLibraryLoadException(EntryPointMissing)` 변환
21. `dotnet build` output에 managed DLL 2종과 native DLL 1종이 함께 생성되는지 확인
22. `dotnet publish` output에 동일한 DLL 구성이 함께 생성되는지 확인
23. 동일한 runtime DLL 경로 요청 시 loader cache 재사용과 즉시 free 없음 확인
24. 테스트 전용 cache dispose 시 단일 free 확인
25. CSV report export
26. Markdown report export
27. BM JSONL replay reader
28. `HarnessCliRunner` 도움말 출력
29. `HarnessCliRunner` 시나리오 인자 누락 오류 처리
30. `HarnessCliRunner` 시나리오 파일 입력 기반 세션 시작 / Bus 전환 / telemetry 출력 / 세션 종료
31. `dotnet build` CLI output에 실행 파일과 managed / native DLL이 함께 생성되는지 확인
32. `dotnet publish` CLI output에 동일한 구성이 함께 생성되는지 확인
33. `examples/mock-cli` example script가 publish / run / output verification까지 끝내는지 확인
34. receive scenario runtime layout smoke test가 payload를 잃지 않는지 확인

## 4. 입력 조건

- `bcSchedules` 1건을 포함한 정상 JSON과 의도적으로 잘못된 JSON을 각각 사용했다.
- fake native client는 세션 시작, 중지, Bus 전환 호출 횟수와 capacity 인자를 기록한다.
- `NativeHarnessClient` 단위 테스트는 fake low-level native API가 세션 핸들 버퍼, health JSON, telemetry JSON, 실패 status code, `requiredCapacity`를 각각 반환하도록 구성했다.
- runtime layout smoke test와 실패 테스트는 `MILSTD1553_NATIVE_RUNTIME_ROOT` 환경 변수 아래 `runtimes/win-x64/native/MilStd1553.Native.dll` 구조를 만들어 사용했다.
- cache 테스트는 placeholder DLL 영향 없이 항상 실제 네이티브 DLL을 다시 빌드한 뒤 로드한다.
- packaging 테스트는 `dotnet build/publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o <temp>`와 `dotnet build/publish src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj -o <temp>` 결과를 직접 검사했다.

## 5. 기대 결과

- Host는 JSON 검증, 세션 제어, telemetry 조회, report export, replay reader 경계를 안정적으로 유지한다.
- `NativeHarnessClient`는 JSON marshalling, status code 변환, `BufferTooSmall` 재시도, runtime loader failure kind 분류를 정확히 수행한다.
- 실제 runtime layout DLL smoke test에서는 `open -> poll telemetry(initial MessageFrame) -> switch -> get health -> poll telemetry(BusSwitch) -> poll telemetry(empty) -> stop` 흐름이 성공해야 한다.
- runtime loader cache는 같은 DLL 경로를 한 번만 적재하고, 테스트 전용 dispose에서만 free 해야 한다.
- CLI 흐름과 packaging output은 문서에 적은 명령과 동일한 결과를 내야 한다.

## 6. 실제 결과

- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 실행 결과 총 34건이 모두 통과했다.
- 2026-04-18 RT ↔ RT timing 사이클에서는 `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj --no-build --filter "FullyQualifiedName~NativeHarnessClientEndToEndTests" -v normal` 기준 영향 범위 smoke 5건을 다시 확인했다.
- 실제 runtime layout smoke test에서 `NativeHarnessClient`는 `OpenSessionAsync` 직후 첫 번째 `PollTelemetryAsync`로 `MessageFrame` 1건을 받았고, 이후 `SwitchBusAsync(BusLine.A)` 뒤 두 번째 `PollTelemetryAsync`로 `BusSwitch` 1건을 받았다.
- 추가 receive scenario smoke test에서 첫 telemetry `MessageFrame`의 payload가 `0x0102`로 유지되는 것을 확인했다.
- stop 이후 `GetHealthSnapshotAsync`는 `SessionNotFound` 기반 `NativeInteropException`으로 실패했다.
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락은 모두 `NativeLibraryLoadException`으로 분류됐다.
- `NativeSessionLibraryCacheTests`는 같은 DLL 경로에 대해 load 1회 / free 0회, cache dispose 이후 free 1회를 확인했다.
- cache 테스트는 실제 네이티브 DLL을 다시 빌드하도록 바꿔 placeholder DLL이 남아도 테스트 순서에 영향을 받지 않았다.
- `MockCliExampleSmokeTests`는 `examples/mock-cli/run_mock_cli_example.ps1`를 실제로 실행해 publish, CLI run, 출력 로그 검증까지 성공함을 확인했다.
- `dotnet build/publish` 테스트는 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 생성됨을 확인했다.
- CLI packaging 테스트는 `MilStd1553.Cli.exe` 또는 `MilStd1553.Cli.dll`, `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 생성됨을 확인했다.
- report / replay 테스트는 JSONL / CSV / Markdown export와 JSONL replay reader가 동일한 telemetry DTO shape를 공유함을 확인했다.
- 재검증 중 VS Code `csdevkit` build host가 기본 `obj/bin`을 잠그는 경우가 있었지만, build host 종료와 `dotnet build-server shutdown` 이후 표준 `dotnet test` 명령이 정상 통과했다.

## 7. 로그 확인 포인트

- 테스트 실행 요약:
  실패 `0`, 통과 `33`, 전체 `33`
- 실제 smoke test 확인 포인트:
  첫 번째 telemetry는 `MessageFrame`
  두 번째 telemetry는 `BusSwitch`
  세 번째 telemetry는 `[]`

## 8. 실패 케이스

- Red 단계에서는 실제 runtime layout smoke test를 먼저 강화해 “첫 번째 `PollTelemetry`가 `MessageFrame`이어야 한다”는 조건을 추가했고, 기존 stub `NativeSessionApi`에서는 이 검증이 실패했다.
- 이후 `NativeSessionApi`가 실제 `BusControllerService` / `BusMonitorService` / `VendorSdkBusAdapter` 경로를 조립하도록 구현을 바꾸고 Green으로 전환했다.

## 9. 리스크

- 실장비용 `IVendorChannel` concrete binding은 아직 없다.
- rich UI가 필요해지면 현재 CLI 기준 진입점을 별도 Presentation 레이어로 확장해야 한다.
- runtime loader cache는 경로 기반 프로세스 공유 정책을 전제로 하므로, 향후 hot-swap이나 명시적 unload 요구가 생기면 정책을 재설계해야 한다.

## 10. 후속 조치

- 실장비용 벤더 SDK가 확보되면 `IVendorChannel` concrete binding과 실장비 smoke test를 추가한다.
- replay / report schema versioning과 richer presentation은 후순위로 관리한다.
