namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// 스테이션 BFF 호출 결과를 성공 값 또는 실패 정보로 감쌉니다.
/// </summary>
/// <typeparam name="TResponse">성공 시 반환할 응답 형식입니다.</typeparam>
public sealed class OperatorExecutionStationClientResult<TResponse>
{
    private OperatorExecutionStationClientResult(
        TResponse? value,
        OperatorExecutionStationClientFailure? failure)
    {
        Value = value;
        Failure = failure;
    }

    /// <summary>
    /// 성공 시 반환된 응답 값을 가져옵니다.
    /// </summary>
    public TResponse? Value { get; }

    /// <summary>
    /// 실패 시 반환된 오류 정보를 가져옵니다.
    /// </summary>
    public OperatorExecutionStationClientFailure? Failure { get; }

    /// <summary>
    /// 현재 결과가 성공인지 반환합니다.
    /// </summary>
    public bool IsSuccess => Failure is null;

    /// <summary>
    /// 성공 응답으로 결과를 생성합니다.
    /// </summary>
    /// <param name="value">성공 응답 값입니다.</param>
    /// <returns>성공 결과입니다.</returns>
    public static OperatorExecutionStationClientResult<TResponse> Success(TResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new OperatorExecutionStationClientResult<TResponse>(value, null);
    }

    /// <summary>
    /// 실패 정보로 결과를 생성합니다.
    /// </summary>
    /// <param name="failure">호출 실패 정보입니다.</param>
    /// <returns>실패 결과입니다.</returns>
    public static OperatorExecutionStationClientResult<TResponse> Fail(
        OperatorExecutionStationClientFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return new OperatorExecutionStationClientResult<TResponse>(default, failure);
    }
}
