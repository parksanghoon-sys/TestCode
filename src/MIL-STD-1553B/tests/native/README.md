# C++ 테스트

이 디렉터리는 네이티브 계층 테스트를 다룬다.

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
- `vcvars64.bat`가 기본 설치 경로 아래에서 검색 가능해야 한다.

## 현재 확인하는 것

- 워드 파싱
- BC / RT / BM 기본 흐름
- timeout / retry / 자동 failover
- Bus A/B 전환
- Mode Code 9종
- line fault
- C ABI 세션 경계
- vendor-neutral SDK adapter lazy open / status mapping / bus select forwarding
- `SimulatorVendorChannel` concrete binding과 selected bus 반영

## 권장 테스트 범위

- 워드 파싱
- 메시지 조합 / 해석
- Mode Code 처리
- 타임아웃 / 오류 주입
- 버스 전환
- RT 대 RT 검증
- 벤더 SDK bridge 계약
