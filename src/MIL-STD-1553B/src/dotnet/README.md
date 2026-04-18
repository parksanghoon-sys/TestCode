# C# 상위 계층

이 디렉터리는 Host / Interop / CLI 같은 상위 계층을 담습니다.

## 책임

- UI / CLI / 운영 도구
- 설정 관리
- 시나리오 오케스트레이션
- 리포트와 로그 표시
- 네이티브 C ABI 호출 래핑

## 출력 기준

- `dotnet build src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj`
  1회로 `MilStd1553.Cli.exe` 또는 `MilStd1553.Cli.dll`, `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 출력됩니다.
- `dotnet publish src/dotnet/MilStd1553.Cli/MilStd1553.Cli.csproj`
  도 같은 runtime layout을 유지합니다.
- `dotnet build src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`
  1회로 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 출력됩니다.
- `dotnet publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`
  도 같은 runtime layout을 유지합니다.

## mock example

- `examples/mock-cli/mock.scenario.json`
  실장비 없이 CLI를 바로 검증하는 기본 시나리오입니다.
- `examples/mock-cli/run_mock_cli_example.ps1`
  CLI를 publish한 뒤 example scenario로 실제 세션을 실행하고 출력까지 검증합니다.
