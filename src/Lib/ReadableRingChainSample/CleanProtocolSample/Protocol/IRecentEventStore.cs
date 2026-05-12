namespace CleanProtocolSample.Protocol;

internal interface IRecentEventStore
{
    void Add(ProtocolMessage message);

    IReadOnlyList<ProtocolMessage> Snapshot();
}
internal interface IHeartbeatWatchdog
{
    DateTime LastBeatUtc { get; }

    void Beat();
}
internal sealed class HeartbeatWatchdog
    : IHeartbeatWatchdog
{
    public DateTime LastBeatUtc { get; private set; }
        = DateTime.UtcNow;

    public void Beat()
    {
        LastBeatUtc = DateTime.UtcNow;
    }
}