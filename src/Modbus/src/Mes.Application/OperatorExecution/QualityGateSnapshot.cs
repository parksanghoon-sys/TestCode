using Mes.Domain.Statuses;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// coordinator 실행 후 품질 게이트의 요약 상태를 나타냅니다.
/// </summary>
/// <param name="QualityStatus">현재 품질 기록 상태입니다.</param>
/// <param name="DecisionStatus">마지막 품질 판정 결과입니다.</param>
/// <param name="OperationStatus">현재 공정 실행 상태입니다.</param>
/// <param name="QualityGateOpen">공정 완료 관점에서 gate가 열려 있으면 <see langword="true"/>입니다.</param>
public sealed record QualityGateSnapshot(
    QualityRecordStatus QualityStatus,
    QualityDecisionStatus? DecisionStatus,
    OperationExecutionStatus OperationStatus,
    bool QualityGateOpen);
