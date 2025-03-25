public class FunctionalProcessor<TInput, TOutput> : IDataProcessor<TInput, TOutput>
{
    private readonly Func<TInput, IResult<TOutput>> _processor;
    public FunctionalProcessor(Func<TInput, IResult<TOutput>> processFunc)
    {
        _processor = processFunc;
    }
    public FunctionalProcessor(Func<TInput,TOutput> func)
    {
        _processor = input => Result.Try(input, func);
    }
    public IResult<TOutput> Process(TInput input)
    {
        return _processor(input);
    }
}
