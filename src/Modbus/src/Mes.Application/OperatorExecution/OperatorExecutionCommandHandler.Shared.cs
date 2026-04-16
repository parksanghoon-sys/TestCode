using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Common;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// 운영자 실행 command handler의 공통 검증과 replay 유틸리티를 제공합니다.
/// </summary>
public sealed partial class OperatorExecutionCommandHandler
{
    /// <summary>
    /// 수신 명령의 receipt 평가를 수행합니다.
    /// </summary>
    /// <typeparam name="TPayload">명령 payload 형식입니다.</typeparam>
    /// <param name="command">평가 대상 명령입니다.</param>
    /// <param name="fingerprint">현재 요청 fingerprint입니다.</param>
    /// <param name="existingReceipt">기존 receipt입니다.</param>
    /// <returns>idempotency 평가 결과입니다.</returns>
    private CommandReceiptEvaluationResult Evaluate<TPayload>(
        BffCommandEnvelope<TPayload> command,
        string fingerprint,
        CommandReceiptRecord? existingReceipt)
    {
        return _receiptPolicy.Evaluate(
            new CommandReceiptEvaluationRequest(
                _fingerprintBuilder.CreateScope(command),
                fingerprint,
                existingReceipt));
    }

    /// <summary>
    /// 일반 command의 replay 응답을 복원합니다.
    /// </summary>
    /// <typeparam name="TResponse">복원할 응답 형식입니다.</typeparam>
    /// <param name="evaluation">receipt 평가 결과입니다.</param>
    /// <returns>replay 결과가 있으면 처리 결과를 반환합니다.</returns>
    private HandledCommandResult<TResponse>? TryReplay<TResponse>(CommandReceiptEvaluationResult evaluation)
    {
        return evaluation.Decision switch
        {
            CommandReceiptDecisionKind.AcceptNew => null,
            CommandReceiptDecisionKind.ReplayStored => new HandledCommandResult<TResponse>(
                evaluation.Decision,
                DeserializeStoredResponse<TResponse>(evaluation),
                null),
            CommandReceiptDecisionKind.Conflict => throw CreateConflictException(evaluation),
            _ => throw new InvalidOperationException("Unsupported command receipt decision.")
        };
    }

    /// <summary>
    /// 공정 완료 명령의 replay 응답과 production actuals batch를 복원합니다.
    /// </summary>
    /// <param name="request">완료 명령 처리 입력입니다.</param>
    /// <param name="evaluation">receipt 평가 결과입니다.</param>
    /// <returns>replay 결과가 있으면 완료 처리 결과를 반환합니다.</returns>
    private CompleteOperationHandledCommandResult? TryReplayComplete(
        OperatorExecutionCommandHandlingRequest<CompleteOperationCommandContract, CompleteOperationCommandState> request,
        CommandReceiptEvaluationResult evaluation)
    {
        return evaluation.Decision switch
        {
            CommandReceiptDecisionKind.AcceptNew => null,
            CommandReceiptDecisionKind.ReplayStored => new CompleteOperationHandledCommandResult(
                evaluation.Decision,
                DeserializeStoredResponse<CompleteOperationResponseContract>(evaluation),
                null,
                PrepareProductionActualsBatch(request.State, evaluation.StoredReceipt!.AcceptedAt)),
            CommandReceiptDecisionKind.Conflict => throw CreateConflictException(evaluation),
            _ => throw new InvalidOperationException("Unsupported command receipt decision.")
        };
    }

    /// <summary>
    /// 저장된 응답 payload를 역직렬화합니다.
    /// </summary>
    /// <typeparam name="TResponse">응답 형식입니다.</typeparam>
    /// <param name="evaluation">receipt 평가 결과입니다.</param>
    /// <returns>receipt에 저장된 응답입니다.</returns>
    private TResponse DeserializeStoredResponse<TResponse>(CommandReceiptEvaluationResult evaluation)
    {
        if (evaluation.StoredReceipt?.ResponseJson is null)
        {
            throw new InvalidOperationException("Stored command receipt must carry a response payload for replay.");
        }

        return _responseSerializer.Deserialize<TResponse>(evaluation.StoredReceipt.ResponseJson);
    }

    /// <summary>
    /// 새롭게 수락한 command의 receipt를 생성합니다.
    /// </summary>
    /// <typeparam name="TPayload">명령 payload 형식입니다.</typeparam>
    /// <typeparam name="TResponse">응답 형식입니다.</typeparam>
    /// <param name="command">수락된 명령입니다.</param>
    /// <param name="evaluation">평가 결과입니다.</param>
    /// <param name="aggregateType">대상 aggregate 형식입니다.</param>
    /// <param name="aggregateId">대상 aggregate 식별자입니다.</param>
    /// <param name="response">저장할 응답입니다.</param>
    /// <param name="acceptedAt">수락 시각입니다.</param>
    /// <returns>재생 가능한 receipt 레코드입니다.</returns>
    private CommandReceiptRecord CreateAcceptedReceipt<TPayload, TResponse>(
        BffCommandEnvelope<TPayload> command,
        CommandReceiptEvaluationResult evaluation,
        string aggregateType,
        string aggregateId,
        TResponse response,
        DateTimeOffset acceptedAt)
    {
        return _receiptPolicy.CreateAcceptedReceipt(
            new RegisterAcceptedCommandReceiptRequest(
                command.CommandId,
                _fingerprintBuilder.CreateScope(command),
                command.ActorId,
                command.StationId,
                command.CorrelationId,
                evaluation.IncomingFingerprint,
                aggregateType,
                aggregateId,
                acceptedAt,
                _responseSerializer.Serialize(response)));
    }

    /// <summary>
    /// 품질 판정 정책을 생성합니다.
    /// </summary>
    /// <param name="decision">판정 결과입니다.</param>
    /// <param name="note">판정 메모입니다.</param>
    /// <returns>coordinator에 전달할 판정 정책입니다.</returns>
    private static QualityDecisionPolicy CreateQualityDecisionPolicy(QualityDecisionStatus decision, string note)
    {
        return decision == QualityDecisionStatus.Failed
            ? new QualityDecisionPolicy(true, NormalizeRequired(note, nameof(note)), QualityGateHoldReason)
            : new QualityDecisionPolicy(false, NormalizeRequired(note, nameof(note)), null);
    }

    /// <summary>
    /// 완료된 공정에서 production actuals batch skeleton을 준비합니다.
    /// </summary>
    /// <param name="state">완료 명령 대상 상태입니다.</param>
    /// <param name="preparedAt">batch 준비 시각입니다.</param>
    /// <returns>준비된 production actuals batch입니다.</returns>
    private PreparedProductionActualsBatch PrepareProductionActualsBatch(
        CompleteOperationCommandState state,
        DateTimeOffset preparedAt)
    {
        return _productionActualsPreparationService.PrepareForCompletedOperation(
            new PrepareProductionActualsBatchRequest(
                state.ProductionOrder,
                state.OperationExecution,
                preparedAt));
    }

    /// <summary>
    /// 품질 hold 적용 응답을 생성합니다.
    /// </summary>
    /// <param name="request">hold 명령 처리 입력입니다.</param>
    /// <returns>hold 응답입니다.</returns>
    private PlaceHoldResponseContract PlaceQualityHold(
        OperatorExecutionCommandHandlingRequest<PlaceHoldCommandContract, PlaceHoldCommandState> request)
    {
        var qualityRecord = request.State.QualityRecord
            ?? throw new ArgumentNullException(nameof(request.State.QualityRecord));
        var linkedOperation = request.State.LinkedOperationExecution
            ?? throw new ArgumentNullException(nameof(request.State.LinkedOperationExecution));

        EnsureMatchingId(qualityRecord.Id.ToString(), request.Command.Payload.SubjectId, nameof(request.Command.Payload.SubjectId));

        if (qualityRecord.Status != QualityRecordStatus.Hold)
        {
            qualityRecord.PlaceHold(request.Command.Payload.Reason, request.ServerReceivedAt);
        }

        var snapshot = _qualityHoldGateCoordinator.Reconcile(
            new QualityGateReconciliationRequest(
                new QualityGateContext(qualityRecord, linkedOperation),
                request.ServerReceivedAt));

        return new PlaceHoldResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.Command.Payload.SubjectType,
            request.Command.Payload.SubjectId,
            snapshot.QualityStatus.ToString());
    }

    /// <summary>
    /// 공정 hold 적용 응답을 생성합니다.
    /// </summary>
    /// <param name="request">hold 명령 처리 입력입니다.</param>
    /// <returns>hold 응답입니다.</returns>
    private PlaceHoldResponseContract PlaceOperationHold(
        OperatorExecutionCommandHandlingRequest<PlaceHoldCommandContract, PlaceHoldCommandState> request)
    {
        var operation = request.State.OperationExecution
            ?? throw new ArgumentNullException(nameof(request.State.OperationExecution));

        EnsureMatchingId(operation.Id.ToString(), request.Command.Payload.SubjectId, nameof(request.Command.Payload.SubjectId));
        operation.PlaceHold(new OperationHoldRequest(
            request.Command.Payload.Reason,
            request.ServerReceivedAt,
            HoldSourceTypes.Manual,
            request.Command.CommandId));

        return new PlaceHoldResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.Command.Payload.SubjectType,
            request.Command.Payload.SubjectId,
            operation.Status.ToString());
    }

    /// <summary>
    /// WIP hold 적용 응답을 생성합니다.
    /// </summary>
    /// <param name="request">hold 명령 처리 입력입니다.</param>
    /// <returns>hold 응답입니다.</returns>
    private PlaceHoldResponseContract PlaceWipHold(
        OperatorExecutionCommandHandlingRequest<PlaceHoldCommandContract, PlaceHoldCommandState> request)
    {
        var wipUnit = request.State.WipUnit
            ?? throw new ArgumentNullException(nameof(request.State.WipUnit));

        EnsureMatchingId(wipUnit.Id.ToString(), request.Command.Payload.SubjectId, nameof(request.Command.Payload.SubjectId));
        wipUnit.PlaceHold(request.Command.Payload.Reason);

        return new PlaceHoldResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.Command.Payload.SubjectType,
            request.Command.Payload.SubjectId,
            wipUnit.Status.ToString());
    }

    /// <summary>
    /// 품질 hold 해제 응답을 생성합니다.
    /// </summary>
    /// <param name="request">해제 명령 처리 입력입니다.</param>
    /// <returns>해제 응답입니다.</returns>
    private ReleaseHoldResponseContract ReleaseQualityHold(
        OperatorExecutionCommandHandlingRequest<ReleaseHoldCommandContract, ReleaseHoldCommandState> request)
    {
        var qualityRecord = request.State.QualityRecord
            ?? throw new ArgumentNullException(nameof(request.State.QualityRecord));
        var linkedOperation = request.State.LinkedOperationExecution
            ?? throw new ArgumentNullException(nameof(request.State.LinkedOperationExecution));

        EnsureMatchingId(qualityRecord.Id.ToString(), request.Command.Payload.SubjectId, nameof(request.Command.Payload.SubjectId));

        var snapshot = _qualityHoldGateCoordinator.ReleaseQualityHold(
            new ReleaseQualityHoldRequest(
                new QualityGateContext(qualityRecord, linkedOperation),
                request.Command.Payload.Note,
                request.ServerReceivedAt));

        return new ReleaseHoldResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.Command.Payload.SubjectType,
            request.Command.Payload.SubjectId,
            snapshot.QualityStatus.ToString());
    }

    /// <summary>
    /// 공정 hold 해제 응답을 생성합니다.
    /// </summary>
    /// <param name="request">해제 명령 처리 입력입니다.</param>
    /// <returns>해제 응답입니다.</returns>
    private ReleaseHoldResponseContract ReleaseOperationHold(
        OperatorExecutionCommandHandlingRequest<ReleaseHoldCommandContract, ReleaseHoldCommandState> request)
    {
        var operation = request.State.OperationExecution
            ?? throw new ArgumentNullException(nameof(request.State.OperationExecution));

        EnsureMatchingId(operation.Id.ToString(), request.Command.Payload.SubjectId, nameof(request.Command.Payload.SubjectId));
        operation.ReleaseHold(request.Command.Payload.Note, request.ServerReceivedAt);

        return new ReleaseHoldResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.Command.Payload.SubjectType,
            request.Command.Payload.SubjectId,
            operation.Status.ToString());
    }

    /// <summary>
    /// WIP hold 해제 응답을 생성합니다.
    /// </summary>
    /// <param name="request">해제 명령 처리 입력입니다.</param>
    /// <returns>해제 응답입니다.</returns>
    private ReleaseHoldResponseContract ReleaseWipHold(
        OperatorExecutionCommandHandlingRequest<ReleaseHoldCommandContract, ReleaseHoldCommandState> request)
    {
        var wipUnit = request.State.WipUnit
            ?? throw new ArgumentNullException(nameof(request.State.WipUnit));

        EnsureMatchingId(wipUnit.Id.ToString(), request.Command.Payload.SubjectId, nameof(request.Command.Payload.SubjectId));
        wipUnit.ReleaseHold();

        return new ReleaseHoldResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.Command.Payload.SubjectType,
            request.Command.Payload.SubjectId,
            wipUnit.Status.ToString());
    }

    /// <summary>
    /// 공정 시작 입력이 현재 aggregate 상태와 일치하는지 검증합니다.
    /// </summary>
    /// <param name="request">검증할 처리 입력입니다.</param>
    private static void ValidateStartOperationRequest(
        OperatorExecutionCommandHandlingRequest<StartOperationCommandContract, StartOperationCommandState> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Command);
        ArgumentNullException.ThrowIfNull(request.State);

        EnsureMatchingId(request.State.ProductionOrder.Id.ToString(), request.Command.Payload.ProductionOrderId, nameof(request.Command.Payload.ProductionOrderId));
        EnsureMatchingId(request.State.OperationExecution.Id.ToString(), request.Command.Payload.OperationExecutionId, nameof(request.Command.Payload.OperationExecutionId));

        if (request.State.OperationExecution.ProductionOrderId != request.State.ProductionOrder.Id)
        {
            throw new DomainException("Start operation requires a matching production order and operation execution.");
        }

        if (request.State.OperationExecution.OperationSequence != request.Command.Payload.OperationSequence)
        {
            throw new DomainException("Start operation requires a matching operation sequence.");
        }

        if (!string.IsNullOrWhiteSpace(request.Command.Payload.QuantityUnit)
            && !string.Equals(request.State.OperationExecution.QuantityUnit, request.Command.Payload.QuantityUnit, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Start operation quantity unit must match the operation unit.");
        }
    }

    /// <summary>
    /// 자재 소모 입력이 현재 aggregate 상태와 일치하는지 검증합니다.
    /// </summary>
    /// <param name="request">검증할 처리 입력입니다.</param>
    private static void ValidateMaterialConsumptionRequest(
        OperatorExecutionCommandHandlingRequest<RecordMaterialConsumptionCommandContract, RecordMaterialConsumptionCommandState> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Command);
        ArgumentNullException.ThrowIfNull(request.State);

        EnsureMatchingId(request.State.OperationExecution.Id.ToString(), request.Command.Payload.OperationExecutionId, nameof(request.Command.Payload.OperationExecutionId));
        EnsureMatchingId(request.State.WipUnit.Id.ToString(), request.Command.Payload.WipUnitId, nameof(request.Command.Payload.WipUnitId));
        EnsureMatchingId(request.State.MaterialLot.Id.ToString(), request.Command.Payload.MaterialLotId, nameof(request.Command.Payload.MaterialLotId));

        if (request.State.WipUnit.CurrentOperationExecutionId != request.State.OperationExecution.Id)
        {
            throw new DomainException("Material consumption requires the WIP unit to be linked to the active operation execution.");
        }

        if (!string.Equals(request.State.MaterialLot.MaterialCode, request.Command.Payload.MaterialCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Material consumption requires a matching material code.");
        }
    }

    /// <summary>
    /// 품질 결과 입력이 현재 aggregate 상태와 일치하는지 검증합니다.
    /// </summary>
    /// <param name="request">검증할 처리 입력입니다.</param>
    private static void ValidateQualityResultRequest(
        OperatorExecutionCommandHandlingRequest<RecordQualityResultCommandContract, RecordQualityResultCommandState> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Command);
        ArgumentNullException.ThrowIfNull(request.State);

        EnsureMatchingId(request.State.QualityRecord.Id.ToString(), request.Command.Payload.QualityRecordId, nameof(request.Command.Payload.QualityRecordId));
        EnsureMatchingId(request.State.WipUnit.Id.ToString(), request.Command.Payload.WipUnitId, nameof(request.Command.Payload.WipUnitId));

        if (request.State.QualityRecord.WipUnitId != request.State.WipUnit.Id)
        {
            throw new DomainException("Quality result requires the quality record to match the current WIP unit.");
        }

        if (request.State.WipUnit.CurrentOperationExecutionId != request.State.OperationExecution.Id)
        {
            throw new DomainException("Quality result requires the WIP unit to point to the current operation execution.");
        }

        if (!string.Equals(request.State.QualityRecord.InspectionCode, request.Command.Payload.InspectionCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Quality result requires a matching inspection code.");
        }
    }

    /// <summary>
    /// 공정 완료 입력이 현재 aggregate 상태와 일치하는지 검증합니다.
    /// </summary>
    /// <param name="request">검증할 처리 입력입니다.</param>
    private static void ValidateCompleteOperationRequest(
        OperatorExecutionCommandHandlingRequest<CompleteOperationCommandContract, CompleteOperationCommandState> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Command);
        ArgumentNullException.ThrowIfNull(request.State);

        EnsureMatchingId(request.State.OperationExecution.Id.ToString(), request.Command.Payload.OperationExecutionId, nameof(request.Command.Payload.OperationExecutionId));

        if (request.State.OperationExecution.ProductionOrderId != request.State.ProductionOrder.Id)
        {
            throw new DomainException("Complete operation requires a matching production order and operation execution.");
        }
    }

    /// <summary>
    /// 품질 판정 문자열을 domain enum으로 변환합니다.
    /// </summary>
    /// <param name="decision">계약에서 받은 판정 문자열입니다.</param>
    /// <returns>domain 품질 판정 enum입니다.</returns>
    private static QualityDecisionStatus ParseDecision(string decision)
    {
        return NormalizeRequired(decision, nameof(decision)).ToLowerInvariant() switch
        {
            QualityDecisionValues.Passed => QualityDecisionStatus.Passed,
            QualityDecisionValues.Failed => QualityDecisionStatus.Failed,
            _ => throw new DomainException($"Unsupported quality decision: {decision}.")
        };
    }

    /// <summary>
    /// 완료 모드 문자열을 정규화하고 지원 여부를 검증합니다.
    /// </summary>
    /// <param name="completionMode">계약에서 받은 완료 모드입니다.</param>
    /// <returns>정규화된 완료 모드입니다.</returns>
    private static string NormalizeCompletionMode(string? completionMode)
    {
        return string.IsNullOrWhiteSpace(completionMode)
            ? CompletionModeValues.Manual
            : completionMode.Trim().ToLowerInvariant() switch
            {
                CompletionModeValues.Manual => CompletionModeValues.Manual,
                CompletionModeValues.EquipmentAssisted => CompletionModeValues.EquipmentAssisted,
                _ => throw new DomainException($"Unsupported completion mode: {completionMode}.")
            };
    }

    /// <summary>
    /// transport 수량 계약을 domain 수량으로 변환합니다.
    /// </summary>
    /// <param name="quantity">transport 수량 계약입니다.</param>
    /// <returns>domain 수량 값 객체입니다.</returns>
    private static MeasuredQuantity ToMeasuredQuantity(MeasuredQuantityContract quantity)
    {
        return new MeasuredQuantity(quantity.Value, quantity.Unit);
    }

    /// <summary>
    /// domain 수량을 transport 계약으로 변환합니다.
    /// </summary>
    /// <param name="quantity">domain 수량입니다.</param>
    /// <returns>transport 수량 계약입니다.</returns>
    private static MeasuredQuantityContract ToContract(MeasuredQuantity quantity)
    {
        return new MeasuredQuantityContract(quantity.Value, quantity.Unit);
    }

    /// <summary>
    /// hold subject에 대응하는 aggregate 형식을 반환합니다.
    /// </summary>
    /// <param name="subjectType">hold 대상 subject 형식입니다.</param>
    /// <returns>receipt에 기록할 aggregate 형식입니다.</returns>
    private static string GetAggregateTypeForSubject(string subjectType)
    {
        return subjectType switch
        {
            HoldSubjectTypeValues.OperationExecution => nameof(OperationExecution),
            HoldSubjectTypeValues.WipUnit => nameof(WipUnit),
            HoldSubjectTypeValues.QualityRecord => nameof(QualityRecord),
            _ => throw new DomainException($"Unsupported hold subject type: {subjectType}.")
        };
    }

    /// <summary>
    /// 상태 식별자가 요청 값과 일치하는지 검증합니다.
    /// </summary>
    /// <param name="actualId">현재 aggregate 식별자입니다.</param>
    /// <param name="expectedId">요청에서 받은 식별자입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 파라미터 이름입니다.</param>
    private static void EnsureMatchingId(string actualId, string expectedId, string parameterName)
    {
        if (!string.Equals(actualId, NormalizeRequired(expectedId, parameterName), StringComparison.Ordinal))
        {
            throw new DomainException($"Request identifier {parameterName} does not match the loaded aggregate state.");
        }
    }

    /// <summary>
    /// 필수 문자열 입력을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 파라미터 이름입니다.</param>
    /// <returns>trim된 문자열입니다.</returns>
    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Required string value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    /// <summary>
    /// idempotency 충돌 예외를 생성합니다.
    /// </summary>
    /// <param name="evaluation">receipt 평가 결과입니다.</param>
    /// <returns>충돌을 설명하는 domain 예외입니다.</returns>
    private static DomainException CreateConflictException(CommandReceiptEvaluationResult evaluation)
    {
        var storedCommandId = evaluation.StoredReceipt?.CommandId ?? "<unknown>";
        return new DomainException($"Idempotency conflict detected against stored command {storedCommandId}.");
    }
}
