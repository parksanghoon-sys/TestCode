using System.Threading.Channels;

namespace CleanProtocolSample.Protocol;

internal sealed class MessageInbox : IMessageInbox
{
    private readonly Channel<ProtocolMessage> _channel = Channel.CreateUnbounded<ProtocolMessage>();
    public async Task<ProtocolMessage> WaitAsync(Func<ProtocolMessage, bool> predicate, CancellationToken cancellationToken)
    {
        await foreach(var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (predicate(message))
            {
                return message;
            }
        }
        throw new OperationCanceledException();
    }
    public async Task PublishAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
