namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// BFF 실패를 스테이션 작업자용 메시지로 변환하는 정책입니다.
/// </summary>
public sealed class OperatorExecutionProblemDisplayPolicy
{
    /// <summary>
    /// 호출 실패를 사용자 메시지로 변환합니다.
    /// </summary>
    /// <param name="failure">변환할 실패 정보입니다.</param>
    /// <returns>작업자에게 표시할 메시지입니다.</returns>
    public StationUserMessage Map(OperatorExecutionStationClientFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        var errorCode = failure.ProblemDetails?.GetErrorCode() ?? failure.ClientErrorCode;
        var detail = failure.ProblemDetails?.Detail ?? failure.ClientMessage;

        return errorCode switch
        {
            "operator_execution.not_found" => new StationUserMessage(
                "대상 없음",
                detail ?? "요청한 작업 대상을 현재 스테이션 범위에서 찾지 못했습니다.",
                "warning"),
            "operator_execution.conflict" => new StationUserMessage(
                "상태 충돌",
                detail ?? "현재 공정 상태가 요청한 작업과 맞지 않습니다. 최신 상태를 다시 확인하세요.",
                "warning"),
            "operator_execution.validation_failed" => new StationUserMessage(
                "입력 확인 필요",
                detail ?? "입력 값이 현재 작업 규칙과 맞지 않습니다.",
                "warning"),
            "transport.invalid_request" => new StationUserMessage(
                "요청 형식 오류",
                detail ?? "전송한 값 형식이 잘못되어 요청을 처리할 수 없습니다.",
                "error"),
            "system.unexpected_error" => new StationUserMessage(
                "서버 오류",
                detail ?? "서버에서 예기치 않은 오류가 발생했습니다.",
                "error"),
            "client.connectivity_failure" => new StationUserMessage(
                "연결 실패",
                detail ?? "BFF 연결에 실패했습니다.",
                "error"),
            "client.empty_response" => new StationUserMessage(
                "응답 본문 누락",
                detail ?? "BFF가 비어 있는 성공 응답을 반환했습니다.",
                "error"),
            "client.invalid_response" => new StationUserMessage(
                "응답 해석 실패",
                detail ?? "BFF 성공 응답을 해석하지 못했습니다.",
                "error"),
            _ => new StationUserMessage(
                "알 수 없는 오류",
                detail ?? "원인을 특정하지 못한 오류가 발생했습니다.",
                "error")
        };
    }
}
