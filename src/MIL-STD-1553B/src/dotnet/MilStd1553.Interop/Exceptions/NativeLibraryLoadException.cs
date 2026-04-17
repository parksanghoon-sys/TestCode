using MilStd1553.Interop.NativeMethods;

namespace MilStd1553.Interop.Exceptions;

/// <summary>
/// 네이티브 런타임 라이브러리 적재 또는 엔트리 포인트 해석 실패를 나타냅니다.
/// </summary>
public sealed class NativeLibraryLoadException : InvalidOperationException
{
    /// <summary>
    /// 실패 유형을 가져옵니다.
    /// </summary>
    public NativeLibraryFailureKind FailureKind { get; }

    /// <summary>
    /// 실패한 네이티브 작업 이름을 가져옵니다.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// 적재를 시도한 라이브러리 경로를 가져옵니다.
    /// </summary>
    public string LibraryPath { get; }

    /// <summary>
    /// 예외를 생성합니다.
    /// </summary>
    /// <param name="failureKind">적재 실패 유형입니다.</param>
    /// <param name="operationName">실패한 네이티브 작업 이름입니다.</param>
    /// <param name="libraryPath">적재를 시도한 라이브러리 경로입니다.</param>
    /// <param name="message">예외 메시지입니다.</param>
    /// <param name="innerException">원본 예외입니다.</param>
    public NativeLibraryLoadException(
        NativeLibraryFailureKind failureKind,
        string operationName,
        string libraryPath,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        OperationName = operationName;
        LibraryPath = libraryPath;
    }
}
