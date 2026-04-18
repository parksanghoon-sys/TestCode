# C# 테스트

이 디렉터리는 Host / Interop / CLI 상위 계층 테스트를 담습니다.

## 실행 명령

```powershell
dotnet test .\tests\dotnet\MilStd1553.Host.Tests\MilStd1553.Host.Tests.csproj
```

## 현재 확인하는 것

- 시나리오 JSON 검증
- 세션 시작 / 중지 / Bus 전환
- telemetry 조회
- JSONL / CSV / Markdown report export
- BM JSONL replay reader
- CLI baseline 인자 해석 / 시나리오 파일 읽기 / 세션 lifecycle orchestration
- mock CLI example publish / run / 출력 검증
- runtime loader 실패 분류
- `dotnet build/publish` one-shot packaging 출력

## 권장 테스트 범위

- 유스케이스 흐름
- 오케스트레이션
- 설정 검증
- 상태 전이
- 로그 / 리포트 매핑
- 네이티브 연동
