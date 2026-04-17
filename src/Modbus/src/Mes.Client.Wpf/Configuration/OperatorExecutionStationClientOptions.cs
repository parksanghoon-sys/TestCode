namespace Mes.Client.Wpf.Configuration;

/// <summary>
/// 스테이션 셸이 operator-execution BFF와 연결될 때 사용하는 구성 값을 나타냅니다.
/// </summary>
public sealed class OperatorExecutionStationClientOptions
{
    /// <summary>
    /// BFF 기본 주소를 가져오거나 설정합니다.
    /// </summary>
    public string BaseAddress { get; set; } = "http://localhost:51398/";

    /// <summary>
    /// 셸이 시작할 때 기본값으로 채울 스테이션 식별자를 가져오거나 설정합니다.
    /// </summary>
    public string? DefaultStationId { get; set; }

    /// <summary>
    /// 인증 연동 전까지 WPF 스테이션 명령에서 사용할 기본 작업자 식별자를 가져오거나 설정합니다.
    /// </summary>
    public string DefaultActorId { get; set; } = "operator.demo";

    /// <summary>
    /// 선택된 queue item이 없을 때 completion 패널에 보여 줄 fallback 수량 단위를 가져오거나 설정합니다.
    /// </summary>
    public string DefaultCompletionQuantityUnit { get; set; } = "EA";

    /// <summary>
    /// 구성의 기본 주소를 검증된 절대 URI로 변환합니다.
    /// </summary>
    /// <returns>검증이 끝난 절대 URI입니다.</returns>
    public Uri ResolveBaseUri()
    {
        if (!Uri.TryCreate(BaseAddress, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Mes:OperatorExecutionBff:BaseAddress must be a valid absolute URI.");
        }

        return uri;
    }

    /// <summary>
    /// 구성의 기본 작업자 식별자를 검증한 값으로 반환합니다.
    /// </summary>
    /// <returns>trim 처리된 기본 작업자 식별자입니다.</returns>
    public string ResolveDefaultActorId()
    {
        if (string.IsNullOrWhiteSpace(DefaultActorId))
        {
            throw new InvalidOperationException("Mes:OperatorExecutionBff:DefaultActorId must be a non-empty string.");
        }

        return DefaultActorId.Trim();
    }

    /// <summary>
    /// 구성의 fallback 완료 수량 단위를 검증한 값으로 반환합니다.
    /// </summary>
    /// <returns>trim 처리된 fallback 완료 수량 단위입니다.</returns>
    public string ResolveDefaultCompletionQuantityUnit()
    {
        if (string.IsNullOrWhiteSpace(DefaultCompletionQuantityUnit))
        {
            throw new InvalidOperationException("Mes:OperatorExecutionBff:DefaultCompletionQuantityUnit must be a non-empty string.");
        }

        return DefaultCompletionQuantityUnit.Trim();
    }
}
