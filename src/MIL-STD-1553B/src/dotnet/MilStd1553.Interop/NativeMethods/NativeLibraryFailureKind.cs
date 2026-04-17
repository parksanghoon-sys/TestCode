namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 네이티브 라이브러리 적재 실패 유형을 나타냅니다.
/// </summary>
public enum NativeLibraryFailureKind
{
    /// <summary>
    /// 런타임 경로에 라이브러리가 없습니다.
    /// </summary>
    LibraryNotFound = 1,

    /// <summary>
    /// 라이브러리 파일이 잘못되었거나 현재 프로세스가 적재할 수 없습니다.
    /// </summary>
    InvalidBinary = 2,

    /// <summary>
    /// 필요한 엔트리 포인트를 라이브러리에서 찾지 못했습니다.
    /// </summary>
    EntryPointMissing = 3,
}
