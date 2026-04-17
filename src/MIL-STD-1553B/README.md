# MIL-STD-1553B 하네스 퀵 가이드

이 저장소는 **MIL-STD-1553B 시뮬레이터 우선 MVP 하네스**를 다룹니다. 현재 기준 산출물은 최종 사용자용 UI/CLI 실행 파일이 아니라, **C++ 네이티브 코어 + C# Host/Interop 라이브러리 + 테스트 경로**입니다.

## 현재 바로 할 수 있는 것

- `dotnet build` 또는 `dotnet publish` 1회로 managed DLL 2종과 native DLL 1종을 함께 출력
- C# Host / Interop 테스트 실행
- C++ 네이티브 테스트 실행
- runtime layout 기준 DLL smoke test와 packaging 검증

## 사전 준비

- Windows PowerShell 사용
- `.NET 10 SDK` 설치
- `Visual Studio 2022` 또는 `MSVC C++ 도구` 설치
  `tests/native/build_native_artifact.ps1`는 `vcvars64.bat`를 찾아 네이티브 빌드를 수행합니다.

## 가장 빠른 시작

저장소 루트에서 아래 순서로 실행하면 됩니다.

```powershell
dotnet build .\src\dotnet\MilStd1553.Interop\MilStd1553.Interop.csproj -c Debug
dotnet test .\tests\dotnet\MilStd1553.Host.Tests\MilStd1553.Host.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1
```

## DLL 한 번에 출력하기

가장 작은 packaging 진입점은 `src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`입니다.

```powershell
dotnet build .\src\dotnet\MilStd1553.Interop\MilStd1553.Interop.csproj -c Debug
```

기본 출력 경로:

- `src/dotnet/MilStd1553.Interop/bin/Debug/net10.0/MilStd1553.Interop.dll`
- `src/dotnet/MilStd1553.Interop/bin/Debug/net10.0/MilStd1553.Host.dll`
- `src/dotnet/MilStd1553.Interop/bin/Debug/net10.0/runtimes/win-x64/native/MilStd1553.Native.dll`

출력 위치를 직접 지정하려면:

```powershell
dotnet publish .\src\dotnet\MilStd1553.Interop\MilStd1553.Interop.csproj -c Debug -o .\artifacts\publish
```

이 경우에도 아래 구성이 함께 나와야 합니다.

- `artifacts/publish/MilStd1553.Interop.dll`
- `artifacts/publish/MilStd1553.Host.dll`
- `artifacts/publish/runtimes/win-x64/native/MilStd1553.Native.dll`

## 테스트 돌리는 법

### .NET 테스트

```powershell
dotnet test .\tests\dotnet\MilStd1553.Host.Tests\MilStd1553.Host.Tests.csproj
```

현재 검증 범위:

- 시나리오 JSON 검증
- 세션 시작 / 중지 / Bus 전환
- telemetry 조회
- JSONL report export
- `BufferTooSmall` 재시도
- explicit runtime loader
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락
- `dotnet build/publish` one-shot packaging

### 네이티브 테스트

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1
```

현재 검증 범위:

- Command / Status Word 해석
- BC → RT
- RT → BC
- RT ↔ RT
- 대표 Mode Code 2종
- timeout / retry
- 수동 Bus A/B 전환
- BM JSONL 저장
- 세션 중심 C ABI

### 네이티브 DLL만 따로 만들기

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\build_native_artifact.ps1 -ArtifactType NativeDll
```

출력 파일:

- `tests/native/artifacts/MilStd1553.Native.dll`

## 문서 위치

- 설계 기준선: `docs/design/2026-04-17_전체하네스MVP_설계검토.md`
- Host / Interop 테스트 계획: `docs/test/2026-04-17_HostInterop_테스트계획.md`
- Host / Interop 테스트 결과: `docs/test/2026-04-17_HostInterop_테스트결과.md`
- 전체 MVP 테스트 계획: `docs/test/2026-04-17_전체하네스MVP_테스트계획.md`
- 전체 MVP 테스트 결과: `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`

## 현재 제한 사항

- 최종 사용자용 CLI / WPF / WinUI 실행 프로젝트는 아직 없습니다.
- 실제 벤더 SDK 어댑터는 아직 연결되지 않았고, 현재 기준은 시뮬레이터 우선 MVP입니다.
- 자동 failover, 추가 Mode Code, RT 대 RT 물리 시퀀스 정합성은 후속 작업입니다.
