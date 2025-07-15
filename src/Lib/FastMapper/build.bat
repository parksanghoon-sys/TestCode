@echo off
setlocal

echo 🚀 FastMapper 빌드 스크립트
echo ==========================

:: 변수 설정
set CONFIGURATION=%~1
if "%CONFIGURATION%"=="" set CONFIGURATION=Release
set OUTPUT_DIR=./artifacts

echo 📦 Configuration: %CONFIGURATION%
echo 📁 Output Directory: %OUTPUT_DIR%

:: 아티팩트 디렉토리 생성
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

:: 의존성 복원
echo 📥 의존성 복원 중...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ❌ 의존성 복원 실패
    exit /b 1
)

:: 빌드
echo 🔨 빌드 중...
dotnet build --configuration %CONFIGURATION% --no-restore
if %ERRORLEVEL% neq 0 (
    echo ❌ 빌드 실패
    exit /b 1
)

:: 테스트 실행
echo 🧪 테스트 실행 중...
dotnet test --configuration %CONFIGURATION% --no-build --verbosity normal
if %ERRORLEVEL% neq 0 (
    echo ❌ 테스트 실패
    exit /b 1
)

:: 패키지 생성 (Release 모드일 때만)
if "%CONFIGURATION%"=="Release" (
    echo 📦 NuGet 패키지 생성 중...
    dotnet pack --configuration Release --no-build --output %OUTPUT_DIR%
    if %ERRORLEVEL% neq 0 (
        echo ❌ 패키지 생성 실패
        exit /b 1
    )
    
    echo ✅ 패키지가 생성되었습니다: %OUTPUT_DIR%
    dir %OUTPUT_DIR%\*.nupkg
)

echo ✅ 빌드 완료!
