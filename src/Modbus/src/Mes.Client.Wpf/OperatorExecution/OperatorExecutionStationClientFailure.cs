namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// 스테이션 BFF 호출 실패 정보를 나타냅니다.
/// </summary>
/// <param name="StatusCode">HTTP 상태 코드입니다.</param>
/// <param name="ProblemDetails">서버가 반환한 problem details입니다.</param>
/// <param name="ClientErrorCode">클라이언트 측에서 분류한 오류 코드입니다.</param>
/// <param name="ClientMessage">클라이언트 측에서 생성한 사용자 표시용 오류 메시지입니다.</param>
public sealed record OperatorExecutionStationClientFailure(
    int? StatusCode,
    BffProblemDetails? ProblemDetails,
    string? ClientErrorCode,
    string? ClientMessage);
