# CURRENT_PLAN

## 현재 목표

- one-shot packaging 출력까지 고정된 상태를 기준으로, 다음 사이클에서는 explicit runtime loader의 native library handle cache / dispose 정책을 정리한다.

## 현재 실행 지점

- 설계 검토 문서가 explicit runtime loader와 one-shot packaging 전략까지 반영된 상태다.
- 루트 `README.md`를 canonical quick guide로 추가했고, build / publish / .NET test / native test 명령을 다시 검증했다.
- 네이티브 Domain / Application / Infrastructure / Interop 초안이 완료됐다.
- Host / Interop / telemetry / report export / runtime loader / packaging 초안이 완료됐다.
- `tests/native/run_tests.ps1` 기준 네이티브 15건 통과
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj` 기준 .NET 22건 통과
- `dotnet build src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj` 재검증 완료
- `dotnet publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj -o artifacts/publish` 재검증 완료
- runtime layout 기반 실제 DLL smoke test 완료
- DLL 누락 / 잘못된 바이너리 / 엔트리 포인트 누락 테스트 완료
- `dotnet build/publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj` 1회 결과로 managed DLL 2종과 native DLL 1종이 함께 출력되는 packaging 테스트 완료

## 다음 실행 단위

1. explicit runtime loader가 적재한 native library handle의 cache / dispose 정책을 설계 문서와 결정 로그에 정리한다.
2. 필요하면 handle lifecycle 정책을 검증하는 .NET 테스트를 추가한다.
3. 관련 테스트 문서와 상태 파일을 다시 갱신한다.

## 후속 작업 후보

- CSV / Markdown report export 확장
- 자동 failover 정책
- `RT <-> RT` 물리 시퀀스 정합성

## 검증 경로

- `tests/native/run_tests.ps1`
- `dotnet test tests/dotnet/MilStd1553.Host.Tests/MilStd1553.Host.Tests.csproj`
- 문서 결과는 `docs/test/2026-04-17_전체하네스MVP_*`, `docs/test/2026-04-17_HostInterop_*`에 누적 반영한다.

## 확인된 사실

- 현재 저장소는 시뮬레이터 우선 MVP 경로를 기준으로 한다.
- C++은 프로토콜 / 시뮬레이터 / BM / C ABI를 담당하고, C#은 세션 제어 / 조회 / 리포트 / 테스트 오케스트레이션을 담당한다.
- Host 로더 경계는 `NativeLibrary.Load` 기반 explicit runtime layout 방식으로 고정됐다.
- `MilStd1553.Interop.csproj`를 기준으로 `dotnet build/publish` 1회 결과에 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 출력된다.

## 미확정 사항

- 실제 사용 장치 벤더 SDK와 장비 종류
- 최종 Host 형태가 CLI인지 WPF / WinUI인지
- MVP 이후 포함할 Mode Code 우선순위 전체 목록
