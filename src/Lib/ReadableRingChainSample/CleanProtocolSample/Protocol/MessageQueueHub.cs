using System.Drawing;
using System.Threading.Channels;

namespace CleanProtocolSample.Protocol;
/// <summary>
/// 송수신 queue 및 최근 이력 관리.
/// </summary>
internal sealed class MessageQueueHub
{
    /// <summary>
    /// 송신 queue.
    /// </summary>
    public Channel<ProtocolMessage> SendQueue { get; }

    /// <summary>
    /// 수신 queue.
    /// </summary>
    public Channel<ProtocolMessage> ReceiveQueue { get; }

    /// <summary>
    /// 최근 송신 이력.
    /// </summary>
    public RingBuffer<ProtocolMessage> RecentSent { get; }

    /// <summary>
    /// 최근 수신 이력.
    /// </summary>
    public RingBuffer<ProtocolMessage> RecentReceived { get; }
    public MessageQueueHub(int historySize)
    {
        SendQueue = Channel.CreateUnbounded<ProtocolMessage>();
        ReceiveQueue = Channel.CreateUnbounded<ProtocolMessage>();

        RecentSent = new RingBuffer<ProtocolMessage>(historySize);
        RecentReceived = new RingBuffer<ProtocolMessage>(historySize);
    }

}
