namespace MilStd1553.Host.Exceptions;

/// <summary>
/// Host 단계의 시나리오 검증 실패를 나타냅니다.
/// </summary>
public sealed class ScenarioValidationException : Exception
{
    /// <summary>
    /// 시나리오 검증 예외를 생성합니다.
    /// </summary>
    /// <param name="errors">검증 실패 목록입니다.</param>
    public ScenarioValidationException(IReadOnlyList<string> errors)
        : base(CreateMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// 검증 실패 목록입니다.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    private static string CreateMessage(IReadOnlyList<string> errors)
    {
        return errors.Count == 0
            ? "시나리오 검증에 실패했습니다."
            : "시나리오 검증에 실패했습니다: " + string.Join("; ", errors);
    }
}
