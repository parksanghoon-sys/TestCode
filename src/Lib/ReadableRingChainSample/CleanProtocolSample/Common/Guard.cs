namespace CleanProtocolSample.Common;
/// <summary>
/// 공통 방어 유틸리티.
/// 입력값 검증을 단순화한다.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Null 여부 검사
    /// </summary>
    /// <typeparam name="T">참조형 클래스</typeparam>
    /// <param name="value">클래스 값</param>
    /// <param name="paramName">파라미터 이름</param>
    /// <returns>Null 여부</returns>
    /// <exception cref="ArgumentNullException">Null 일시 에러</exception>
    public static T NotNull<T>(this T? value, string? paramName = null)
        where T : class
    {
        if( value is null)
            throw new ArgumentNullException(paramName ?? typeof(T).Name);
        return value;
    }
    /// <summary>
    /// 문자열 null/empty 검사.
    /// </summary>
    public static string NotNullOrWhiteSpace(
        this string? value,
        string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value cannot be null or whitespace.",
                paramName);
        }

        return value;
    }
    /// <summary>
    /// 숫자 양수 검사.
    /// </summary>
    public static int Positive(
        this int value,
        string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "Value must be positive.");
        }

        return value;
    }

    /// <summary>
    /// TimeSpan 양수 검사.
    /// </summary>
    public static TimeSpan Positive(
        TimeSpan value,
        string? paramName = null)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                "TimeSpan must be positive.");
        }

        return value;
    }
    /// <summary>
    /// 컬렉션 비어있는지 검사.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static IReadOnlyCollection<T> NotEmpty<T>(this IReadOnlyCollection<T>? collection, string? paramName = null)
    {
        if(collection is null)
            throw new ArgumentNullException(paramName);

        if(collection.Count == 0)
            throw new ArgumentException(
            "Collection cannot be empty.",
            paramName);

        return collection;
    }

}
