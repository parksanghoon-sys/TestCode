public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    EResultStatus Status { get; }
    string ErrorMessage { get; }
    bool HasValue { get; }
}
