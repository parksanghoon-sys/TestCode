namespace CleanProtocolSample.Protocol;

internal sealed class RecentEventStore : IRecentEventStore
{
    private readonly RingBuffer<ProtocolMessage> _buffer = new RingBuffer<ProtocolMessage>(24);
    public void Add(ProtocolMessage message)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        _buffer.Add(message);
    }
    public IReadOnlyList<ProtocolMessage> Snapshot() 
    { 
        return _buffer.Snapshot(); 
    }
}