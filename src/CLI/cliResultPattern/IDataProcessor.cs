
// 데이터 프로세서 인터페이스
public interface IDataProcessor<in TInput, out TOutput>
{
    IResult<TOutput> Process(TInput input);
}
