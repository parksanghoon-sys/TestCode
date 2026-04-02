namespace ReadableRingChainSample.Core;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    protected Result(bool isSucess, string errorCode, string errorMessage)
    {
        IsSuccess = isSucess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
    public static Result Success() => new Result(true, string.Empty, string.Empty);
    public static Result Failure(string errorCode, string errorMessage) => new Result(false, errorCode, errorMessage);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);    
    public override string ToString()
    {
         return IsSuccess? "Success" : $"Failure: {ErrorCode} - {ErrorMessage}";
    }
}
public sealed class Result<T> : Result
{
    public T? Value { get; }
    private Result(bool isSucess, T? value, string errorCode, string errorMessage) : base(isSucess, errorCode, errorMessage)
    {
        Value = value;
    }
    public static Result<T> Success(T value) => new(true, value, string.Empty, string.Empty);
    public static new Result<T> Failure(string errorCode, string errorMessage) => new (false, default,errorCode, errorMessage);
}
