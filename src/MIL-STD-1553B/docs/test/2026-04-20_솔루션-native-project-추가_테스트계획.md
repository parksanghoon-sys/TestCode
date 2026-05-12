# 솔루션 native project 추가 테스트 계획

## 테스트 대상

- `MIL-STD-1553B.sln`
- `src/native/MilStd1553.Native.vcxproj`
- `tests/native/MilStd1553.Native.Tests.vcxproj`

## 테스트 시나리오

- Visual Studio/MSBuild가 솔루션에서 네이티브 프로젝트를 인식한다.
- `Debug|x64` 구성으로 네이티브 DLL 프로젝트가 빌드된다.
- `Debug|x64` 구성으로 네이티브 테스트 프로젝트가 빌드된다.
- 기존 `tests/native/run_tests.ps1` 경로가 계속 성공한다.

## 정상 흐름

- 솔루션 빌드 성공
- `tests/native/artifacts/MilStd1553.Native.dll` 생성
- `tests/native/artifacts/NativeTests.exe` 생성
- 네이티브 테스트 34건 통과

## 비정상 흐름

- `x64` 솔루션 구성 누락으로 C++ 프로젝트가 빌드되지 않는 경우
- 출력 경로 불일치로 기존 스크립트가 산출물을 찾지 못하는 경우
- .NET 프로젝트 플랫폼 매핑 오류로 솔루션 빌드가 깨지는 경우

## 타임아웃 / 재시도

- 솔루션 빌드 실패 시 단일 프로젝트 빌드로 원인 분리
- 테스트 실행 실패 시 기존 `run_tests.ps1` 재실행으로 회귀 여부 확인

## Bus A/B 전환

- 구조 변경 작업이므로 기능 변경 없음
- 회귀 확인은 기존 네이티브 테스트의 Bus A/B 전환 케이스 통과로 대체

## BC → RT

- 기능 변경 없음
- 기존 네이티브 테스트 회귀로 확인

## RT → BC

- 기능 변경 없음
- 기존 네이티브 테스트 회귀로 확인

## RT ↔ RT

- 기능 변경 없음
- 기존 네이티브 테스트 회귀로 확인

## Mode Code

- 기능 변경 없음
- 기존 Mode Code 테스트 통과로 회귀 확인

## 로그 확인 포인트

- `msbuild` 출력에서 `MilStd1553.Native` 및 `MilStd1553.Native.Tests` 빌드 성공 여부
- `tests/native/run_tests.ps1` 출력에서 전체 테스트 통과 건수

## 결과 요약

- 솔루션에 네이티브 프로젝트가 정상 편입되었는지
- 기존 네이티브 테스트 경로가 유지되는지
