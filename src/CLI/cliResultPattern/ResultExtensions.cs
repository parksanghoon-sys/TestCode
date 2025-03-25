
// 결과 출력 헬퍼 메서드
public static class ResultExtensions
{
    public static void Display<T>(this IResult<T> result, string successPrefix = "성공")
    {
        if (result.IsSuccess)
        {
            Console.WriteLine($"{successPrefix}: {result.Value}");
        }
        else
        {
            Console.WriteLine($"실패: {result.ErrorMessage}");
        }
    }
}
