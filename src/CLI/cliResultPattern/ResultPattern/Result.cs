public class Result : IResult
{ 
    private readonly EResultStatus _status;
    private readonly string _errorMessage;
    public Result(EResultStatus status, string errorMessage)
    {
        _errorMessage = errorMessage;
        _status = status;
    }
    public bool IsSuccess => _status == EResultStatus.Success;
    public bool IsFailure => _status == EResultStatus.Failure;
    public EResultStatus Status => _status;
    public string ErrorMessage => _errorMessage;
    public virtual bool HasValue => false;

    public static Result Success() => new Result(EResultStatus.Success, string.Empty);
    public static Result Failure(string errorMessage) => new Result(EResultStatus.Failure, errorMessage);
    public static Result<TValue> Success<TValue>(TValue value) => new Result<TValue>(value, EResultStatus.Success, string.Empty);
    public static Result<TValue> Failure<TValue>(string errorMessage) => new Result<TValue>(default, EResultStatus.Failure, errorMessage);
    public static Result<TOutput> Try<IInput, TOutput>(IInput input, Func<IInput, TOutput> func)
    {
        try
        {
            var result = func(input);
            return Success(result);
        }
        catch (Exception ex)
        {
            return Failure<TOutput>($"함수 실행 중 오류 {ex.Message}");
        }
    }
}
