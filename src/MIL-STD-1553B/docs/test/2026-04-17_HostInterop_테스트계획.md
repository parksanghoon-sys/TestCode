# 2026-04-17_HostInterop_테스트계획

## 1. 테스트 대상
- `src/dotnet/MilStd1553.Host/Services/ScenarioDefinitionJsonLoader`
- `src/dotnet/MilStd1553.Host/Services/SessionCoordinator`
- `src/dotnet/MilStd1553.Host/Services/TelemetryQueryService`
- `src/dotnet/MilStd1553.Host/Services/JsonLinesSessionReportExporter`
- `src/dotnet/MilStd1553.Interop/Services/NativeHarnessClient`
- `src/dotnet/MilStd1553.Interop/NativeMethods/PInvokeNativeSessionApi`
- `src/dotnet/MilStd1553.Interop/NativeMethods/NativeSessionLibrary`
- `src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`
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
- JSONL session report export
- fake low-level native API 기반 JSON marshalling / status code 변환
- `BufferTooSmall` 응답 기반 UTF-16 caller buffer 재할당 재시도
- `runtimes/win-x64/native/MilStd1553.Native.dll` 기준 실제 런타임 로더 smoke test
- 런타임 DLL 누락, 잘못된 바이너리, 엔트리 포인트 불일치 실패 경로
- `dotnet build/publish` 1회로 managed DLL과 native DLL이 함께 출력되는 packaging 경로

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
17. 실제 runtime layout DLL 적재 기반 `NativeHarnessClient` smoke test
18. runtime DLL 누락 시 `NativeLibraryLoadException(LibraryNotFound)` 변환
19. 잘못된 runtime DLL 적재 시 `NativeLibraryLoadException(InvalidBinary)` 변환
20. 엔트리 포인트 누락 DLL 적재 시 `NativeLibraryLoadException(EntryPointMissing)` 변환
21. `dotnet build`가 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`를 함께 출력
22. `dotnet publish`가 동일한 runtime layout으로 DLL을 함께 출력

## 5. MIL-STD-1553B 전용 확인 관점
- `BC -> RT`, `RT -> BC`, `RT <-> RT`, `Mode Code`, timeout / retry 규칙은 네이티브 테스트에서 검증한다.
- Host / Interop 테스트는 위 프로토콜 결과를 다시 해석하지 않고 세션 경계, DTO 전달, 예외 변환, Bus A/B 전환 요청 위임, caller buffer 재시도, 런타임 로더 실패 분류, packaging 출력 경로가 깨지지 않는지를 확인한다.

## 6. 입력 조건
- JSON은 설계 문서 기준 `bcSchedules`, `channelId`, `activeBus` 필드를 포함한다.
- fake native client는 세션 시작, 중지, Bus 전환 호출 횟수와 버퍼 capacity 인자를 기록한다.
- `NativeHarnessClient` 단위 테스트는 fake low-level native API가 세션 핸들 버퍼, health JSON, telemetry JSON, 실패 status code, `requiredCapacity`를 스크립트로 반환하도록 구성한다.
- runtime layout smoke test와 실패 테스트는 `MILSTD1553_NATIVE_RUNTIME_ROOT` 환경 변수 아래에 `runtimes/win-x64/native/MilStd1553.Native.dll` 구조를 만든다.
- packaging 테스트는 `dotnet build/publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o <temp>` 실행 결과를 직접 검사한다.

## 7. 기대 결과
- 잘못된 JSON은 Host에서 차단되고 native 호출로 내려가지 않는다.
- 정상 JSON은 `SessionCoordinator`를 통해 세션 시작 결과로 변환된다.
- 중복 시작과 세션 없는 Bus 전환은 `InvalidOperationException`으로 차단된다.
- telemetry 조회는 활성 세션이 있을 때만 허용된다.
- report export는 summary line 1건과 telemetry event line N건을 JSONL로 출력한다.
- `NativeHarnessClient`는 scenario JSON 직렬화, health / telemetry JSON 역직렬화, status code 예외 변환을 수행한다.
- `BufferTooSmall` 발생 시 `NativeHarnessClient`는 native가 돌려준 필요 길이로 버퍼를 재할당하고 재시도한다.
- runtime layout smoke test에서는 실제 DLL 적재 후 `open -> switch -> get health -> poll telemetry -> poll telemetry(empty) -> stop` 흐름이 성공하고, stop 이후 `GetHealthSnapshotAsync`는 `NativeInteropException`으로 실패한다.
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락 시 로더 경계는 `NativeLibraryLoadException`으로 실패한다.
- `dotnet build/publish` 1회 결과 폴더에는 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 존재해야 한다.

## 8. 로그 확인 포인트
- `dotnet test` 복원과 빌드 성공 여부
- `dotnet build/publish` 출력 루트와 runtime layout 경로 아래 DLL 존재 여부
- 최종 테스트 요약의 통과 개수와 실패 0건 여부

## 9. 실패 케이스 및 분석 포인트
- NuGet 복원 실패 시 Host 테스트가 시작되지 않을 수 있다.
- Visual Studio C++ 도구가 없으면 실제 DLL smoke test, placeholder DLL 빌드, packaging target이 실패한다.
- JSON 계약이 바뀌면 Host 검증 로직과 `NativeHarnessClient` 매핑 테스트가 함께 깨질 수 있다.
- runtime loader 예외 유형이나 packaging 출력 경로가 바뀌면 테스트 기대값과 문서를 함께 갱신해야 한다.

## 10. 리스크 및 후속 조치
- 현재는 `MilStd1553.Interop`를 packaging 루트로 고정했지만, 최종 사용자용 실행 프로젝트가 별도로 생기면 output 루트 기준을 다시 정리해야 한다.
- CSV / Markdown report export와 native library handle lifecycle 정책은 다음 슬라이스에서 확장한다.
