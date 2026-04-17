# C++ 테스트

이 디렉터리에는 네이티브 계층 테스트를 둔다.

## 실행 명령

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1
```

## 네이티브 DLL만 빌드할 때

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\build_native_artifact.ps1 -ArtifactType NativeDll
```

## 사전 준비

- Windows PowerShell
- Visual Studio 2022 또는 MSVC C++ 도구
- `vcvars64.bat`가 기본 설치 경로 아래에서 탐색 가능해야 함

## 현재 확인하는 것

- 워드 파싱
- BC / RT / BM 기본 흐름
- timeout / retry
- Bus A/B 전환
- Mode Code 2종
- C ABI 세션 경계

## 권장 테스트 범위

- 워드 파싱
- 메시지 조립/해석
- Mode Code 처리
- 타임아웃
- 오류 주입
- 버스 전환
