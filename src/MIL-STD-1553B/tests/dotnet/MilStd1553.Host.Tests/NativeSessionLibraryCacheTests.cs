using System.Diagnostics;
using MilStd1553.Interop.NativeMethods;

namespace MilStd1553.Host.Tests;

/// <summary>
/// 네이티브 런타임 로더의 경로별 캐시와 테스트용 dispose 정책을 검증합니다.
/// </summary>
[Collection("Native runtime loader")]
public sealed class NativeSessionLibraryCacheTests
{
    /// <summary>
    /// 동일한 경로를 여러 번 요청해도 라이브러리는 한 번만 적재하고 즉시 해제하지 않아야 합니다.
    /// </summary>
    [Fact]
    public void Load_WhenPathIsSame_ReusesCachedLibraryWithoutImmediateFree()
    {
        var platform = new CountingNativeLibraryPlatform();
        var cache = new NativeSessionLibraryCache(platform);
        var libraryPath = GetBuiltNativeLibraryPath();

        var firstLibrary = cache.Load(libraryPath);
        var secondLibrary = cache.Load(libraryPath);

        Assert.Same(firstLibrary, secondLibrary);
        Assert.Equal(1, platform.LoadCallCount);
        Assert.Equal(0, platform.FreeCallCount);

        cache.Dispose();
    }

    /// <summary>
    /// 테스트용 dispose는 캐시에 남은 라이브러리 핸들을 한 번만 해제하고 idempotent 해야 합니다.
    /// </summary>
    [Fact]
    public void Dispose_WhenCacheWasLoaded_FreesHandleOnce()
    {
        var platform = new CountingNativeLibraryPlatform();
        var cache = new NativeSessionLibraryCache(platform);

        cache.Load(GetBuiltNativeLibraryPath());

        Assert.Equal(1, platform.LoadCallCount);
        Assert.Equal(0, platform.FreeCallCount);

        cache.Dispose();
        cache.Dispose();

        Assert.Equal(1, platform.FreeCallCount);
    }

    /// <summary>
    /// 테스트에 사용할 실제 네이티브 DLL 경로를 찾습니다.
    /// </summary>
    /// <returns>빌드된 네이티브 DLL 경로입니다.</returns>
    private static string GetBuiltNativeLibraryPath()
    {
        var repositoryRoot = FindRepositoryRoot();
        BuildNativeLibraryArtifact(repositoryRoot.FullName);
        var nativeLibraryPath = Path.Combine(
            repositoryRoot.FullName,
            "tests",
            "native",
            "artifacts",
            "MilStd1553.Native.dll");

        Assert.True(File.Exists(nativeLibraryPath), $"테스트용 네이티브 DLL이 없습니다: {nativeLibraryPath}");
        return nativeLibraryPath;
    }

    /// <summary>
    /// 캐시 테스트에 사용할 실제 네이티브 DLL을 빌드합니다.
    /// </summary>
    /// <param name="repositoryRootPath">저장소 루트 경로입니다.</param>
    private static void BuildNativeLibraryArtifact(string repositoryRootPath)
    {
        var buildScriptPath = Path.Combine(repositoryRootPath, "tests", "native", "build_native_artifact.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{buildScriptPath}\" -ArtifactType NativeDll",
            WorkingDirectory = repositoryRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("네이티브 DLL 빌드 프로세스를 시작하지 못했습니다.");

        process.WaitForExit();
        if (process.ExitCode == 0)
        {
            return;
        }

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        throw new InvalidOperationException(
            $"네이티브 DLL 빌드가 실패했습니다.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
    }

    /// <summary>
    /// 저장소 루트를 탐색합니다.
    /// </summary>
    /// <returns>저장소 루트 디렉터리입니다.</returns>
    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENT_RULES.md")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("저장소 루트를 찾지 못했습니다.");
    }

    private sealed class CountingNativeLibraryPlatform : INativeLibraryPlatform
    {
        public int LoadCallCount { get; private set; }

        public int FreeCallCount { get; private set; }

        public nint Load(string libraryPath)
        {
            LoadCallCount++;
            return RuntimeNativeLibraryPlatform.Instance.Load(libraryPath);
        }

        public nint GetExport(nint libraryHandle, string exportName)
        {
            return RuntimeNativeLibraryPlatform.Instance.GetExport(libraryHandle, exportName);
        }

        public void Free(nint libraryHandle)
        {
            FreeCallCount++;
            RuntimeNativeLibraryPlatform.Instance.Free(libraryHandle);
        }
    }
}
