using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CleanProtocolSample.Protocol;

internal sealed class MessageRouter : IMessageRouter
{
    private readonly ConcurrentDictionary<string, Channel<ProtocolMessage>> _channels = new();
    public ChannelReader<ProtocolMessage> Subscribe(string code)
    {
        return _channels.GetOrAdd(code, _ => Channel.CreateUnbounded<ProtocolMessage>()).Reader;
    }
    public async Task PublishAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        if (_channels.TryGetValue(message.Code,out var channel))
        {
            await channel.Writer.WriteAsync(message, cancellationToken);
        }
    }
}