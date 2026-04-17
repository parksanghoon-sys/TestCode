namespace MilStd1553.Interop.Exceptions;

/// <summary>
/// 네이티브 C ABI 호출 실패를 표현합니다.
/// </summary>
public sealed class NativeInteropException : Exception
{
    /// <summary>
    /// 네이티브 상태 코드를 보관합니다.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// 예외를 생성합니다.
    /// </summary>
    /// <param name="statusCode">네이티브 상태 코드입니다.</param>
    /// <param name="message">예외 메시지입니다.</param>
    public NativeInteropException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
