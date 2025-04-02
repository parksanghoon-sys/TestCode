namespace MessageBusLib.Pub;

/// <summary>
/// RPC 응답 데이터
/// </summary>
[Serializable]
public class RpcResponse
{
    /// <summary>
    /// 응답 데이터
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// 응답 타입 정보
    /// </summary>
    public string ResponseTypeName { get; set; }

    /// <summary>
    /// 오류 메시지 (성공 시 null)
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
}
