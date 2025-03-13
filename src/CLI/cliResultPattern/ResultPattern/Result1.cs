
// 제네릭 결과 구현 (값을 포함)
public class Result<TValue> : Result, IResult<TValue>
{
    private readonly TValue _value;
    private readonly bool _hasValue;
    public Result(TValue value, EResultStatus status, string errorMessage) : base(status, errorMessage)
    {
        _value = value;
        _hasValue = IsSuccess && value != null;
    }
    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException();
            }
            return _value;
        }
    }

    public override bool HasValue => _hasValue;
    /// <summary>
    /// 결과 값에 함수 적용 (매핑)
    /// </summary>
    /// <typeparam name="TOutput"></typeparam>
    /// <param name="mapper"></param>
    /// <returns></returns>
    public Result<TOutput> Map<TOutput>(Func<TValue, TOutput> mapper)
    {
        return IsFailure ? Result.Failure<TOutput>(ErrorMessage) : Result.Success(mapper(Value));
    }
    public Result<TValue> Ensure(Predicate<TValue> predicate, string errorMessage)
    {
        if (IsFailure)
        {
            return this;
        }
        return predicate(Value) ? this : Result.Failure<TValue>(errorMessage);
    }
    public Result<TOutput> Cast<TOutput>() where TOutput : class
    {
        if(IsFailure)
            return Failure<TOutput>(ErrorMessage);
        if (Value is TOutput output)
        {
            return Result.Success(output);
        }
        return Result.Failure<TOutput>($"캐스팅 실패: {typeof(TOutput)}");
    }
}
