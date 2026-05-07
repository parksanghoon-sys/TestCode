namespace CleanProtocolSample.Common;
/// <summary>
/// 성공/실패 상태를 표현하는 기본 Result.
/// 예외 대신 명시적인 오류 흐름 제어를 위해 사용.
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string ErrorCode { get; } = string.Empty;
    public string ErrorMessage { get; } = string.Empty;

    private Result(
    bool isSuccess,
    string errorCode,
    string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 성공 Result 생성.
    /// </summary>
    public static Result Success()
        => new(true, string.Empty, string.Empty);

    /// <summary>
    /// 실패 Result 생성.
    /// </summary>
    public static Result Fail(
        string errorCode,
        string errorMessage)
        => new(false, errorCode, errorMessage);
}
