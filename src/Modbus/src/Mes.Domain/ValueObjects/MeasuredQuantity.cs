using Mes.Domain.Common;

namespace Mes.Domain.ValueObjects;

/// <summary>
/// 단위가 포함된 수량 값을 표현합니다.
/// </summary>
public readonly record struct MeasuredQuantity
{
    /// <summary>
    /// 수량과 단위를 사용해 측정 수량을 초기화합니다.
    /// </summary>
    /// <param name="value">수량 값입니다.</param>
    /// <param name="unit">수량 단위입니다.</param>
    public MeasuredQuantity(decimal value, string unit)
    {
        Value = DomainGuard.NonNegative(value, nameof(value));
        Unit = DomainGuard.NotWhiteSpace(unit, nameof(unit)).ToUpperInvariant();
    }

    public decimal Value { get; }

    public string Unit { get; }

    /// <summary>
    /// 지정한 단위의 0 수량을 생성합니다.
    /// </summary>
    /// <param name="unit">수량 단위입니다.</param>
    /// <returns>값이 0인 측정 수량입니다.</returns>
    public static MeasuredQuantity Zero(string unit) => new(0m, unit);

    /// <summary>
    /// 같은 단위의 수량을 현재 수량에 더합니다.
    /// </summary>
    /// <param name="other">더할 수량입니다.</param>
    /// <returns>합산된 새 측정 수량입니다.</returns>
    public MeasuredQuantity Add(MeasuredQuantity other)
    {
        EnsureSameUnit(other);
        return new MeasuredQuantity(Value + other.Value, Unit);
    }

    /// <summary>
    /// 같은 단위의 수량을 현재 수량에서 차감합니다.
    /// </summary>
    /// <param name="other">차감할 수량입니다.</param>
    /// <returns>차감된 새 측정 수량입니다.</returns>
    public MeasuredQuantity Subtract(MeasuredQuantity other)
    {
        EnsureSameUnit(other);
        DomainGuard.Against(other.Value > Value, "Quantity cannot go below zero.");
        return new MeasuredQuantity(Value - other.Value, Unit);
    }

    /// <summary>
    /// 비교 대상 수량의 단위가 현재 단위와 같은지 확인합니다.
    /// </summary>
    /// <param name="other">단위를 비교할 수량입니다.</param>
    private void EnsureSameUnit(MeasuredQuantity other)
    {
        DomainGuard.Against(!string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase), "Quantity units must match.");
    }
}
