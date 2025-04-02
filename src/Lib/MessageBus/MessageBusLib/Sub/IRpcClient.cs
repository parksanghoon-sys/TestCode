namespace MessageBusLib.Sub;

/// <summary>
/// RPC 클라이언트 인터페이스
/// </summary>
public interface IRpcClient : IDisposable
{
    /// <summary>
    /// RPC 호출
    /// </summary>
    Task<TResult> CallAsync<TResult>(string serviceTopic, object data);
}
