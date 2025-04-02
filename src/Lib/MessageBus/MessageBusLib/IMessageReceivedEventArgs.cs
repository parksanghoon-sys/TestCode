using MessageBusLib.Messages;

namespace MessageBusLib;

/// <summary>
/// 메시지 핸들러 대리자
/// </summary>
/// <param name="sender"></param>
/// <param name="args"></param>
public delegate void MessageHandler(object sender, IMessageReceivedEventArgs args);
/// <summary>
/// 메시지 수신 이벤트 인자 인터페이스
/// </summary>
public interface IMessageReceivedEventArgs
{
    /// <summary>
    /// 수신된 메시지
    /// </summary>
    IMessage Message { get; }
}
/// <summary>
/// 메시지 수신 이벤트 인자
/// </summary>
public class MessageReceivedEventArgs : EventArgs, IMessageReceivedEventArgs
{
    public IMessage Message { get; }

    public MessageReceivedEventArgs(IMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }
}