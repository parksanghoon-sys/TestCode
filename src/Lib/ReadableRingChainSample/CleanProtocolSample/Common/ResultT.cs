namespace CleanProtocolSample.Common;

/// <summary>
/// 값을 포함하는 Result.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public string ErrorCode { get; }
    public string ErrorMessage { get; }

    private Result(
        bool isSuccess,
        T? value,
        string errorCode,
        string errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value)
        => new(true, value, string.Empty, string.Empty);

    public static Result<T> Fail(
        string errorCode,
        string errorMessage)
        => new(false, default, errorCode, errorMessage);
}