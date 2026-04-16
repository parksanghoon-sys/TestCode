using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Common;
using Mes.Domain.Statuses;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 품질 기록과 공정 실행 사이의 release-1 quality hold 게이트 규칙을 조정합니다.
/// </summary>
public sealed class QualityHoldGateCoordinator
{
    /// <summary>
    /// 품질 판정 결과를 기록하고 필요하면 operation hold로 materialize합니다.
    /// </summary>
    /// <param name="request">품질 판정 기록 요청입니다.</param>
    /// <returns>처리 후 품질 게이트 요약 상태입니다.</returns>
    public QualityGateSnapshot RecordQualityResult(RecordQualityDecisionRequest request)
    {
        ValidateRecordRequest(request);

        if (IsReplay(request))
        {
            return request.Policy.BlocksOperation
                ? Reconcile(new QualityGateReconciliationRequest(request.Context, request.OccurredAt))
                : CreateSnapshot(request.Context);
        }

        ApplyDecision(request);

        if (!request.Policy.BlocksOperation)
        {
            return CreateSnapshot(request.Context);
        }

        request.Context.QualityRecord.PlaceHold(GetHoldReason(request), request.OccurredAt);
        return Reconcile(new QualityGateReconciliationRequest(request.Context, request.OccurredAt));
    }

    /// <summary>
    /// 품질 hold를 해제하고 hold provenance가 일치하는 operation hold만 함께 해제합니다.
    /// </summary>
    /// <param name="request">품질 hold 해제 요청입니다.</param>
    /// <returns>처리 후 품질 게이트 요약 상태입니다.</returns>
    public QualityGateSnapshot ReleaseQualityHold(ReleaseQualityHoldRequest request)
    {
        if (request.Context.QualityRecord.Status == QualityRecordStatus.Released)
        {
            return Reconcile(new QualityGateReconciliationRequest(request.Context, request.OccurredAt, request.ReleaseNote));
        }

        if (request.Context.QualityRecord.Status != QualityRecordStatus.Hold)
        {
            throw new DomainException("Only a held quality record can be released.");
        }

        request.Context.QualityRecord.ReleaseHold(request.ReleaseNote, request.OccurredAt);
        return Reconcile(new QualityGateReconciliationRequest(request.Context, request.OccurredAt, request.ReleaseNote));
    }

    /// <summary>
    /// 품질 기록과 공정 실행의 hold 상태를 release-1 규칙에 맞게 다시 맞춥니다.
    /// </summary>
    /// <param name="request">정합성 회복 요청입니다.</param>
    /// <returns>정합성 회복 후 품질 게이트 요약 상태입니다.</returns>
    public QualityGateSnapshot Reconcile(QualityGateReconciliationRequest request)
    {
        var qualityRecord = request.Context.QualityRecord;
        var operationExecution = request.Context.OperationExecution;

        if (qualityRecord.Status == QualityRecordStatus.Hold)
        {
            if (operationExecution.Status != OperationExecutionStatus.Hold && IsOperationActive(operationExecution))
            {
                operationExecution.PlaceHold(new OperationHoldRequest(
                    GetQualityHoldReason(qualityRecord),
                    request.OccurredAt,
                    HoldSourceTypes.QualityRecord,
                    qualityRecord.Id.ToString()));
            }

            return CreateSnapshot(request.Context);
        }

        if (operationExecution.IsHeldBy(HoldSourceTypes.QualityRecord, qualityRecord.Id.ToString()))
        {
            operationExecution.ReleaseHold(new OperationHoldReleaseRequest(
                GetReleaseNote(request),
                request.OccurredAt,
                HoldSourceTypes.QualityRecord,
                qualityRecord.Id.ToString()));
        }

        return CreateSnapshot(request.Context);
    }

    /// <summary>
    /// 공정 완료 전에 quality gate가 열려 있는지 검사합니다.
    /// </summary>
    /// <param name="operationExecution">검사할 공정 실행 aggregate입니다.</param>
    public void EnsureCompletionAllowed(OperationExecution operationExecution)
    {
        if (operationExecution.Status == OperationExecutionStatus.Hold)
        {
            throw new DomainException("Operation completion is blocked while the quality gate is on hold.");
        }
    }

    /// <summary>
    /// 품질 판정 요청이 현재 상태에서 유효한지 검증합니다.
    /// </summary>
    /// <param name="request">검증할 품질 판정 요청입니다.</param>
    private static void ValidateRecordRequest(RecordQualityDecisionRequest request)
    {
        _ = request.Context ?? throw new ArgumentNullException(nameof(request.Context));
        _ = request.Context.QualityRecord ?? throw new ArgumentNullException(nameof(request.Context.QualityRecord));
        _ = request.Context.OperationExecution ?? throw new ArgumentNullException(nameof(request.Context.OperationExecution));

        if (request.Policy.BlocksOperation && string.IsNullOrWhiteSpace(request.Policy.HoldReason))
        {
            throw new DomainException("Blocking quality decisions require a hold reason.");
        }
    }

    /// <summary>
    /// 현재 요청이 동일 판정의 안전한 재실행인지 판별합니다.
    /// </summary>
    /// <param name="request">판별할 품질 판정 요청입니다.</param>
    /// <returns>같은 결과의 재실행이면 <see langword="true"/>입니다.</returns>
    private static bool IsReplay(RecordQualityDecisionRequest request)
    {
        var qualityRecord = request.Context.QualityRecord;

        if (qualityRecord.DecisionStatus != request.Decision)
        {
            return false;
        }

        return request.Policy.BlocksOperation
            ? qualityRecord.Status == QualityRecordStatus.Hold
            : qualityRecord.Status == MapDecisionToRecordStatus(request.Decision);
    }

    /// <summary>
    /// 품질 aggregate에 판정 결과를 반영합니다.
    /// </summary>
    /// <param name="request">판정 반영 요청입니다.</param>
    private static void ApplyDecision(RecordQualityDecisionRequest request)
    {
        if (request.Decision == QualityDecisionStatus.Passed)
        {
            request.Context.QualityRecord.RecordPass(request.Policy.DecisionNote, request.OccurredAt);
            return;
        }

        request.Context.QualityRecord.RecordFail(request.Policy.DecisionNote, request.OccurredAt);
    }

    /// <summary>
    /// blocking 품질 판정에 사용할 hold 사유를 반환합니다.
    /// </summary>
    /// <param name="request">품질 판정 요청입니다.</param>
    /// <returns>정규화된 hold 사유입니다.</returns>
    private static string GetHoldReason(RecordQualityDecisionRequest request)
    {
        return request.Policy.HoldReason!.Trim();
    }

    /// <summary>
    /// 품질 기록에 저장된 hold 사유를 operation hold 사유로 변환합니다.
    /// </summary>
    /// <param name="qualityRecord">대상 품질 기록입니다.</param>
    /// <returns>operation hold에 사용할 사유입니다.</returns>
    private static string GetQualityHoldReason(QualityRecord qualityRecord)
    {
        if (string.IsNullOrWhiteSpace(qualityRecord.HoldReason))
        {
            throw new DomainException("Held quality records must keep a hold reason.");
        }

        return qualityRecord.HoldReason;
    }

    /// <summary>
    /// operation hold 해제에 사용할 메모를 반환합니다.
    /// </summary>
    /// <param name="request">정합성 회복 요청입니다.</param>
    /// <returns>정규화된 해제 메모입니다.</returns>
    private static string GetReleaseNote(QualityGateReconciliationRequest request)
    {
        return string.IsNullOrWhiteSpace(request.ReleaseNote)
            ? "quality gate synchronized"
            : request.ReleaseNote.Trim();
    }

    /// <summary>
    /// 공정 실행이 quality hold를 적용할 수 있는 활성 상태인지 판별합니다.
    /// </summary>
    /// <param name="operationExecution">판별할 공정 실행입니다.</param>
    /// <returns>활성 상태이면 <see langword="true"/>입니다.</returns>
    private static bool IsOperationActive(OperationExecution operationExecution)
    {
        return operationExecution.Status is OperationExecutionStatus.Ready
            or OperationExecutionStatus.Queued
            or OperationExecutionStatus.Running
            or OperationExecutionStatus.Paused
            or OperationExecutionStatus.Rework;
    }

    /// <summary>
    /// 품질 판정 enum을 품질 기록 상태 enum으로 변환합니다.
    /// </summary>
    /// <param name="decisionStatus">변환할 품질 판정 상태입니다.</param>
    /// <returns>대응하는 품질 기록 상태입니다.</returns>
    private static QualityRecordStatus MapDecisionToRecordStatus(QualityDecisionStatus decisionStatus)
    {
        return decisionStatus == QualityDecisionStatus.Passed
            ? QualityRecordStatus.Passed
            : QualityRecordStatus.Failed;
    }

    /// <summary>
    /// 현재 aggregate 조합의 품질 게이트 상태를 요약합니다.
    /// </summary>
    /// <param name="context">요약할 aggregate 묶음입니다.</param>
    /// <returns>현재 품질 게이트 스냅샷입니다.</returns>
    private static QualityGateSnapshot CreateSnapshot(QualityGateContext context)
    {
        return new QualityGateSnapshot(
            context.QualityRecord.Status,
            context.QualityRecord.DecisionStatus,
            context.OperationExecution.Status,
            context.OperationExecution.Status != OperationExecutionStatus.Hold);
    }
}
