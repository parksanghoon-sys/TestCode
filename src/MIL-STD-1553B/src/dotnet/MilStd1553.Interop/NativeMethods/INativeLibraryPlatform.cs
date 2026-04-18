namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 네이티브 라이브러리 적재와 엔트리 포인트 조회를 추상화합니다.
/// </summary>
internal interface INativeLibraryPlatform
{
    /// <summary>
    /// 지정한 경로의 네이티브 라이브러리를 적재합니다.
    /// </summary>
    /// <param name="libraryPath">적재할 라이브러리 경로입니다.</param>
    /// <returns>적재된 라이브러리 핸들입니다.</returns>
    nint Load(string libraryPath);

    /// <summary>
    /// 지정한 라이브러리 핸들에서 엔트리 포인트를 조회합니다.
    /// </summary>
    /// <param name="libraryHandle">대상 라이브러리 핸들입니다.</param>
    /// <param name="exportName">조회할 엔트리 포인트 이름입니다.</param>
    /// <returns>엔트리 포인트 함수 포인터입니다.</returns>
    nint GetExport(nint libraryHandle, string exportName);

    /// <summary>
    /// 지정한 라이브러리 핸들을 해제합니다.
    /// </summary>
    /// <param name="libraryHandle">해제할 라이브러리 핸들입니다.</param>
    void Free(nint libraryHandle);
}
