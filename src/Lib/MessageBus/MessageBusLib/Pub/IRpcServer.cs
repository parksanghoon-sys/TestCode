namespace MessageBusLib.Pub;

/// <summary>
/// RPC 서버 인터페이스
/// </summary>
public interface IRpcServer : IDisposable
{
    /// <summary>
    /// 서비스 토픽
    /// </summary>
    string ServiceTopic { get; }
}