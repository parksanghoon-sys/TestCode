using Mes.Domain.Abstractions;

namespace Mes.Domain.Common;

/// <summary>
/// 도메인 모델 전반에서 공통으로 사용하는 검증 도우미를 제공합니다.
/// </summary>
internal static class DomainGuard
{
    /// <summary>
    /// 지정한 조건이 참이면 도메인 예외를 발생시킵니다.
    /// </summary>
    /// <param name="condition">예외를 발생시킬 조건입니다.</param>
    /// <param name="message">조건 위반 시 사용할 메시지입니다.</param>
    public static void Against(bool condition, string message)
    {
        if (condition)
        {
            throw new DomainException(message);
        }
    }

    /// <summary>
    /// 공백이 아닌 문자열 값을 정규화하여 반환합니다.
    /// </summary>
    /// <param name="value">검증할 문자열입니다.</param>
    /// <param name="parameterName">오류 메시지에 사용할 매개변수 이름입니다.</param>
    /// <returns>앞뒤 공백이 제거된 문자열입니다.</returns>
    public static string NotWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{parameterName} cannot be empty.");
        }

        return value.Trim();
    }

    /// <summary>
    /// 값이 0보다 큰 양수인지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 정수 값입니다.</param>
    /// <param name="parameterName">오류 메시지에 사용할 매개변수 이름입니다.</param>
    /// <returns>검증을 통과한 원본 값입니다.</returns>
    public static int Positive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{parameterName} must be greater than zero.");
        }

        return value;
    }

    /// <summary>
    /// 값이 음수가 아닌지 검증합니다.
    /// </summary>
    /// <param name="value">검증할 수량 값입니다.</param>
    /// <param name="parameterName">오류 메시지에 사용할 매개변수 이름입니다.</param>
    /// <returns>검증을 통과한 원본 값입니다.</returns>
    public static decimal NonNegative(decimal value, string parameterName)
    {
        if (value < 0)
        {
            throw new DomainException($"{parameterName} cannot be negative.");
        }

        return value;
    }
}
