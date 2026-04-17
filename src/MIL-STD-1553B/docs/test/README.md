# 테스트 문서 위치

이 디렉터리에는 테스트 계획서와 테스트 결과 문서를 저장한다.

가장 빠른 검증 명령은 아래 두 가지다.

```powershell
dotnet test .\tests\dotnet\MilStd1553.Host.Tests\MilStd1553.Host.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1
```

## 파일명 규칙

- `YYYY-MM-DD_기능명_테스트계획.md`
- `YYYY-MM-DD_기능명_테스트결과.md`

## 필수 포함 항목

- 테스트 대상
- 테스트 시나리오
- 정상 흐름
- 비정상 흐름
- 타임아웃 / 재시도
- Bus A/B 전환
- BC → RT
- RT → BC
- RT ↔ RT
- Mode Code
- 로그 확인 포인트
- 결과 요약
