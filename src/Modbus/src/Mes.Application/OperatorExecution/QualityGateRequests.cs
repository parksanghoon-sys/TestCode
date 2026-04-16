using Mes.Domain.Statuses;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 품질 판정 이후 게이트 정책을 함께 전달하는 입력 묶음입니다.
/// </summary>
/// <param name="BlocksOperation">판정 결과가 공정 진행을 차단하면 <see langword="true"/>입니다.</param>
/// <param name="DecisionNote">품질 판정 메모입니다.</param>
/// <param name="HoldReason">차단 시 사용할 hold 사유입니다.</param>
public sealed record QualityDecisionPolicy(
    bool BlocksOperation,
    string DecisionNote,
    string? HoldReason);

/// <summary>
/// 품질 판정 기록과 hold materialization에 필요한 입력 묶음입니다.
/// </summary>
/// <param name="Context">대상 aggregate 묶음입니다.</param>
/// <param name="Decision">기록할 품질 판정 결과입니다.</param>
/// <param name="Policy">판정 이후 적용할 게이트 정책입니다.</param>
/// <param name="OccurredAt">처리 시각입니다.</param>
public sealed record RecordQualityDecisionRequest(
    QualityGateContext Context,
    QualityDecisionStatus Decision,
    QualityDecisionPolicy Policy,
    DateTimeOffset OccurredAt);

/// <summary>
/// 품질 hold 해제와 operation gate 정합성 회복에 필요한 입력 묶음입니다.
/// </summary>
/// <param name="Context">대상 aggregate 묶음입니다.</param>
/// <param name="ReleaseNote">hold 해제 메모입니다.</param>
/// <param name="OccurredAt">처리 시각입니다.</param>
public sealed record ReleaseQualityHoldRequest(
    QualityGateContext Context,
    string ReleaseNote,
    DateTimeOffset OccurredAt);

/// <summary>
/// 품질 게이트 상태를 다시 맞출 때 사용하는 입력 묶음입니다.
/// </summary>
/// <param name="Context">대상 aggregate 묶음입니다.</param>
/// <param name="OccurredAt">정합성 회복 시각입니다.</param>
/// <param name="ReleaseNote">품질 hold 해제와 함께 operation hold를 풀 때 사용할 메모입니다.</param>
public sealed record QualityGateReconciliationRequest(
    QualityGateContext Context,
    DateTimeOffset OccurredAt,
    string? ReleaseNote = null);
