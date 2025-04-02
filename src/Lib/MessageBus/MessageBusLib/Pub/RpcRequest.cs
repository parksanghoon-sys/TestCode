namespace MessageBusLib.Pub;

/// <summary>
/// RPC 요청 데이터
/// </summary>
[Serializable]
public class RpcRequest
{
    /// <summary>
    /// 요청 데이터
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// 응답 토픽
    /// </summary>
    public string ReplyTopic { get; set; }

    /// <summary>
    /// 요청 타입 정보
    /// </summary>
    public string RequestTypeName { get; set; }
}
