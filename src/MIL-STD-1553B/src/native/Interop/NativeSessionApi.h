#pragma once

#include <cstdint>

#if defined(_WIN32)
#define MILSTD1553_NATIVE_EXPORT __declspec(dllexport)
#else
#define MILSTD1553_NATIVE_EXPORT
#endif

namespace MilStd1553::Interop
{
/// <summary>
/// 세션 중심 C ABI가 반환하는 상태 코드를 나타냅니다.
/// </summary>
enum class NativeCallStatus : std::int32_t
{
    Success = 0,
    InvalidArgument = 1,
    SessionNotFound = 2,
    BufferTooSmall = 3,
    InternalError = 4,
};
}

extern "C"
{
/// <summary>
/// 시나리오 JSON으로 네이티브 세션을 생성합니다.
/// </summary>
/// <param name="scenarioJson">시작할 시나리오 JSON입니다.</param>
/// <param name="sessionHandleBuffer">세션 핸들을 받을 UTF-16 버퍼입니다.</param>
/// <param name="sessionHandleCapacity">세션 핸들 버퍼 길이입니다.</param>
/// <param name="requiredSessionHandleCapacity">세션 핸들에 필요한 UTF-16 버퍼 길이입니다. 널 종료를 포함합니다.</param>
/// <param name="healthJsonBuffer">초기 health snapshot JSON을 받을 UTF-16 버퍼입니다.</param>
/// <param name="healthJsonCapacity">health JSON 버퍼 길이입니다.</param>
/// <param name="requiredHealthJsonCapacity">health JSON에 필요한 UTF-16 버퍼 길이입니다. 널 종료를 포함합니다.</param>
/// <returns>네이티브 호출 상태 코드입니다.</returns>
MILSTD1553_NATIVE_EXPORT int MilStd1553_OpenSession(
    const wchar_t* scenarioJson,
    wchar_t* sessionHandleBuffer,
    int sessionHandleCapacity,
    int* requiredSessionHandleCapacity,
    wchar_t* healthJsonBuffer,
    int healthJsonCapacity,
    int* requiredHealthJsonCapacity);

/// <summary>
/// 네이티브 세션을 종료합니다.
/// </summary>
/// <param name="sessionHandle">종료할 세션 핸들입니다.</param>
/// <returns>네이티브 호출 상태 코드입니다.</returns>
MILSTD1553_NATIVE_EXPORT int MilStd1553_StopSession(const wchar_t* sessionHandle);

/// <summary>
/// 네이티브 세션의 활성 버스를 전환합니다.
/// </summary>
/// <param name="sessionHandle">대상 세션 핸들입니다.</param>
/// <param name="busLine">전환할 버스 값입니다. 0은 A, 1은 B입니다.</param>
/// <returns>네이티브 호출 상태 코드입니다.</returns>
MILSTD1553_NATIVE_EXPORT int MilStd1553_SwitchBus(
    const wchar_t* sessionHandle,
    int busLine);

/// <summary>
/// 네이티브 세션의 최신 health snapshot JSON을 반환합니다.
/// </summary>
/// <param name="sessionHandle">대상 세션 핸들입니다.</param>
/// <param name="healthJsonBuffer">health JSON을 받을 UTF-16 버퍼입니다.</param>
/// <param name="healthJsonCapacity">health JSON 버퍼 길이입니다.</param>
/// <param name="requiredHealthJsonCapacity">health JSON에 필요한 UTF-16 버퍼 길이입니다. 널 종료를 포함합니다.</param>
/// <returns>네이티브 호출 상태 코드입니다.</returns>
MILSTD1553_NATIVE_EXPORT int MilStd1553_GetHealthSnapshot(
    const wchar_t* sessionHandle,
    wchar_t* healthJsonBuffer,
    int healthJsonCapacity,
    int* requiredHealthJsonCapacity);

/// <summary>
/// 네이티브 세션의 pending telemetry 이벤트를 JSON 배열로 반환합니다.
/// </summary>
/// <param name="sessionHandle">대상 세션 핸들입니다.</param>
/// <param name="telemetryJsonBuffer">telemetry JSON 배열을 받을 UTF-16 버퍼입니다.</param>
/// <param name="telemetryJsonCapacity">telemetry JSON 버퍼 길이입니다.</param>
/// <param name="requiredTelemetryJsonCapacity">telemetry JSON에 필요한 UTF-16 버퍼 길이입니다. 널 종료를 포함합니다.</param>
/// <returns>네이티브 호출 상태 코드입니다.</returns>
MILSTD1553_NATIVE_EXPORT int MilStd1553_PollTelemetry(
    const wchar_t* sessionHandle,
    wchar_t* telemetryJsonBuffer,
    int telemetryJsonCapacity,
    int* requiredTelemetryJsonCapacity);
}
