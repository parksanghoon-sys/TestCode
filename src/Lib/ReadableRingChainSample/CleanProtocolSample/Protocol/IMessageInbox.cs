namespace CleanProtocolSample.Protocol;

internal interface IMessageInbox
{
    Task<ProtocolMessage> WaitAsync(Func<ProtocolMessage, bool> predicate, CancellationToken cancellationToken);
}
