# MIL-STD-1553B 하네스 빠른 가이드

이 저장소는 **MIL-STD-1553B simulator 우선 MVP 하네스**를 담습니다. 현재 기준 산출물은 **C++ 네이티브 코어 + C# Host / Interop + CLI baseline + 테스트 경로**입니다.

## 지금 바로 할 수 있는 것

- `dotnet build` 또는 `dotnet publish` 1회로 CLI 실행 파일과 managed DLL 2종, native DLL 1종을 함께 출력
- 시나리오 JSON 파일로 세션 시작 / Bus 전환 / telemetry 출력 실행
- C# Host / Interop 테스트 실행
- C++ 네이티브 테스트 실행
- runtime layout DLL smoke test와 packaging 검증
- 자동 failover, CSV / Markdown report export, BM JSONL replay 최소 경로 검증

## 사전 준비

- Windows PowerShell
- `.NET 10 SDK`
- `Visual Studio 2022` 또는 `MSVC C++ 도구`

`tests/native/build_native_artifact.ps1`는 `vcvars64.bat`를 찾아 네이티브 빌드를 수행합니다.

## 가장 빠른 시작

저장소 루트에서 아래 순서로 실행하면 됩니다.

```powershell
dotnet build .\src\dotnet\MilStd1553.Cli\MilStd1553.Cli.csproj -c Debug
dotnet run --project .\src\dotnet\MilStd1553.Cli\MilStd1553.Cli.csproj -- --help
dotnet test .\tests\dotnet\MilStd1553.Host.Tests\MilStd1553.Host.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1
```

## mock example 바로 실행

실장비 없이 simulator / mock 경로로 실제 세션을 돌려보려면 아래 example 스크립트를 실행합니다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\examples\mock-cli\run_mock_cli_example.ps1
```

example 자산 위치:

- `examples/mock-cli/mock.scenario.json`
- `examples/mock-cli/run_mock_cli_example.ps1`
- `examples/mock-cli/README.md`

성공하면 `artifacts/mock-cli-example/mock-cli-output.log`에 로그가 남고, 콘솔에는 세션 시작, Bus 전환, `MessageFrame`, `BusSwitch`, 세션 종료가 출력됩니다.

## CLI 실행

CLI 진입점은 `src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj`입니다.

```powershell
dotnet run --project .\src\dotnet\MilStd1553.Cli\MilStd1553.Cli.csproj -- --scenario .\examples\mock-cli\mock.scenario.json --switch-bus B --poll-telemetry
```

시나리오 JSON은 최소한 `channelId`, `activeBus`, `bcSchedules`를 포함해야 합니다.

## DLL 한 번에 출력

운영용 기본 packaging 진입점은 `src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj`입니다.

```powershell
dotnet build .\src\dotnet\MilStd1553.Cli\MilStd1553.Cli.csproj -c Debug
```

기본 출력 경로:

- `src/dotnet/MilStd1553.Cli/bin/Debug/net10.0/MilStd1553.Cli.exe` 또는 `MilStd1553.Cli.dll`
- `src/dotnet/MilStd1553.Cli/bin/Debug/net10.0/MilStd1553.Interop.dll`
- `src/dotnet/MilStd1553.Cli/bin/Debug/net10.0/MilStd1553.Host.dll`
- `src/dotnet/MilStd1553.Cli/bin/Debug/net10.0/runtimes/win-x64/native/MilStd1553.Native.dll`

라이브러리 기준 packaging 진입점은 `src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`입니다.

직접 출력 경로를 지정하려면:

```powershell
dotnet publish .\src\dotnet\MilStd1553.Cli\MilStd1553.Cli.csproj -c Debug -o .\artifacts\publish
```

## 테스트 돌리는 법

### .NET 테스트

```powershell
dotnet test .\tests\dotnet\MilStd1553.Host.Tests\MilStd1553.Host.Tests.csproj
```

현재 검증 범위:

- 시나리오 JSON 검증
- 세션 시작 / 중지 / Bus 전환
- telemetry 조회
- JSONL / CSV / Markdown report export
- BM JSONL replay reader
- CLI argument parsing / scenario file reading / session lifecycle orchestration
- mock CLI example publish / run / 출력 검증
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
- 대표 Mode Code 5종 + 1차 Mode Code 4종
- timeout / retry / 자동 failover
- 수동 Bus A/B 전환
- BM JSONL 저장
- 세션 중심 C ABI

### 네이티브 DLL만 만들기

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\build_native_artifact.ps1 -ArtifactType NativeDll
```

출력 파일:

- `tests/native/artifacts/MilStd1553.Native.dll`

## 문서 위치

- 전체 MVP 설계:
  `docs/design/2026-04-17_전체하네스MVP_설계검토.md`
- mock example 설계:
  `docs/design/2026-04-18_mock-cli-example_설계검토.md`
- Mode Code tranche 1 설계:
  `docs/design/2026-04-18_ModeCode-tranche1_설계검토.md`
- Host / Interop 테스트 계획 / 결과:
  `docs/test/2026-04-17_HostInterop_테스트계획.md`
  `docs/test/2026-04-17_HostInterop_테스트결과.md`
- 전체 MVP 테스트 계획 / 결과:
  `docs/test/2026-04-17_전체하네스MVP_테스트계획.md`
  `docs/test/2026-04-17_전체하네스MVP_테스트결과.md`
- mock example 테스트 계획 / 결과:
  `docs/test/2026-04-18_mock-cli-example_테스트계획.md`
  `docs/test/2026-04-18_mock-cli-example_테스트결과.md`
- Mode Code tranche 1 테스트 계획 / 결과:
  `docs/test/2026-04-18_ModeCode-tranche1_테스트계획.md`
  `docs/test/2026-04-18_ModeCode-tranche1_테스트결과.md`

## 현재 제한 사항

- 운영용 CLI baseline은 있지만 WPF / WinUI 기반 rich UI는 아직 없습니다.
- 실제 벤더 SDK adapter는 아직 없고 현재 기준선은 simulator 우선 MVP입니다.
- 실장비 벤더 SDK 구체화, richer replay, richer report presentation은 후속 작업입니다.
