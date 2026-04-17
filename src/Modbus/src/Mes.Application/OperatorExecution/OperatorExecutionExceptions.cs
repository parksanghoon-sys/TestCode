namespace Mes.Application.OperatorExecution;

/// <summary>
/// operator-execution problem details가 함께 전달할 오류 문맥을 정의합니다.
/// </summary>
/// <param name="AggregateType">오류와 연관된 aggregate 종류입니다.</param>
/// <param name="AggregateId">오류와 연관된 aggregate 식별자입니다.</param>
/// <param name="CommandId">오류와 연관된 command 식별자입니다.</param>
/// <param name="IdempotencyKey">오류와 연관된 idempotency key입니다.</param>
public sealed record OperatorExecutionErrorContext(
    string? AggregateType,
    string? AggregateId,
    string? CommandId,
    string? IdempotencyKey);

/// <summary>
/// operator-execution slice에서 HTTP problem details로 승격될 수 있는 결정적 오류의 기본 형식입니다.
/// </summary>
public abstract class OperatorExecutionRequestException : InvalidOperationException
{
    /// <summary>
    /// operator-execution 요청 예외를 초기화합니다.
    /// </summary>
    /// <param name="message">오류 메시지입니다.</param>
    /// <param name="errorCode">안정적인 문제 코드입니다.</param>
    /// <param name="context">오류 문맥입니다.</param>
    protected OperatorExecutionRequestException(
        string message,
        string errorCode,
        OperatorExecutionErrorContext? context = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Context = context;
    }

    /// <summary>
    /// host가 problem details에 기록할 안정적인 오류 코드입니다.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// host가 함께 노출할 선택적 오류 문맥입니다.
    /// </summary>
    public OperatorExecutionErrorContext? Context { get; }
}

/// <summary>
/// authoritative aggregate 또는 조회 대상이 없을 때 사용하는 예외입니다.
/// </summary>
public sealed class OperatorExecutionNotFoundException : OperatorExecutionRequestException
{
    /// <summary>
    /// not-found 예외를 초기화합니다.
    /// </summary>
    /// <param name="message">오류 메시지입니다.</param>
    /// <param name="context">오류 문맥입니다.</param>
    public OperatorExecutionNotFoundException(
        string message,
        OperatorExecutionErrorContext? context = null)
        : base(message, "operator_execution.not_found", context)
    {
    }
}

/// <summary>
/// idempotency 또는 현재 상태 충돌에 사용하는 예외입니다.
/// </summary>
public sealed class OperatorExecutionConflictException : OperatorExecutionRequestException
{
    /// <summary>
    /// conflict 예외를 초기화합니다.
    /// </summary>
    /// <param name="message">오류 메시지입니다.</param>
    /// <param name="context">오류 문맥입니다.</param>
    public OperatorExecutionConflictException(
        string message,
        OperatorExecutionErrorContext? context = null)
        : base(message, "operator_execution.conflict", context)
    {
    }
}

/// <summary>
/// authoritative MES 상태와 맞지 않는 의미적 요청 오류에 사용하는 예외입니다.
/// </summary>
public sealed class OperatorExecutionValidationException : OperatorExecutionRequestException
{
    /// <summary>
    /// validation 예외를 초기화합니다.
    /// </summary>
    /// <param name="message">오류 메시지입니다.</param>
    /// <param name="context">오류 문맥입니다.</param>
    public OperatorExecutionValidationException(
        string message,
        OperatorExecutionErrorContext? context = null)
        : base(message, "operator_execution.validation_failed", context)
    {
    }
}
