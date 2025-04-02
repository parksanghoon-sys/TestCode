namespace MessageBusLib.Sub;

/// <summary>
/// RPC 클라이언트 구성 옵션
/// </summary>
public class RpcClientOptions
{
    /// <summary>
    /// 응답 대기 시간 (밀리초)
    /// </summary>
    public int Timeout { get; set; } = 30000; // 30초
}