# C# 상위 계층

이 디렉터리는 다음 책임을 가집니다.

- UI / CLI / 운영 도구
- 설정 관리
- 시나리오 오케스트레이션
- 리포트 및 로그 표시
- 통합 제어 계층

## 규칙

- Domain / Application을 우회해서 직접 Infrastructure 구현을 참조하지 않는다.
- 모든 public 타입과 메서드에는 한글 XML 주석을 작성한다.
- 프로젝트가 허용하면 최신 C# 문법을 우선한다.

## 출력 기준

- `dotnet build src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`
  1회로 `MilStd1553.Interop.dll`, `MilStd1553.Host.dll`, `runtimes/win-x64/native/MilStd1553.Native.dll`가 함께 출력된다.
- `dotnet publish src/dotnet/MilStd1553.Interop/MilStd1553.Interop.csproj`
  도 같은 runtime layout을 유지한다.
