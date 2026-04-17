using System.Runtime.InteropServices;

namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 네이티브 런타임 라이브러리 배치 경로를 계산합니다.
/// </summary>
internal static class NativeLibraryPathResolver
{
    internal const string RuntimeRootEnvironmentVariableName = "MILSTD1553_NATIVE_RUNTIME_ROOT";

    /// <summary>
    /// 현재 환경 기준 예상 라이브러리 경로를 계산합니다.
    /// </summary>
    /// <returns>예상 라이브러리 절대 경로입니다.</returns>
    internal static string GetExpectedLibraryPath()
    {
        return GetExpectedLibraryPath(GetRuntimeRoot());
    }

    /// <summary>
    /// 지정한 런타임 루트 기준 예상 라이브러리 경로를 계산합니다.
    /// </summary>
    /// <param name="runtimeRootPath">런타임 루트 경로입니다.</param>
    /// <returns>예상 라이브러리 절대 경로입니다.</returns>
    internal static string GetExpectedLibraryPath(string runtimeRootPath)
    {
        return Path.Combine(
            runtimeRootPath,
            "runtimes",
            GetRuntimeIdentifier(),
            "native",
            NativeMethods.LibraryFileName);
    }

    private static string GetRuntimeRoot()
    {
        var configuredRuntimeRoot = Environment.GetEnvironmentVariable(RuntimeRootEnvironmentVariableName);
        return string.IsNullOrWhiteSpace(configuredRuntimeRoot)
            ? AppContext.BaseDirectory
            : configuredRuntimeRoot;
    }

    private static string GetRuntimeIdentifier()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("현재 MVP 런타임 로더는 Windows 배치 경로만 지원합니다.");
        }

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.Arm64 => "win-arm64",
            _ => throw new PlatformNotSupportedException("현재 MVP 런타임 로더는 x64 또는 arm64 아키텍처만 지원합니다."),
        };
    }
}
