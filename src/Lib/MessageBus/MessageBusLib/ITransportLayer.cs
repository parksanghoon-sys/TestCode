using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MessageBusLib;

/// <summary>
/// 메시지 전송 메커니즘을 위한 추상 인터페이스
/// </summary>
public interface ITransportLayer : IDisposable
{
    /// <summary>
    /// 메시지 전송
    /// </summary>
    void SendMessage(byte[] data);

    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    event EventHandler<TransportMessageReceivedEventArgs> MessageReceived;

    /// <summary>
    /// 전송 계층 시작
    /// </summary>
    void Start();

    /// <summary>
    /// 전송 계층 중지
    /// </summary>
    void Stop();
}
