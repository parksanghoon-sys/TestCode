//#include <iostream>
//#include <fstream>
//#include <vector>
//#include <string>
//#include <algorithm>
//
//int main() {
//    std::string filePath = "D:/MyStudy/03.TEST/src/CLI/cliSocketTest/cliFileReadTest/Debug/test.txt"; // 수정된 파일 경로
//    std::ifstream file(filePath);
//
//    if (file.is_open()) {
//        std::string line;
//        while (std::getline(file, line)) {
//            unsigned char* buffer = (unsigned char*)malloc(static_cast<unsigned int>(line.length() + 1));
//            std::memcpy(buffer, &line,strlen(line.c_str()));
//            std::vector<char> charVector(line.begin(), line.end());
//            // charVector를 사용하여 필요한 작업 수행
//            // 예: 각 문자를 출력
//            for (char c : charVector) {
//                std::cout << c;
//            }
//            std::cout << std::endl;
//        }
//        file.close();
//    }
//    else {
//        std::cerr << "파일을 열 수 없습니다." << std::endl;
//    }
//
//    return 0;
//}
//#include <iostream>
//
//int main() {
//    unsigned int test = 1;
//    char* c = (char*)&test;
//
//    if (*c) {
//        std::cout << "리틀 엔디안(Little-endian)" << std::endl;
//    }
//    else {
//        std::cout << "빅 엔디안(Big-endian)" << std::endl;
//    }
//
//    return 0;
//}
//#include <iostream>
//#include <cstdint>
//#include <array>
//#include <iomanip>
//
//uint32_t littleEndianToBigEndian(uint32_t value) {
//    return ((value >> 24) & 0xFF) |
//        ((value >> 8) & 0xFF00) |
//        ((value << 8) & 0xFF0000) |
//        ((value << 24) & 0xFF000000);
//}
//
//int main() {
//    std::array<uint32_t, 3> littleEndianArray = { 0x12345678, 0xABCDEF01, 0x98765432 };
//    std::array<uint32_t, 3> bigEndianArray;
//
//    std::cout << "리틀 엔디안 배열:" << std::endl;
//    for (uint32_t value : littleEndianArray) {
//        std::cout << "0x" << std::hex << std::setw(8) << std::setfill('0') << value << std::endl;
//    }
//
//    // 배열의 각 요소를 빅 엔디안으로 변환
//    for (size_t i = 0; i < littleEndianArray.size(); ++i) {
//        bigEndianArray[i] = littleEndianToBigEndian(littleEndianArray[i]);
//    }
//
//    std::cout << "\n빅 엔디안 배열:" << std::endl;
//    for (uint32_t value : bigEndianArray) {
//        std::cout << "0x" << std::hex << std::setw(8) << std::setfill('0') << value << std::endl;
//    }
//
//    return 0;
//}
#include <iostream>
#include <cstring> // strlen 사용을 위한 헤더

void readString(const char* data) {
    if (data == nullptr) {
        std::cout << "데이터가 없습니다." << std::endl;
        return;
    }

    std::cout << "문자열: " << data << std::endl;

    // 문자열 길이 확인 (null 문자 제외)
    size_t length = std::strlen(data);
    std::cout << "문자열 길이: " << length << std::endl;

    // 각 문자 출력
    std::cout << "각 문자: ";
    for (size_t i = 0; i < length; ++i) {
        std::cout << data[i] << " ";
    }
    std::cout << std::endl;
}
int main() {
    int value = 0x12345678;
    int* address = &value; // value의 주소를 address 포인터에 저장
    char* bytePtr = reinterpret_cast<char*>(&value); // value의 주소를 char*로 형변환
    const char* str = "Hello, world!";
    readString(str);

    std::cout << "Value: " << value << std::endl;
    return 0; // 중단점 설정
}
