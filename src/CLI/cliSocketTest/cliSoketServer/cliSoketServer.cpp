
#include <iostream>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <thread>
#include "../cliSocketTest/Socket.h"

#pragma comment(lib, "ws2_32.lib")

void handleTCPClient(std::unique_ptr<Socket> clientSocket) {
    char buffer[1024];
    int bytesReceived;

    while (true) {
        bytesReceived = clientSocket->receiveData(buffer, sizeof(buffer));
        if (bytesReceived > 0) {
            buffer[bytesReceived] = '\0';
            std::cout << "Received (TCP): " << buffer << std::endl;

            // 클라이언트에게 다시 데이터 전송
            clientSocket->sendData(buffer, bytesReceived);
        }
        else if (bytesReceived == 0) {
            std::cout << "TCP Client disconnected." << std::endl;
            break;
        }
        else {
            std::cerr << "TCP Receive failed: " << WSAGetLastError() << std::endl;
            break;
        }
    }
}

void handleUDPClient(std::unique_ptr<Socket> serverSocket, sockaddr_in clientAddr) {
    char buffer[1024];
    int bytesReceived;

    while (true) {
        bytesReceived = serverSocket->receiveData(buffer, sizeof(buffer));
        if (bytesReceived > 0) {
            buffer[bytesReceived] = '\0';
            std::cout << "Received (UDP): " << buffer << std::endl;

            // 클라이언트에게 다시 데이터 전송
            serverSocket->sendData(buffer, bytesReceived);
        }
        else if (bytesReceived == 0) {
            std::cout << "UDP Client disconnected." << std::endl;
            break;
        }
        else {
            std::cerr << "UDP Receive failed: " << WSAGetLastError() << std::endl;
            break;
        }
    }
}

int main() {
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cerr << "WSAStartup failed." << std::endl;
        return 1;
    }

    Protocol protocol = Protocol::TCP; // 또는 Protocol::UDP
    std::unique_ptr<Socket> serverSocket = SocketFactory::createSocket(protocol);

    if (!serverSocket) {
        std::cerr << "Socket creation failed." << std::endl;
        WSACleanup();
        return 1;
    }

    sockaddr_in serverAddr, clientAddr;
    int clientAddrLen = sizeof(clientAddr);

    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_port = htons(12345);

    if (serverSocket->createSocket() != 0 || serverSocket->bindSocket(serverAddr) != 0) {
        WSACleanup();
        return 1;
    }

    if (protocol == Protocol::TCP) {
        if (serverSocket->listenSocket() != 0) {
            WSACleanup();
            return 1;
        }

        while (true) {
            SOCKET client = serverSocket->acceptSocket(clientAddr, clientAddrLen);
            if (client == INVALID_SOCKET) {
                std::cerr << "Accept failed: " << WSAGetLastError() << std::endl;
                continue;
            }

            std::cout << "TCP Client connected." << std::endl;

            std::unique_ptr<Socket> clientSocket = SocketFactory::createSocket(protocol);
            if (clientSocket) {
                TCPSocket* tcpClientSocket = dynamic_cast<TCPSocket*>(clientSocket.get());
                if (tcpClientSocket) {
                    tcpClientSocket->sock = client;
                    std::thread(handleTCPClient, std::move(clientSocket)).detach();
                }
            }
        }
    }
    else if (protocol == Protocol::UDP) {
        while (true) {
            handleUDPClient(std::move(serverSocket), clientAddr);
            serverSocket = SocketFactory::createSocket(protocol);
            serverSocket->createSocket();
            serverSocket->bindSocket(serverAddr);
        }
    }

    serverSocket->closeSocket();
    WSACleanup();

    return 0;
}