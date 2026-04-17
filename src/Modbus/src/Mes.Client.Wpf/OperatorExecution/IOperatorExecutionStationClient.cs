using Mes.Application.Contracts.OperatorExecution;

namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// 스테이션 셸이 operator-execution BFF와 통신할 때 사용하는 클라이언트 계약입니다.
/// </summary>
public interface IOperatorExecutionStationClient
{
    /// <summary>
    /// 지정한 스테이션의 현재 작업 큐를 조회합니다.
    /// </summary>
    /// <param name="request">조회 요청입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 작업 큐 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    Task<OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>> GetStationWorkQueueAsync(
        GetStationWorkQueueRequestContract request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지정한 공정 실행에 대한 시작 명령을 전송합니다.
    /// </summary>
    /// <param name="command">시작 명령 계약입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 시작 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    Task<OperatorExecutionStationClientResult<StartOperationResponseContract>> StartOperationAsync(
        StartOperationCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지정한 공정 실행에 대한 자재 투입 명령을 전송합니다.
    /// </summary>
    /// <param name="command">자재 투입 명령 계약입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 자재 투입 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    Task<OperatorExecutionStationClientResult<RecordMaterialConsumptionResponseContract>> RecordMaterialConsumptionAsync(
        RecordMaterialConsumptionCommandContract command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지정한 공정 실행에 대한 완료 명령을 전송합니다.
    /// </summary>
    /// <param name="command">완료 명령 계약입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 완료 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    Task<OperatorExecutionStationClientResult<CompleteOperationResponseContract>> CompleteOperationAsync(
        CompleteOperationCommandContract command,
        CancellationToken cancellationToken = default);
}
