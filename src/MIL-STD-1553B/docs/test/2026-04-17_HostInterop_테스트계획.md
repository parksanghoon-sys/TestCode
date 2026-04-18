# 2026-04-17_HostInterop_테스트계획

## 1. 테스트 대상
- `src/dotnet/MilStd1553.Host/Services/ScenarioDefinitionJsonLoader`
- `src/dotnet/MilStd1553.Host/Services/CsvSessionReportExporter`
- `src/dotnet/MilStd1553.Host/Services/MarkdownSessionReportExporter`
- `src/dotnet/MilStd1553.Host/Services/JsonLinesTelemetryReplayReader`
- `src/dotnet/MilStd1553.Host/Services/SessionCoordinator`
- `src/dotnet/MilStd1553.Host/Services/TelemetryQueryService`
- `src/dotnet/MilStd1553.Host/Services/JsonLinesSessionReportExporter`
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
- .NET SDK: `10.0.103`
- 테스트 프레임워크: xUnit
- 실행 명령: `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj`
- 실제 네이티브 DLL 빌드 전제: Windows + PowerShell + Visual Studio 2022 C++ 도구

## 3. 테스트 범위
- Host 계층의 시나리오 JSON 검증
- 세션 시작, 중복 시작 차단, 세션 없는 Bus A/B 전환 차단, 정상 중지
- 활성 세션 기반 telemetry 조회
- JSONL / CSV / Markdown session report export
- BM JSONL replay reader
- fake low-level native API 기반 JSON marshalling / status code 변환
- `BufferTooSmall` 응답 기반 UTF-16 caller buffer 재할당 재시도
- `runtimes/win-x64/native/MilStd1553.Native.dll` 기준 실제 런타임 로더 smoke test
- 런타임 DLL 누락, 잘못된 바이너리, 엔트리 포인트 불일치 실패 경로
- 경로별 native library handle cache 재사용과 테스트용 dispose 정책
- `dotnet build/publish` 1회로 managed DLL과 native DLL이 함께 출력되는 packaging 경로
- CLI baseline의 시나리오 파일 입력, 세션 실행, 선택적 Bus 전환, telemetry 출력, 세션 종료

## 4. 테스트 시나리오 목록
1. 잘못된 시나리오 JSON 거부
2. 정상 시나리오 JSON 로딩
3. 정상 시작 후 중복 시작 차단
4. 세션 없는 Bus A/B 전환 차단
5. 세션 중지 후 상태 정리
6. fake native client를 통한 Host-Interop 세션 경계 검증
7. 활성 세션 없는 telemetry 조회 차단
8. 활성 세션 이후 telemetry DTO 반환
9. JSONL report summary line 생성
10. 빈 telemetry일 때 summary-only export
11. scenario JSON 직렬화와 session start result 매핑
12. native health JSON / telemetry JSON payload 매핑
13. native 실패 status code의 `NativeInteropException` 변환
14. `OpenSessionAsync`의 `BufferTooSmall` 재시도
15. `GetHealthSnapshotAsync`의 `BufferTooSmall` 재시도
16. `PollTelemetryAsync`의 `BufferTooSmall` 재시도
17. 실제 runtime layout DLL 적재 기반 `NativeHarnessClient` smoke test와 초기 `MessageFrame` telemetry 확인
18. runtime DLL 누락 시 `NativeLibraryLoadException(LibraryNotFound)` 변환
19. 잘못된 runtime DLL 적재 시 `NativeLibraryLoadException(InvalidBinary)` 변환
20. 엔트리 포인트 누락 DLL 적재 시 `NativeLibraryLoadException(EntryPointMissing)` 변환
21. `dotnet build`가 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`를 함께 출력
22. `dotnet publish`가 동일한 runtime layout으로 DLL을 함께 출력
23. 동일한 runtime DLL 경로를 여러 번 요청해도 로더가 한 번만 적재하고 즉시 free 하지 않음
24. 테스트 전용 cache dispose는 캐시된 라이브러리 핸들을 한 번만 해제
25. CSV exporter는 summary와 event 행을 같은 telemetry 의미로 출력
26. Markdown exporter는 session summary와 telemetry 표를 출력
27. JSONL replay reader는 BM 로그를 telemetry DTO로 복원
28. `HarnessCliRunner`는 `--help` 요청 시 사용법을 출력하고 종료
29. `HarnessCliRunner`는 `--scenario` 누락 시 오류와 사용법을 출력하고 실패 종료
30. `HarnessCliRunner`는 시나리오 파일을 읽어 세션 시작, 선택적 Bus 전환, telemetry 출력, 세션 종료를 순서대로 수행
31. `dotnet build`가 `MilStd1553.Cli.exe`(또는 `MilStd1553.Cli.dll`), `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`를 함께 출력
32. `dotnet publish`가 CLI 기준 동일한 runtime layout으로 실행 파일과 DLL을 함께 출력

## 5. MIL-STD-1553B 전용 확인 관점
- `BC -> RT`, `RT -> BC`, `RT <-> RT`, `Mode Code`, timeout / retry 규칙은 네이티브 테스트에서 검증한다.
- Host / Interop 테스트는 위 프로토콜 결과를 다시 해석하지 않고 세션 경계, DTO 전달, 예외 변환, Bus A/B 전환 요청 위임, caller buffer 재시도, 런타임 로더 실패 분류, packaging 출력 경로가 깨지지 않는지를 확인한다.

## 6. 입력 조건
- JSON은 설계 문서 기준 `bcSchedules`, `channelId`, `activeBus` 필드를 포함한다.
- fake native client는 세션 시작, 중지, Bus 전환 호출 횟수와 버퍼 capacity 인자를 기록한다.
- `NativeHarnessClient` 단위 테스트는 fake low-level native API가 세션 핸들 버퍼, health JSON, telemetry JSON, 실패 status code, `requiredCapacity`를 스크립트로 반환하도록 구성한다.
- runtime layout smoke test와 실패 테스트는 `MILSTD1553_NATIVE_RUNTIME_ROOT` 환경 변수 아래에 `runtimes/win-x64/native/MilStd1553.Native.dll` 구조를 만든다.
- packaging 테스트는 `dotnet build/publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o <temp>`와 `dotnet build/publish src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj -o <temp>` 실행 결과를 직접 검사한다.
- CLI 테스트는 fake `ISessionCoordinator`, fake `ITelemetryQueryService`, fake `IScenarioFileReader`를 사용해 파일 입출력과 네이티브 호출을 분리한다.

## 7. 기대 결과
- 잘못된 JSON은 Host에서 차단되고 native 호출로 내려가지 않는다.
- 정상 JSON은 `SessionCoordinator`를 통해 세션 시작 결과로 변환된다.
- 중복 시작과 세션 없는 Bus 전환은 `InvalidOperationException`으로 차단된다.
- telemetry 조회는 활성 세션이 있을 때만 허용된다.
- report export는 JSONL / CSV / Markdown 포맷으로 같은 telemetry 의미를 유지한다.
- replay reader는 BM JSONL을 `TelemetryEventRecord` 목록으로 복원한다.
- `NativeHarnessClient`는 scenario JSON 직렬화, health / telemetry JSON 역직렬화, status code 예외 변환을 수행한다.
- `BufferTooSmall` 발생 시 `NativeHarnessClient`는 native가 돌려준 필요 길이로 버퍼를 재할당하고 재시도한다.
- runtime layout smoke test에서는 실제 DLL 적재 후 `open -> poll telemetry(initial message frame) -> switch -> get health -> poll telemetry(bus switch) -> poll telemetry(empty) -> stop` 흐름이 성공하고, stop 이후 `GetHealthSnapshotAsync`는 `NativeInteropException`으로 실패한다.
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락 시 로더 경계는 `NativeLibraryLoadException`으로 실패한다.
- runtime loader는 resolved path 기준으로 네이티브 DLL을 프로세스 캐시에 보관하고, 성공 적재 직후 free 하지 않는다.
- 테스트 전용 cache dispose는 캐시된 라이브러리 핸들을 한 번만 해제해야 한다.
- `dotnet build/publish` 1회 결과 폴더에는 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 존재해야 한다.
- CLI 실행 경로는 시나리오 파일 내용이 유효할 때 세션 시작과 종료를 한 번씩만 호출하고, 선택적 Bus 전환과 telemetry 출력이 요청된 순서대로 이어져야 한다.

## 8. 로그 확인 포인트
- `dotnet test` 복원과 빌드 성공 여부
- `dotnet build/publish` 출력 루트와 runtime layout 경로 아래 DLL 존재 여부
- 최종 테스트 요약의 통과 개수와 실패 0건 여부
- CSV / Markdown / replay 출력이 JSONL telemetry 의미와 모순되지 않는지 여부

## 9. 실패 케이스 및 분석 포인트
- NuGet 복원 실패 시 Host 테스트가 시작되지 않을 수 있다.
- Visual Studio C++ 도구가 없으면 실제 DLL smoke test, placeholder DLL 빌드, packaging target이 실패한다.
- JSON 계약이 바뀌면 Host 검증 로직과 `NativeHarnessClient` 매핑 테스트가 함께 깨질 수 있다.
- runtime loader 예외 유형이나 packaging 출력 경로가 바뀌면 테스트 기대값과 문서를 함께 갱신해야 한다.
- hot-swap 또는 명시적 unload 요구가 생기면 현재 프로세스 수명 기반 cache 정책을 다시 검토해야 한다.
- replay 입력 스키마가 BM JSONL과 달라지면 Host replay reader와 exporter 테스트를 함께 갱신해야 한다.
- CLI 인자 규격이나 출력 형식이 바뀌면 quick guide와 CLI 테스트 기대값을 함께 갱신해야 한다.

## 10. 리스크 및 후속 조치
- 운영자용 기본 실행 루트는 `MilStd1553.Cli`로 옮기되, `MilStd1553.Interop` 라이브러리 packaging 경로도 하위 호환으로 유지해야 한다.
- CSV / Markdown report export와 replay reader는 이번 슬라이스에서 최소 구현으로 고정하고, 편집형 replay는 후속 범위로 둔다.
- 실제 벤더 SDK 연결은 네이티브 `IVendorChannel` 구현이 준비된 뒤 Host smoke test 대상에 포함한다.
