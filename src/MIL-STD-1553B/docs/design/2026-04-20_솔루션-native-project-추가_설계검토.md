# 솔루션 native project 추가 설계 검토

## 배경

현재 저장소의 네이티브 C++ 코드는 `tests/native/run_tests.ps1`와 `build_native_artifact.ps1`를 통해서만 빌드할 수 있고, `MIL-STD-1553B.sln`에는 .NET 프로젝트만 포함되어 있다.
이 상태에서는 Visual Studio에서 솔루션을 열어도 네이티브 계층의 소스 집합과 산출물을 직접 탐색하거나 빌드하기 어렵다.

## 목표

- `MIL-STD-1553B.sln`에 네이티브 DLL 프로젝트와 네이티브 테스트 프로젝트를 추가한다.
- 기존 PowerShell 빌드 경로와 충돌하지 않도록 동일한 산출물 이름과 위치를 유지한다.
- .NET 중심 솔루션 구조를 유지하면서 `src/native`, `tests/native`를 Visual Studio에서 함께 다룰 수 있게 만든다.

## 범위

- `src/native`용 Visual C++ DLL 프로젝트 추가
- `tests/native`용 Visual C++ 테스트 실행 파일 프로젝트 추가
- `.sln`에 native solution folder 및 x64 구성 추가
- 구조 변경에 따른 문서와 상태 파일 갱신

## 제외 범위

- CMake 전환
- GoogleTest 등 외부 테스트 프레임워크 도입
- 기존 `run_tests.ps1`를 MSBuild 기반으로 재작성
- 네이티브 코드 기능 변경

## 용어 정의

- 네이티브 DLL 프로젝트: `MilStd1553.Native.dll`을 생성하는 C++ 프로젝트
- 네이티브 테스트 프로젝트: `NativeTests.exe`를 생성하고 `tests/native/NativeTests.cpp`를 실행하는 C++ 프로젝트

## 요구사항

- Visual Studio 2022 기준으로 솔루션에서 C++ 프로젝트가 로드되어야 한다.
- 네이티브 프로젝트는 `x64` 구성으로 빌드 가능해야 한다.
- DLL 산출물은 기존과 동일하게 `tests/native/artifacts/MilStd1553.Native.dll`에 생성되어야 한다.
- 테스트 실행 파일은 기존과 동일하게 `tests/native/artifacts/NativeTests.exe`에 생성되어야 한다.
- 기존 .NET 프로젝트는 계속 `Any CPU` 구성으로 유지하되, 솔루션 전체에서는 `x64` 구성을 함께 제공해야 한다.

## 책임 분리

### C++

- `src/native/MilStd1553.Native.vcxproj`: 네이티브 런타임 DLL 빌드
- `tests/native/MilStd1553.Native.Tests.vcxproj`: 네이티브 테스트 실행 파일 빌드

### C#

- 변경 없음
- 기존 `MilStd1553.Host`, `MilStd1553.Interop`, `MilStd1553.Cli` 책임 유지

## 레이어 영향도

- Domain: 변경 없음
- Application: 변경 없음
- Infrastructure: 변경 없음
- Presentation/Host: 변경 없음
- Build/Solution 구성: 변경 있음

## 클래스/인터페이스 설계 초안

- 새 런타임 클래스나 인터페이스는 추가하지 않는다.
- 프로젝트 파일이 기존 소스 집합을 그대로 참조한다.

## 데이터 흐름 또는 시퀀스

1. 개발자가 `MIL-STD-1553B.sln`을 Visual Studio로 연다.
2. `src/native/MilStd1553.Native` 프로젝트를 빌드하면 `MilStd1553.Native.dll`이 생성된다.
3. `tests/native/MilStd1553.Native.Tests` 프로젝트를 빌드하면 `NativeTests.exe`가 생성된다.
4. 기존 `run_tests.ps1`는 동일한 산출물 위치를 계속 사용할 수 있다.

## 테스트 전략

- `msbuild MIL-STD-1553B.sln /p:Configuration=Debug /p:Platform=x64`로 솔루션 빌드 검증
- `tests/native/run_tests.ps1` 재실행으로 기존 네이티브 테스트 경로 회귀 확인
- 필요 시 `NativeTests.exe` 직접 실행 가능 여부 확인

## 리스크 및 대응

- 리스크: 솔루션 구성에 `x64`가 추가되면서 .NET 프로젝트 플랫폼 매핑이 꼬일 수 있다.
  대응: .NET 프로젝트는 `x64 -> Any CPU`로 명시 매핑한다.
- 리스크: 프로젝트 파일과 스크립트가 서로 다른 산출물 경로를 쓰면 혼란이 생길 수 있다.
  대응: 기존 `tests/native/artifacts` 경로를 그대로 사용한다.
- 리스크: PowerShell 빌드와 MSBuild 빌드의 컴파일 옵션 차이로 동작이 달라질 수 있다.
  대응: C++20, 유니코드, `/utf-8` 등 핵심 옵션을 동일하게 맞춘다.

## 완료 기준

- `MIL-STD-1553B.sln`에서 네이티브 프로젝트 2개가 보인다.
- `x64` 구성으로 솔루션 빌드가 성공한다.
- `tests/native/run_tests.ps1`가 계속 성공한다.
- 관련 테스트 문서와 상태 문서가 현재 구조를 반영한다.
