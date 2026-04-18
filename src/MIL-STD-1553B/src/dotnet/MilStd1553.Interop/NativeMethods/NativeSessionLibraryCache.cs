namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 경로별 네이티브 세션 라이브러리를 프로세스 범위에서 재사용합니다.
/// </summary>
internal sealed class NativeSessionLibraryCache : IDisposable
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, NativeSessionLibrary> libraries;
    private readonly INativeLibraryPlatform platform;
    private bool disposed;

    /// <summary>
    /// 지정한 플랫폼 구현으로 캐시를 생성합니다.
    /// </summary>
    /// <param name="platform">네이티브 라이브러리 적재 플랫폼입니다.</param>
    internal NativeSessionLibraryCache(INativeLibraryPlatform platform)
    {
        this.platform = platform;
        libraries = new Dictionary<string, NativeSessionLibrary>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 지정한 경로의 네이티브 세션 라이브러리를 적재하거나 캐시에서 재사용합니다.
    /// </summary>
    /// <param name="libraryPath">적재할 라이브러리 경로입니다.</param>
    /// <returns>캐시된 네이티브 세션 라이브러리입니다.</returns>
    internal NativeSessionLibrary Load(string libraryPath)
    {
        ThrowIfDisposed();

        lock (syncRoot)
        {
            if (libraries.TryGetValue(libraryPath, out var cachedLibrary))
            {
                return cachedLibrary;
            }

            var loadedLibrary = NativeSessionLibrary.Load(libraryPath, platform);
            libraries.Add(libraryPath, loadedLibrary);
            return loadedLibrary;
        }
    }

    /// <summary>
    /// 캐시에 남아 있는 네이티브 라이브러리 핸들을 정리합니다.
    /// </summary>
    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            foreach (var library in libraries.Values)
            {
                library.ReleaseForTestsOnly();
            }

            libraries.Clear();
            disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(NativeSessionLibraryCache));
        }
    }
}
