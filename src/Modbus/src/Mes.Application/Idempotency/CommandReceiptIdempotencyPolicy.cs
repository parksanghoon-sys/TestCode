namespace Mes.Application.Idempotency;

/// <summary>
/// command receipt의 replay 및 conflict 판정 규칙을 제공합니다.
/// </summary>
public sealed class CommandReceiptIdempotencyPolicy
{
    /// <summary>
    /// 현재 요청과 기존 receipt를 비교해 idempotency 판정을 수행합니다.
    /// </summary>
    /// <param name="request">판정할 요청 정보입니다.</param>
    /// <returns>신규 수락, 재생, 충돌 중 하나의 판정 결과입니다.</returns>
    public CommandReceiptEvaluationResult Evaluate(CommandReceiptEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scope);

        var normalizedFingerprint = NormalizeRequired(request.RequestFingerprint, nameof(request.RequestFingerprint));

        if (request.ExistingReceipt is null)
        {
            return new CommandReceiptEvaluationResult(
                CommandReceiptDecisionKind.AcceptNew,
                null,
                normalizedFingerprint);
        }

        if (!string.Equals(request.ExistingReceipt.Scope.Channel, request.Scope.Channel, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(request.ExistingReceipt.Scope.CommandType, request.Scope.CommandType, StringComparison.Ordinal)
            || !string.Equals(request.ExistingReceipt.Scope.IdempotencyKey, request.Scope.IdempotencyKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Existing receipt scope must match the evaluated incoming scope.");
        }

        return string.Equals(request.ExistingReceipt.RequestFingerprint, normalizedFingerprint, StringComparison.Ordinal)
            ? new CommandReceiptEvaluationResult(
                CommandReceiptDecisionKind.ReplayStored,
                request.ExistingReceipt,
                normalizedFingerprint)
            : new CommandReceiptEvaluationResult(
                CommandReceiptDecisionKind.Conflict,
                request.ExistingReceipt,
                normalizedFingerprint);
    }

    /// <summary>
    /// 수락된 결과를 replay 가능한 receipt 레코드로 정규화합니다.
    /// </summary>
    /// <param name="request">receipt 생성 요청입니다.</param>
    /// <returns>저장 가능한 receipt 레코드입니다.</returns>
    public CommandReceiptRecord CreateAcceptedReceipt(RegisterAcceptedCommandReceiptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scope);

        return new CommandReceiptRecord(
            NormalizeRequired(request.CommandId, nameof(request.CommandId)),
            new CommandReceiptScope(
                NormalizeRequired(request.Scope.Channel, nameof(request.Scope.Channel)).ToLowerInvariant(),
                NormalizeRequired(request.Scope.CommandType, nameof(request.Scope.CommandType)),
                NormalizeRequired(request.Scope.IdempotencyKey, nameof(request.Scope.IdempotencyKey))),
            NormalizeRequired(request.ActorId, nameof(request.ActorId)),
            NormalizeOptional(request.StationId),
            NormalizeRequired(request.CorrelationId, nameof(request.CorrelationId)),
            NormalizeRequired(request.RequestFingerprint, nameof(request.RequestFingerprint)),
            NormalizeRequired(request.AggregateType, nameof(request.AggregateType)),
            NormalizeRequired(request.AggregateId, nameof(request.AggregateId)),
            request.AcceptedAt,
            CommandReceiptResultCodeValues.Accepted,
            NormalizeOptional(request.ResponseJson));
    }

    /// <summary>
    /// 필수 문자열을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 매개변수 이름입니다.</param>
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
    /// 선택 문자열을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <returns>trim된 문자열 또는 <see langword="null"/>입니다.</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
