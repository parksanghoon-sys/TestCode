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
};

/// <summary>
/// 현재 MVP에서 다루는 대표 Mode Code를 나타냅니다.
/// </summary>
enum class ModeCode : std::uint8_t
{
    TransmitStatusWord = 0x02,
    TransmitBitWord = 0x13,
};

/// <summary>
/// 모니터링 이벤트의 종류를 나타냅니다.
/// </summary>
enum class TelemetryEventType : std::uint8_t
{
    MessageFrame = 0,
    BusSwitch = 1,
};
}
