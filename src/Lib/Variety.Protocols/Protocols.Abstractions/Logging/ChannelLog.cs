using Protocols.Abstractions.Channels;

namespace Protocols.Abstractions.Logging
{
    public abstract class ChannelLog
    {
        public DateTime TimeStamp { get; }
        public string ChnnelDescription { get; }
        protected ChannelLog(IChannel channel)
        {
            TimeStamp = DateTime.UtcNow;
            ChnnelDescription = channel.Description;
        }
    }
}