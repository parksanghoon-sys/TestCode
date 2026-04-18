using System.Runtime.InteropServices;

namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 운영 환경의 `NativeLibrary` API를 감싼 기본 플랫폼 구현입니다.
/// </summary>
internal sealed class RuntimeNativeLibraryPlatform : INativeLibraryPlatform
{
    /// <summary>
    /// 기본 플랫폼 싱글턴입니다.
    /// </summary>
    internal static RuntimeNativeLibraryPlatform Instance { get; } = new();

    private RuntimeNativeLibraryPlatform()
    {
    }

    /// <summary>
    /// 지정한 경로의 네이티브 라이브러리를 적재합니다.
    /// </summary>
    /// <param name="libraryPath">적재할 라이브러리 경로입니다.</param>
    /// <returns>적재된 라이브러리 핸들입니다.</returns>
    public nint Load(string libraryPath)
    {
        return NativeLibrary.Load(libraryPath);
    }

    /// <summary>
    /// 지정한 라이브러리 핸들에서 엔트리 포인트를 조회합니다.
    /// </summary>
    /// <param name="libraryHandle">대상 라이브러리 핸들입니다.</param>
    /// <param name="exportName">조회할 엔트리 포인트 이름입니다.</param>
    /// <returns>엔트리 포인트 함수 포인터입니다.</returns>
    public nint GetExport(nint libraryHandle, string exportName)
    {
        return NativeLibrary.GetExport(libraryHandle, exportName);
    }

    /// <summary>
    /// 지정한 라이브러리 핸들을 해제합니다.
    /// </summary>
    /// <param name="libraryHandle">해제할 라이브러리 핸들입니다.</param>
    public void Free(nint libraryHandle)
    {
        NativeLibrary.Free(libraryHandle);
    }
}
