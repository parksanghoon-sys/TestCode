using Mes.Application.Contracts.OperatorExecution;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 운영자 실행 command를 application service로 전달하는 요청 묶음입니다.
/// </summary>
/// <typeparam name="TCommand">처리할 command 계약 형식입니다.</typeparam>
/// <param name="Command">처리할 command 계약입니다.</param>
/// <param name="ServerReceivedAt">서버 수신 시각입니다.</param>
public sealed record ExecuteOperatorExecutionCommandRequest<TCommand>(
    TCommand Command,
    DateTimeOffset ServerReceivedAt);

/// <summary>
/// 작업 큐 query를 application service로 전달하는 요청 묶음입니다.
/// </summary>
/// <param name="Request">작업 큐 query 계약입니다.</param>
/// <param name="SnapshotTakenAt">snapshot 기준 시각입니다.</param>
public sealed record ExecuteStationWorkQueueQueryRequest(
    GetStationWorkQueueRequestContract Request,
    DateTimeOffset SnapshotTakenAt);
