#!/bin/bash

echo "🚀 FastMapper 빌드 스크립트"
echo "=========================="

# 변수 설정
CONFIGURATION=${1:-Release}
OUTPUT_DIR="./artifacts"

echo "📦 Configuration: $CONFIGURATION"
echo "📁 Output Directory: $OUTPUT_DIR"

# 아티팩트 디렉토리 생성
mkdir -p $OUTPUT_DIR

# 의존성 복원
echo "📥 의존성 복원 중..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ 의존성 복원 실패"
    exit 1
fi

# 빌드
echo "🔨 빌드 중..."
dotnet build --configuration $CONFIGURATION --no-restore
if [ $? -ne 0 ]; then
    echo "❌ 빌드 실패"
    exit 1
fi

# 테스트 실행
echo "🧪 테스트 실행 중..."
dotnet test --configuration $CONFIGURATION --no-build --verbosity normal
if [ $? -ne 0 ]; then
    echo "❌ 테스트 실패"
    exit 1
fi

# 패키지 생성 (Release 모드일 때만)
if [ "$CONFIGURATION" = "Release" ]; then
    echo "📦 NuGet 패키지 생성 중..."
    dotnet pack --configuration Release --no-build --output $OUTPUT_DIR
    if [ $? -ne 0 ]; then
        echo "❌ 패키지 생성 실패"
        exit 1
    fi
    
    echo "✅ 패키지가 생성되었습니다: $OUTPUT_DIR"
    ls -la $OUTPUT_DIR/*.nupkg
fi

echo "✅ 빌드 완료!"
