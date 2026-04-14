using Mes.Domain.Common;

namespace Mes.Domain.ValueObjects;

/// <summary>
/// 생산 오더 식별자를 표현합니다.
/// </summary>
public readonly record struct ProductionOrderId
{
    /// <summary>
    /// 생산 오더 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">생산 오더 원시 식별자 값입니다.</param>
    public ProductionOrderId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}

/// <summary>
/// 공정 실행 식별자를 표현합니다.
/// </summary>
public readonly record struct OperationExecutionId
{
    /// <summary>
    /// 공정 실행 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">공정 실행 원시 식별자 값입니다.</param>
    public OperationExecutionId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}

/// <summary>
/// WIP 단위 식별자를 표현합니다.
/// </summary>
public readonly record struct WipUnitId
{
    /// <summary>
    /// WIP 단위 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">WIP 원시 식별자 값입니다.</param>
    public WipUnitId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}

/// <summary>
/// 자재 Lot 식별자를 표현합니다.
/// </summary>
public readonly record struct MaterialLotId
{
    /// <summary>
    /// 자재 Lot 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">자재 Lot 원시 식별자 값입니다.</param>
    public MaterialLotId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}

/// <summary>
/// 품질 기록 식별자를 표현합니다.
/// </summary>
public readonly record struct QualityRecordId
{
    /// <summary>
    /// 품질 기록 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">품질 기록 원시 식별자 값입니다.</param>
    public QualityRecordId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}

/// <summary>
/// 예외 승인 요청 식별자를 표현합니다.
/// </summary>
public readonly record struct OverrideRequestId
{
    /// <summary>
    /// 예외 승인 요청 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">예외 승인 요청 원시 식별자 값입니다.</param>
    public OverrideRequestId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}

/// <summary>
/// 작업 스테이션 식별자를 표현합니다.
/// </summary>
public readonly record struct StationId
{
    /// <summary>
    /// 작업 스테이션 식별자를 초기화합니다.
    /// </summary>
    /// <param name="value">작업 스테이션 원시 식별자 값입니다.</param>
    public StationId(string value)
    {
        Value = DomainGuard.NotWhiteSpace(value, nameof(value));
    }

    public string Value { get; }

    /// <summary>
    /// 식별자를 문자열로 반환합니다.
    /// </summary>
    /// <returns>원본 식별자 문자열입니다.</returns>
    public override string ToString() => Value;
}
