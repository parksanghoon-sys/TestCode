# 2026-04-17_HostInterop_테스트결과

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
- 실행 명령:
  `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj`
- 실제 네이티브 DLL 빌드 전제:
  Windows + PowerShell + Visual Studio 2022 C++ 도구

## 3. 테스트 시나리오
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
21. `dotnet build` output에 managed DLL 2종과 native DLL 1종이 함께 생성되는지 확인
22. `dotnet publish` output에 동일한 DLL 구성이 함께 생성되는지 확인

## 4. 입력 조건
- `bcSchedules` 1건을 포함한 정상 JSON과 의도적으로 잘못된 JSON을 각각 사용했다.
- fake native client는 세션 시작, 중지, Bus 전환 호출 횟수와 capacity 인자를 기록했다.
- `NativeHarnessClient` 단위 테스트는 fake low-level native API가 세션 핸들 버퍼, health JSON, telemetry JSON, 실패 status code, `requiredCapacity`를 각각 반환하도록 구성했다.
- runtime layout smoke test와 실패 테스트는 `MILSTD1553_NATIVE_RUNTIME_ROOT` 환경 변수 아래 `runtimes/win-x64/native/MilStd1553.Native.dll` 구조를 만들었다.
- packaging 테스트는 `dotnet build/publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o <temp>` 실행 결과를 직접 검사했다.

## 5. 기대 결과
- 잘못된 JSON은 `ScenarioValidationException`으로 차단된다.
- 정상 JSON은 `SessionCoordinator`를 통해 세션 시작 결과로 변환된다.
- 세션 없는 Bus 전환은 차단된다.
- 세션 중지 후 `HasActiveSession`은 `false`가 된다.
- telemetry 조회는 활성 세션이 있을 때만 허용된다.
- report export는 summary line 1건과 telemetry event line N건을 JSONL로 출력한다.
- `NativeHarnessClient`는 scenario JSON 직렬화, health / telemetry JSON 역직렬화, status code 예외 변환을 수행한다.
- `BufferTooSmall` 발생 시 `requiredCapacity` 값으로 버퍼를 재할당해 재시도한다.
- 실제 runtime layout smoke test에서는 `open -> switch -> get health -> poll telemetry -> poll telemetry(empty) -> stop`가 성공하고, stop 이후 health 조회는 `SessionNotFound` 기반 `NativeInteropException`으로 실패한다.
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락 시 `NativeLibraryLoadException`으로 실패한다.
- `dotnet build/publish` 출력 루트에는 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 존재한다.

## 6. 실제 결과
- `dotnet test` 결과 총 22건이 모두 통과했다.
- Host의 JSON 검증, 세션 제어, telemetry 조회, report export 경계가 유지된 상태로 통과했다.
- `NativeHarnessClient` 단위 테스트는 fake low-level native API를 통해 JSON marshalling / status code 변환 / `BufferTooSmall` 재시도를 검증했다.
- 실제 runtime layout smoke test는 `MilStd1553.Native.dll`을 `runtimes/win-x64/native` 경로에 배치한 뒤 명시적 런타임 로더가 동작함을 확인했다.
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락 시나리오는 모두 `NativeLibraryLoadException`으로 분류됐다.
- `dotnet build/publish` 테스트는 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `MilStd1553.Native.dll`가 한 번의 명령 결과 폴더 아래 함께 생성되는 것을 확인했다.
- 루트 `README.md`에 적을 quick guide 명령과 동일한 `dotnet build`, `dotnet publish`, `dotnet test` 경로를 다시 실행해 문서화 대상 명령이 현재 상태와 일치함을 확인했다.

## 7. 로그 확인 포인트
- 빌드 출력:
  `MilStd1553.Host.dll`, `MilStd1553.Interop.dll`, `MilStd1553.Host.Tests.dll`
- 테스트 요약:
  실패 `0`, 통과 `22`, 전체 `22`

## 8. 실패 케이스
- 이번 슬라이스 Red 단계에서는 packaging 테스트를 먼저 추가해 `dotnet build/publish` output 아래 native DLL이 없다는 실패를 만들었다.
- 이후 `MilStd1553.Interop.csproj`에 MSBuild packaging target을 추가해 Green을 확보했다.

## 9. 리스크
- 현재는 `MilStd1553.Interop`를 packaging 루트로 고정했지만, 나중에 사용자 실행 프로젝트가 별도로 생기면 output 루트 기준을 다시 정리해야 한다.
- WPF / CLI 중 최종 Host 프레임워크는 여전히 미정이다.

## 10. 후속 조치
- native library handle cache / dispose 정책을 별도 문서와 테스트로 정리한다.
- CSV / Markdown report export를 확장한다.
