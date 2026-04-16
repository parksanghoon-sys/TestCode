using Mes.Domain.Aggregates;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 품질 게이트 coordinator가 함께 다루는 aggregate 묶음입니다.
/// </summary>
/// <param name="QualityRecord">품질 기록 aggregate입니다.</param>
/// <param name="OperationExecution">공정 실행 aggregate입니다.</param>
public sealed record QualityGateContext(
    QualityRecord QualityRecord,
    OperationExecution OperationExecution);
