using System.Text;

namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// 명시적 런타임 로더를 통해 네이티브 세션 API를 호출하는 기본 구현입니다.
/// </summary>
internal sealed class PInvokeNativeSessionApi : INativeSessionApi
{
    private static readonly NativeSessionLibraryCache SharedLibraryCache =
        new(RuntimeNativeLibraryPlatform.Instance);

    private readonly Lazy<NativeSessionLibrary> sessionLibrary;

    /// <summary>
    /// 기본 런타임 경로에서 네이티브 세션 API를 호출하는 구현을 생성합니다.
    /// </summary>
    public PInvokeNativeSessionApi()
        : this(() => SharedLibraryCache.Load(NativeLibraryPathResolver.GetExpectedLibraryPath()))
    {
    }

    /// <summary>
    /// 지정한 라이브러리 로더로 네이티브 세션 API 구현을 생성합니다.
    /// </summary>
    /// <param name="libraryFactory">네이티브 세션 라이브러리 생성기입니다.</param>
    internal PInvokeNativeSessionApi(Func<NativeSessionLibrary> libraryFactory)
    {
        sessionLibrary = new Lazy<NativeSessionLibrary>(libraryFactory);
    }

    /// <summary>
    /// 시나리오 JSON으로 세션을 생성합니다.
    /// </summary>
    public int OpenSession(
        string scenarioJson,
        StringBuilder sessionHandleBuffer,
        int sessionHandleCapacity,
        out int requiredSessionHandleCapacity,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity)
    {
        return sessionLibrary.Value.OpenSession(
            scenarioJson,
            sessionHandleBuffer,
            sessionHandleCapacity,
            out requiredSessionHandleCapacity,
            healthJsonBuffer,
            healthJsonCapacity,
            out requiredHealthJsonCapacity);
    }

    /// <summary>
    /// 세션을 종료합니다.
    /// </summary>
    public int StopSession(string sessionHandle)
    {
        return sessionLibrary.Value.StopSession(sessionHandle);
    }

    /// <summary>
    /// 활성 버스를 전환합니다.
    /// </summary>
    public int SwitchBus(string sessionHandle, int busLine)
    {
        return sessionLibrary.Value.SwitchBus(sessionHandle, busLine);
    }

    /// <summary>
    /// health snapshot JSON을 조회합니다.
    /// </summary>
    public int GetHealthSnapshot(
        string sessionHandle,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity)
    {
        return sessionLibrary.Value.GetHealthSnapshot(
            sessionHandle,
            healthJsonBuffer,
            healthJsonCapacity,
            out requiredHealthJsonCapacity);
    }

    /// <summary>
    /// pending telemetry JSON 배열을 조회합니다.
    /// </summary>
    public int PollTelemetry(
        string sessionHandle,
        StringBuilder telemetryJsonBuffer,
        int telemetryJsonCapacity,
        out int requiredTelemetryJsonCapacity)
    {
        return sessionLibrary.Value.PollTelemetry(
            sessionHandle,
            telemetryJsonBuffer,
            telemetryJsonCapacity,
            out requiredTelemetryJsonCapacity);
    }
}
