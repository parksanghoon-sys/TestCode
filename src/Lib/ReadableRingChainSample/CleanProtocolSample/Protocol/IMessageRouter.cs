using System.Threading.Channels;

namespace CleanProtocolSample.Protocol;

internal interface IMessageRouter
{
    ChannelReader<ProtocolMessage> Subscribe(string code);
}
