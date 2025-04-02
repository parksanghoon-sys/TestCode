using MessageBusLib.Pub;
using MessageBusLib.Serialization;
using MessageBusLib.Sub;

namespace MessageBusLib;

/// <summary>
/// 메시지 버스 인터페이스
/// </summary>
public interface IMessageBus : IDisposable
{
    /// <summary>
    /// 메시지 수신 이벤트
    /// </summary>
    event EventHandler<IMessageReceivedEventArgs> MessageReceived;

    /// <summary>
    /// 메시지 발행
    /// </summary>
    string Publish(string topic, object data);

    /// <summary>
    /// 지정된 ID로 메시지 발행
    /// </summary>
    void PublishWithId(string messageId, string topic, object data);

    /// <summary>
    /// 토픽 구독
    /// </summary>
    void Subscribe(string topic, MessageHandler handler);

    /// <summary>
    /// 토픽 구독 해제
    /// </summary>
    void Unsubscribe(string topic, MessageHandler handler = null);
}
