using System.Text;

namespace MilStd1553.Interop.NativeMethods;

/// <summary>
/// NativeHarnessClient가 호출하는 저수준 세션 중심 네이티브 API 계약입니다.
/// </summary>
internal interface INativeSessionApi
{
    /// <summary>
    /// 시나리오 JSON으로 세션을 생성합니다.
    /// </summary>
    /// <param name="scenarioJson">직렬화된 시나리오 JSON입니다.</param>
    /// <param name="sessionHandleBuffer">세션 핸들 수신 버퍼입니다.</param>
    /// <param name="sessionHandleCapacity">세션 핸들 버퍼 길이입니다.</param>
    /// <param name="healthJsonBuffer">health snapshot JSON 수신 버퍼입니다.</param>
    /// <param name="healthJsonCapacity">health JSON 버퍼 길이입니다.</param>
    /// <returns>네이티브 상태 코드입니다.</returns>
    int OpenSession(
        string scenarioJson,
        StringBuilder sessionHandleBuffer,
        int sessionHandleCapacity,
        out int requiredSessionHandleCapacity,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity);

    /// <summary>
    /// 세션을 종료합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <returns>네이티브 상태 코드입니다.</returns>
    int StopSession(string sessionHandle);

    /// <summary>
    /// 활성 버스를 전환합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="busLine">전환할 버스 값입니다.</param>
    /// <returns>네이티브 상태 코드입니다.</returns>
    int SwitchBus(string sessionHandle, int busLine);

    /// <summary>
    /// health snapshot JSON을 조회합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="healthJsonBuffer">health JSON 수신 버퍼입니다.</param>
    /// <param name="healthJsonCapacity">health JSON 버퍼 길이입니다.</param>
    /// <returns>네이티브 상태 코드입니다.</returns>
    int GetHealthSnapshot(
        string sessionHandle,
        StringBuilder healthJsonBuffer,
        int healthJsonCapacity,
        out int requiredHealthJsonCapacity);

    /// <summary>
    /// pending telemetry JSON 배열을 조회합니다.
    /// </summary>
    /// <param name="sessionHandle">대상 세션 핸들입니다.</param>
    /// <param name="telemetryJsonBuffer">telemetry JSON 수신 버퍼입니다.</param>
    /// <param name="telemetryJsonCapacity">telemetry JSON 버퍼 길이입니다.</param>
    /// <returns>네이티브 상태 코드입니다.</returns>
    int PollTelemetry(
        string sessionHandle,
        StringBuilder telemetryJsonBuffer,
        int telemetryJsonCapacity,
        out int requiredTelemetryJsonCapacity);
}
