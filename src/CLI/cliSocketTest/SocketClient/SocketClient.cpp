
#include <iostream>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <string>
#include "../cliSocketTest/Socket.h"

#pragma comment(lib, "ws2_32.lib")

int main(int argc, char* argv[]) {
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cerr << "WSAStartup failed." << std::endl;
        return 1;
    }

    Protocol protocol = Protocol::TCP; // 기본값은 TCP
    if (argc > 1 && std::string(argv[1]) == "udp") {
        protocol = Protocol::UDP;
    }

    std::unique_ptr<Socket> clientSocket = SocketFactory::createSocket(protocol);

    if (!clientSocket) {
        std::cerr << "Socket creation failed." << std::endl;
        WSACleanup();
        return 1;
    }

    sockaddr_in serverAddr;
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_port = htons(12345);

    if (inet_pton(AF_INET, "127.0.0.1", &(serverAddr.sin_addr)) <= 0) {
        std::cerr << "Invalid address" << std::endl;
        WSACleanup();
        return 1;
    }

    if (clientSocket->createSocket() != 0 || clientSocket->connectSocket(serverAddr) != 0) {
        WSACleanup();
        return 1;
    }

    std::string message;
    char buffer[1024];
    int bytesReceived;

    while (true) {
        std::cout << "Enter message: ";
        std::getline(std::cin, message);

        if (message == "exit") {
            break;
        }

        if (clientSocket->sendData(message.c_str(), message.length()) == SOCKET_ERROR) {
            std::cerr << "Send failed: " << WSAGetLastError() << std::endl;
            break;
        }

        bytesReceived = clientSocket->receiveData(buffer, sizeof(buffer));
        if (bytesReceived > 0) {
            buffer[bytesReceived] = '\0';
            if (protocol == Protocol::TCP) {
                std::cout << "Received (TCP): " << buffer << std::endl;
            }
            else {
                std::cout << "Received (UDP): " << buffer << std::endl;
            }
        }
        else if (bytesReceived == 0) {
            std::cout << "Server disconnected." << std::endl;
            break;
        }
        else {
            std::cerr << "Receive failed: " << WSAGetLastError() << std::endl;
            break;
        }
    }

    clientSocket->closeSocket();
    WSACleanup();

    return 0;
}