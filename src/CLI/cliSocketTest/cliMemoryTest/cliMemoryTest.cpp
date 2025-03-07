#include <iostream>
#include <cstring>
#include <cstdlib>

int main() {
    unsigned char* buffer = (unsigned char*)malloc(10);
    unsigned char* data = (unsigned char*)malloc(10);

    // buffer에 데이터 저장
    for (int i = 0; i < 10; ++i) {
        buffer[i] = i;
    }

    // buffer의 내용을 data에 복사
    memcpy(data, buffer, 10);

    // buffer 메모리 해제
    free(buffer);

    // data는 여전히 buffer의 내용을 가지고 있지만, 더 이상 안전하지 않음
    for (int i = 0; i < 10; ++i) {
        std::cout << (int)data[i] << " "; // 출력: 0 1 2 3 4 5 6 7 8 9 (일반적으로는 유지되지만, 보장되지 않음)
    }
    std::cout << std::endl;

    // data 메모리 해제
    free(data);

    return 0;
}