#pragma once

#include <cstdint>

namespace MilStd1553::Domain
{
/// <summary>
/// 1553 이중화 버스 라인을 나타냅니다.
/// </summary>
enum class BusLine : std::uint8_t
{
    A = 0,
    B = 1,
};

/// <summary>
/// 전송 결과의 오류 유형을 나타냅니다.
/// </summary>
enum class ErrorCode : std::uint8_t
{
    None = 0,
    Timeout = 1,
    InvalidWord = 2,
    UnsupportedModeCode = 3,
    LineFault = 4,
    AdapterFailure = 5,
};

/// <summary>
/// 현재 저장소에서 지원하거나 우선순위를 정의한 Mode Code를 나타냅니다.
/// </summary>
enum class ModeCode : std::uint8_t
{
    DynamicBusControl = 0x00,
    Synchronize = 0x01,
    TransmitStatusWord = 0x02,
    InhibitTerminalFlag = 0x06,
    OverrideInhibitTerminalFlag = 0x07,
    ResetRemoteTerminal = 0x08,
    TransmitVectorWord = 0x10,
    SynchronizeWithDataWord = 0x11,
    TransmitLastCommandWord = 0x12,
    TransmitBitWord = 0x13,
};

/// <summary>
/// 모니터링 이벤트의 종류를 나타냅니다.
/// </summary>
enum class TelemetryEventType : std::uint8_t
{
    MessageFrame = 0,
    BusSwitch = 1,
    AutoFailover = 2,
};
}
