namespace CleanProtocolSample.Protocol;

internal class SequentialCorrelationIdGenerator : ICorrelationIdGenerator
{
    private int _value;
    public int Next()
    {
        return Interlocked.Increment(ref _value);
    }
}
