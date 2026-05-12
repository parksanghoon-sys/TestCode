# 솔루션 native project 추가 테스트 결과

## 테스트 대상

- `MIL-STD-1553B.sln`
- `src/native/MilStd1553.Native.vcxproj`
- `tests/native/MilStd1553.Native.Tests.vcxproj`

## 테스트 환경

- Windows PowerShell
- Visual Studio 2022 Build Tools / MSVC v143
- 작업 경로: `D:\01_MyStudy\03.TEST\src\MIL-STD-1553B`

## 테스트 시나리오

- `Debug|x64` 솔루션 빌드
- 기존 네이티브 테스트 스크립트 재실행

## 입력 조건

- 솔루션에 네이티브 프로젝트 2개 추가
- 기존 `tests/native/artifacts` 출력 경로 유지

## 기대 결과

- `msbuild`로 솔루션 빌드 성공
- `NativeTests.exe` 및 `MilStd1553.Native.dll` 생성
- 기존 네이티브 테스트 전체 통과

## 실제 결과

- `C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe .\MIL-STD-1553B.sln /t:Build /p:Configuration=Debug /p:Platform=x64` 실행 성공
- `MilStd1553.Native.vcxproj`와 `MilStd1553.Native.Tests.vcxproj`가 솔루션에서 함께 빌드되었다.
- `tests/native/artifacts/MilStd1553.Native.dll` 생성 확인
- `tests/native/artifacts/NativeTests.exe` 생성 확인
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1` 재실행 결과 네이티브 테스트 34건 통과

## 로그 확인 포인트

- `msbuild MIL-STD-1553B.sln /p:Configuration=Debug /p:Platform=x64`
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\native\run_tests.ps1`

## 실패 케이스

- 초기 검증에서 `MSBuild` 기본 경로가 PATH에 없어 직접 경로를 사용했다.
- 초기 검증에서 VC++ 프로젝트가 `<codecvt>` deprecation 경고를 오류로 처리해 빌드가 실패했고, 프로젝트 전처리기 정의에 `_SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING`를 추가해 해결했다.

## 리스크

- VC++ 프로젝트의 기본 링커/출력 설정이 기존 스크립트 경로와 다를 수 있음
- 솔루션 플랫폼 매핑이 일부 환경에서 다르게 보일 수 있음

## 후속 조치

- README와 상태 문서에 솔루션 기반 네이티브 빌드 경로를 반영한다.
