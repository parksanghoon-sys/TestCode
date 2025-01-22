# Python 3.12 Slim 버전을 베이스로 사용
FROM python:3.12-slim

# 필요한 패키지 설치
RUN apt-get update && apt-get install -y \
    git \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# 작업 디렉토리 설정
WORKDIR /app

# 기본 커맨드 설정 (컨테이너 시작 시 bash 실행)
CMD ["bash"]