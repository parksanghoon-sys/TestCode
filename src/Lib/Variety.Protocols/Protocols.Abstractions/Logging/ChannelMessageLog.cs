using Protocols.Abstractions.Channels;

namespace Protocols.Abstractions.Logging
{
    /// <summary>
    /// 통신 채널을 통해 주고받은 메시지에 대한 Log
    /// </summary>
    public abstract class ChannelMessageLog : ChannelLog
    {
        /// <summary>
        /// 생성자 
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="message">프로토콜 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        protected ChannelMessageLog(IChannel channel, IProtocolMessage message, byte[] rawMessage)
            :base(channel)
        {
            Message = message;
            RawMessage = rawMessage ?? new byte[0];
        }
        public IProtocolMessage Message { get; }
        public IReadOnlyList<byte> RawMessage { get; }
        public override string ToString()
        {
            return BitConverter.ToString(RawMessage as byte[]).Replace('-', ' ');
        }
    }
}