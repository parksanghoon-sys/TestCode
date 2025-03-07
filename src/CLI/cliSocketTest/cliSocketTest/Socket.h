#pragma once

#include <winsock2.h>
#include <memory>

enum class Protocol {
    TCP,
    UDP
};

class Socket {
public:
    virtual ~Socket() {}
    virtual int createSocket() = 0;
    virtual int bindSocket(sockaddr_in& addr) = 0;
    virtual int listenSocket() = 0;
    virtual int acceptSocket(sockaddr_in& clientAddr, int& clientAddrLen) = 0;
    virtual int connectSocket(sockaddr_in& addr) = 0;
    virtual int sendData(const char* buffer, int len) = 0;
    virtual int receiveData(char* buffer, int len) = 0;
    virtual void closeSocket() = 0;
public:
    SOCKET sock;
};

class TCPSocket : public Socket {
public:
    int createSocket() override;
    int bindSocket(sockaddr_in& addr) override;
    int listenSocket() override;
    int acceptSocket(sockaddr_in& clientAddr, int& clientAddrLen) override;
    int connectSocket(sockaddr_in& addr) override;
    int sendData(const char* buffer, int len) override;
    int receiveData(char* buffer, int len) override;
    void closeSocket() override;
};

class UDPSocket : public Socket {
public:
    int createSocket() override;
    int bindSocket(sockaddr_in& addr) override;
    int listenSocket() override;
    int acceptSocket(sockaddr_in& clientAddr, int& clientAddrLen) override;
    int connectSocket(sockaddr_in& addr) override;
    int sendData(const char* buffer, int len) override;
    int receiveData(char* buffer, int len) override;
    void closeSocket() override;
};

class SocketFactory {
public:
    static std::unique_ptr<Socket> createSocket(Protocol protocol);
};