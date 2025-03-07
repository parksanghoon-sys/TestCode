#include "Socket.h"
#include <iostream>
#include <iomanip> // std::hex, std::setw

#pragma comment(lib, "ws2_32.lib")

int TCPSocket::createSocket() {
    sock = socket(AF_INET, SOCK_STREAM, 0);
    if (sock == INVALID_SOCKET) {
        std::cerr << "TCP Socket creation failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int TCPSocket::bindSocket(sockaddr_in& addr) {
    if (bind(sock, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        std::cerr << "TCP Bind failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int TCPSocket::listenSocket() {
    if (listen(sock, SOMAXCONN) == SOCKET_ERROR) {
        std::cerr << "TCP Listen failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int TCPSocket::acceptSocket(sockaddr_in& clientAddr, int& clientAddrLen) {
    return accept(sock, (sockaddr*)&clientAddr, &clientAddrLen);
}

int TCPSocket::connectSocket(sockaddr_in& addr) {
    if (connect(sock, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        std::cerr << "TCP Connect failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int TCPSocket::sendData(const char* buffer, int len) {
    return send(sock, buffer, len, 0);
}

int TCPSocket::receiveData(char* buffer, int len) {    
    int receivedBytes = recv(sock, buffer, len, 0);
    if (receivedBytes > 0) {
        // 수신된 데이터를 배열로 처리
        for (int i = 0; i < receivedBytes; ++i) {
            std::cout << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(static_cast<unsigned char>(buffer[i])) << " ";
        }
        // 0~len 범위까지 확인하고싶다면 receivedBytes가 len보다 작을수도있다는걸 감안해야합니다.
        for (int i = 0; i < len; ++i) {
            // buffer[i]를 사용하여 각 바이트에 접근하고 필요한 작업을 수행
            std::cout << "check byte " << i << ": " << static_cast<int>(buffer[i]) << std::endl;
        }

    }
    return receivedBytes;
}

void TCPSocket::closeSocket() {
    closesocket(sock);
}

int UDPSocket::createSocket() {
    sock = socket(AF_INET, SOCK_DGRAM, 0);
    if (sock == INVALID_SOCKET) {
        std::cerr << "UDP Socket creation failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int UDPSocket::bindSocket(sockaddr_in& addr) {
    if (bind(sock, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        std::cerr << "UDP Bind failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int UDPSocket::listenSocket() {
    std::cerr << "UDP does not support listen." << std::endl;
    return -1;
}

int UDPSocket::acceptSocket(sockaddr_in& clientAddr, int& clientAddrLen) {
    std::cerr << "UDP does not support accept." << std::endl;
    return -1;
}

int UDPSocket::connectSocket(sockaddr_in& addr) {
    if (connect(sock, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        std::cerr << "UDP Connect failed: " << WSAGetLastError() << std::endl;
        return -1;
    }
    return 0;
}

int UDPSocket::sendData(const char* buffer, int len) {
    sockaddr_in addr;
    int addrLen = sizeof(addr);
    getpeername(sock, (sockaddr*)&addr, &addrLen);
    return sendto(sock, buffer, len, 0, (sockaddr*)&addr, sizeof(addr));
}

int UDPSocket::receiveData(char* buffer, int len) {
    sockaddr_in addr;
    int addrLen = sizeof(addr);
    return recvfrom(sock, buffer, len, 0, (sockaddr*)&addr, &addrLen);
}

void UDPSocket::closeSocket() {
    closesocket(sock);
}

std::unique_ptr<Socket> SocketFactory::createSocket(Protocol protocol) {
    if (protocol == Protocol::TCP) {
        return std::make_unique<TCPSocket>();
    }
    else if (protocol == Protocol::UDP) {
        return std::make_unique<UDPSocket>();
    }
    return nullptr;
}