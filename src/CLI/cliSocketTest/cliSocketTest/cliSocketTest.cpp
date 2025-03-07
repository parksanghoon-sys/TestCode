//#include "Socket.h"
//#include <iostream>
//#include <winsock2.h>
//#include <ws2tcpip.h>
//
//#pragma comment(lib, "ws2_32.lib")
//
//int main() {
//    WSADATA wsaData;
//    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
//        std::cerr << "WSAStartup failed." << std::endl;
//        return 1;
//    }
//
//    Protocol protocol = Protocol::TCP; // 또는 Protocol::UDP
//    std::unique_ptr<Socket> serverSocket = SocketFactory::createSocket(protocol);
//    std::unique_ptr<Socket> clientSocket = SocketFactory::createSocket(protocol);
//
//    if (!serverSocket || !clientSocket) {
//        std::cerr << "Socket creation failed." << std::endl;
//        WSACleanup();
//        return 1;
//    }
//
//    sockaddr_in serverAddr, clientAddr;
//    int clientAddrLen = sizeof(clientAddr);
//
//    serverAddr.sin_family = AF_INET;
//    serverAddr.sin_addr.s_addr = INADDR_ANY;
//    serverAddr.sin_port = htons(12345);
//
//    clientAddr.sin_family = AF_INET;
//    //clientAddr.sin_addr.s_addr = inet_addr("127.0.0.1"); // 서버와 동일한 IP
//    clientAddr.sin_port = htons(12345);
//
//    if (inet_pton(AF_INET, "127.0.0.1", &(clientAddr.sin_addr)) <= 0) {
//        std::cerr << "Invalid address" << std::endl;
//        WSACleanup();
//        return 1;
//    }
//
//    if (serverSocket->createSocket() != 0 || serverSocket->bindSocket(serverAddr) != 0) {
//        WSACleanup();
//        return 1;
//    }
//
//    if (protocol == Protocol::TCP) {
//        if (serverSocket->listenSocket() != 0) {
//            WSACleanup();
//            return 1;
//        }
//
//        SOCKET client = serverSocket->acceptSocket(clientAddr, clientAddrLen);
//        if (client == INVALID_SOCKET) {
//            std::cerr << "Accept failed: " << WSAGetLastError() << std::endl;
//            WSACleanup();
//            return 1;
//        }
//
//        clientSocket->sock = client; // 클라이언트 소켓에 연결된 소켓할당
//    }
//    else if (protocol == Protocol::UDP) {
//        if (clientSocket->createSocket() != 0 || clientSocket->connectSocket(serverAddr) != 0) {
//            WSACleanup();
//            return 1;
//        }
//        serverSocket->connectSocket(clientAddr);
//    }
//
//    const char* message = "Hello from ";
//    char buffer[1024];
//    int bytesReceived;
//
//    if (protocol == Protocol::TCP) {
//        if (clientSocket->sendData(message, strlen(message)) == SOCKET_ERROR) {
//            std::cerr << "Send failed: " << WSAGetLastError() << std::endl;
//            WSACleanup();
//            return 1;
//        }
//
//        bytesReceived = clientSocket->receiveData(buffer, sizeof(buffer));
//        if (bytesReceived > 0) {
//            buffer[bytesReceived] = '\0';
//            std::cout << "Received (TCP): " << buffer << std::endl;
//        }
//    }
//    else if (protocol == Protocol::UDP) {
//        if (clientSocket->sendData(message, strlen(message)) == SOCKET_ERROR) {
//            std::cerr << "Send failed: " << WSAGetLastError() << std::endl;
//            WSACleanup();
//            return 1;
//        }
//
//        bytesReceived = serverSocket->receiveData(buffer, sizeof(buffer));
//        if (bytesReceived > 0) {
//            buffer[bytesReceived] = '\0';
//            std::cout << "Received (UDP): " << buffer << std::endl;
//        }
//    }
//
//    serverSocket->closeSocket();
//    clientSocket->closeSocket();
//    WSACleanup();
//
//    return 0;
//}