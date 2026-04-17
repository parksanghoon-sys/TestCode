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
/// 운영자 실행 파일럿 슬라이스의 command 처리 진입점을 제공합니다.
/// </summary>
public sealed partial class OperatorExecutionCommandHandler
{
    private const string QualityGateHoldReason = "quality gate active";
    private const string CompletionScrapReason = "completion consolidated scrap";

    private readonly CanonicalCommandFingerprintBuilder _fingerprintBuilder;
    private readonly CommandReceiptIdempotencyPolicy _receiptPolicy;
    private readonly StoredCommandResponseSerializer _responseSerializer;
    private readonly QualityHoldGateCoordinator _qualityHoldGateCoordinator;
    private readonly ProductionActualsPreparationService _productionActualsPreparationService;

    /// <summary>
    /// 운영자 실행 command handler를 초기화합니다.
    /// </summary>
    /// <param name="fingerprintBuilder">command fingerprint 계산기입니다.</param>
    /// <param name="receiptPolicy">idempotency receipt 정책입니다.</param>
    /// <param name="responseSerializer">저장 응답 직렬화기입니다.</param>
    /// <param name="qualityHoldGateCoordinator">품질 게이트 coordinator입니다.</param>
    /// <param name="productionActualsPreparationService">생산실적 skeleton 준비기입니다.</param>
    public OperatorExecutionCommandHandler(
        CanonicalCommandFingerprintBuilder fingerprintBuilder,
        CommandReceiptIdempotencyPolicy receiptPolicy,
        StoredCommandResponseSerializer responseSerializer,
        QualityHoldGateCoordinator qualityHoldGateCoordinator,
        ProductionActualsPreparationService productionActualsPreparationService)
    {
        _fingerprintBuilder = fingerprintBuilder;
        _receiptPolicy = receiptPolicy;
        _responseSerializer = responseSerializer;
        _qualityHoldGateCoordinator = qualityHoldGateCoordinator;
        _productionActualsPreparationService = productionActualsPreparationService;
    }

    /// <summary>
    /// `start-operation` 명령을 처리합니다.
    /// </summary>
    /// <param name="request">명령 계약과 aggregate 상태 묶음입니다.</param>
    /// <returns>처리 결과와 replay용 receipt입니다.</returns>
    public HandledCommandResult<StartOperationResponseContract> Handle(
        OperatorExecutionCommandHandlingRequest<StartOperationCommandContract, StartOperationCommandState> request)
    {
        ValidateStartOperationRequest(request);

        var evaluation = Evaluate(request.Command, _fingerprintBuilder.Build(request.Command), request.ExistingReceipt);
        var replayed = TryReplay<StartOperationResponseContract>(evaluation);
        if (replayed is not null)
        {
            return replayed;
        }

        var stationId = new StationId(NormalizeRequired(request.Command.StationId, nameof(request.Command.StationId)));
        request.State.OperationExecution.Start(stationId, request.ServerReceivedAt);

        if (request.State.ProductionOrder.Status is ProductionOrderStatus.Dispatched or ProductionOrderStatus.PartiallyCompleted)
        {
            request.State.ProductionOrder.MarkInProgress();
        }

        var response = new StartOperationResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.State.OperationExecution.Id.ToString(),
            request.State.OperationExecution.Status.ToString(),
            request.State.OperationExecution.StartedAt ?? request.ServerReceivedAt);

        return new HandledCommandResult<StartOperationResponseContract>(
            evaluation.Decision,
            response,
            CreateAcceptedReceipt(
                request.Command,
                evaluation,
                nameof(OperationExecution),
                request.State.OperationExecution.Id.ToString(),
                response,
                request.ServerReceivedAt));
    }

    /// <summary>
    /// `record-material-consumption` 명령을 처리합니다.
    /// </summary>
    /// <param name="request">명령 계약과 aggregate 상태 묶음입니다.</param>
    /// <returns>처리 결과와 replay용 receipt입니다.</returns>
    public HandledCommandResult<RecordMaterialConsumptionResponseContract> Handle(
        OperatorExecutionCommandHandlingRequest<RecordMaterialConsumptionCommandContract, RecordMaterialConsumptionCommandState> request)
    {
        ValidateMaterialConsumptionRequest(request);

        var evaluation = Evaluate(request.Command, _fingerprintBuilder.Build(request.Command), request.ExistingReceipt);
        var replayed = TryReplay<RecordMaterialConsumptionResponseContract>(evaluation);
        if (replayed is not null)
        {
            return replayed;
        }

        var consumedQuantity = ToMeasuredQuantity(request.Command.Payload.Quantity);
        var genealogyCountBefore = request.State.MaterialLot.GenealogyLinks.Count;

        if (request.State.MaterialLot.Status == MaterialLotStatus.Available)
        {
            request.State.MaterialLot.IssueToLine();
        }

        request.State.MaterialLot.ConsumeFor(request.State.WipUnit.Id, consumedQuantity, request.ServerReceivedAt);

        var response = new RecordMaterialConsumptionResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.State.MaterialLot.Id.ToString(),
            ToContract(request.State.MaterialLot.AvailableQuantity),
            request.State.MaterialLot.GenealogyLinks.Count > genealogyCountBefore);

        return new HandledCommandResult<RecordMaterialConsumptionResponseContract>(
            evaluation.Decision,
            response,
            CreateAcceptedReceipt(
                request.Command,
                evaluation,
                nameof(MaterialLot),
                request.State.MaterialLot.Id.ToString(),
                response,
                request.ServerReceivedAt));
    }

    /// <summary>
    /// `place-hold` 명령을 처리합니다.
    /// </summary>
    /// <param name="request">명령 계약과 aggregate 상태 묶음입니다.</param>
    /// <returns>처리 결과와 replay용 receipt입니다.</returns>
    public HandledCommandResult<PlaceHoldResponseContract> Handle(
        OperatorExecutionCommandHandlingRequest<PlaceHoldCommandContract, PlaceHoldCommandState> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var evaluation = Evaluate(request.Command, _fingerprintBuilder.Build(request.Command), request.ExistingReceipt);
        var replayed = TryReplay<PlaceHoldResponseContract>(evaluation);
        if (replayed is not null)
        {
            return replayed;
        }

        var response = request.Command.Payload.SubjectType switch
        {
            HoldSubjectTypeValues.OperationExecution => PlaceOperationHold(request),
            HoldSubjectTypeValues.WipUnit => PlaceWipHold(request),
            HoldSubjectTypeValues.QualityRecord => PlaceQualityHold(request),
            _ => throw new OperatorExecutionValidationException(
                $"Unsupported hold subject type: {request.Command.Payload.SubjectType}.")
        };

        return new HandledCommandResult<PlaceHoldResponseContract>(
            evaluation.Decision,
            response,
            CreateAcceptedReceipt(
                request.Command,
                evaluation,
                GetAggregateTypeForSubject(request.Command.Payload.SubjectType),
                request.Command.Payload.SubjectId,
                response,
                request.ServerReceivedAt));
    }

    /// <summary>
    /// `release-hold` 명령을 처리합니다.
    /// </summary>
    /// <param name="request">명령 계약과 aggregate 상태 묶음입니다.</param>
    /// <returns>처리 결과와 replay용 receipt입니다.</returns>
    public HandledCommandResult<ReleaseHoldResponseContract> Handle(
        OperatorExecutionCommandHandlingRequest<ReleaseHoldCommandContract, ReleaseHoldCommandState> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var evaluation = Evaluate(request.Command, _fingerprintBuilder.Build(request.Command), request.ExistingReceipt);
        var replayed = TryReplay<ReleaseHoldResponseContract>(evaluation);
        if (replayed is not null)
        {
            return replayed;
        }

        var response = request.Command.Payload.SubjectType switch
        {
            HoldSubjectTypeValues.OperationExecution => ReleaseOperationHold(request),
            HoldSubjectTypeValues.WipUnit => ReleaseWipHold(request),
            HoldSubjectTypeValues.QualityRecord => ReleaseQualityHold(request),
            _ => throw new OperatorExecutionValidationException(
                $"Unsupported hold subject type: {request.Command.Payload.SubjectType}.")
        };

        return new HandledCommandResult<ReleaseHoldResponseContract>(
            evaluation.Decision,
            response,
            CreateAcceptedReceipt(
                request.Command,
                evaluation,
                GetAggregateTypeForSubject(request.Command.Payload.SubjectType),
                request.Command.Payload.SubjectId,
                response,
                request.ServerReceivedAt));
    }

    /// <summary>
    /// `record-quality-result` 명령을 처리합니다.
    /// </summary>
    /// <param name="request">명령 계약과 aggregate 상태 묶음입니다.</param>
    /// <returns>처리 결과와 replay용 receipt입니다.</returns>
    public HandledCommandResult<RecordQualityResultResponseContract> Handle(
        OperatorExecutionCommandHandlingRequest<RecordQualityResultCommandContract, RecordQualityResultCommandState> request)
    {
        ValidateQualityResultRequest(request);

        var evaluation = Evaluate(request.Command, _fingerprintBuilder.Build(request.Command), request.ExistingReceipt);
        var replayed = TryReplay<RecordQualityResultResponseContract>(evaluation);
        if (replayed is not null)
        {
            return replayed;
        }

        var decision = ParseDecision(request.Command.Payload.Decision);
        var snapshot = _qualityHoldGateCoordinator.RecordQualityResult(
            new RecordQualityDecisionRequest(
                new QualityGateContext(request.State.QualityRecord, request.State.OperationExecution),
                decision,
                CreateQualityDecisionPolicy(decision, request.Command.Payload.Note),
                request.ServerReceivedAt));

        var response = new RecordQualityResultResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.State.QualityRecord.Id.ToString(),
            snapshot.QualityStatus.ToString(),
            snapshot.QualityGateOpen);

        return new HandledCommandResult<RecordQualityResultResponseContract>(
            evaluation.Decision,
            response,
            CreateAcceptedReceipt(
                request.Command,
                evaluation,
                nameof(QualityRecord),
                request.State.QualityRecord.Id.ToString(),
                response,
                request.ServerReceivedAt));
    }

    /// <summary>
    /// `complete-operation` 명령을 처리합니다.
    /// </summary>
    /// <param name="request">명령 계약과 aggregate 상태 묶음입니다.</param>
    /// <returns>처리 결과, replay용 receipt, production actuals skeleton입니다.</returns>
    public CompleteOperationHandledCommandResult Handle(
        OperatorExecutionCommandHandlingRequest<CompleteOperationCommandContract, CompleteOperationCommandState> request)
    {
        ValidateCompleteOperationRequest(request);

        var evaluation = Evaluate(request.Command, _fingerprintBuilder.Build(request.Command), request.ExistingReceipt);
        var replayed = TryReplayComplete(request, evaluation);
        if (replayed is not null)
        {
            return replayed;
        }

        _ = NormalizeCompletionMode(request.Command.Payload.CompletionMode);
        _qualityHoldGateCoordinator.EnsureCompletionAllowed(request.State.OperationExecution);

        if (request.Command.Payload.ScrapQuantity is not null && request.Command.Payload.ScrapQuantity.Value > 0m)
        {
            request.State.OperationExecution.RecordScrap(
                ToMeasuredQuantity(request.Command.Payload.ScrapQuantity),
                CompletionScrapReason,
                request.ServerReceivedAt);
        }

        request.State.OperationExecution.Complete(
            ToMeasuredQuantity(request.Command.Payload.GoodQuantity),
            request.ServerReceivedAt);

        ApplyOrderCompletionProgression(request.State);

        var preparedBatch = PrepareProductionActualsBatch(request.State, request.ServerReceivedAt);
        var response = new CompleteOperationResponseContract(
            true,
            request.Command.CommandId,
            request.ServerReceivedAt,
            request.State.OperationExecution.Id.ToString(),
            request.State.OperationExecution.Status.ToString(),
            request.State.OperationExecution.CompletedAt ?? request.ServerReceivedAt,
            preparedBatch.Status);

        return new CompleteOperationHandledCommandResult(
            evaluation.Decision,
            response,
            CreateAcceptedReceipt(
                request.Command,
                evaluation,
                nameof(OperationExecution),
                request.State.OperationExecution.Id.ToString(),
                response,
                request.ServerReceivedAt),
            preparedBatch);
    }
}
