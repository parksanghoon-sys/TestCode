#include "Domain/CommandWord.h"

namespace MilStd1553::Domain
{
namespace
{
constexpr std::uint16_t RtShift = 11;
constexpr std::uint16_t TrShift = 10;
constexpr std::uint16_t SubAddressShift = 5;
constexpr std::uint16_t FieldMask = 0x1FU;
}

CommandWord::CommandWord(
    const std::uint8_t terminalAddress,
    const bool transmit,
    const std::uint8_t subAddress,
    const std::uint8_t wordCountOrModeCode)
    : terminalAddress_(static_cast<std::uint8_t>(terminalAddress & FieldMask))
    , transmit_(transmit)
    , subAddress_(static_cast<std::uint8_t>(subAddress & FieldMask))
    , wordCountOrModeCode_(static_cast<std::uint8_t>(wordCountOrModeCode & FieldMask))
{
}

CommandWord CommandWord::FromRaw(const std::uint16_t rawWord)
{
    return CommandWord(
        static_cast<std::uint8_t>((rawWord >> RtShift) & FieldMask),
        ((rawWord >> TrShift) & 0x1U) != 0U,
        static_cast<std::uint8_t>((rawWord >> SubAddressShift) & FieldMask),
        static_cast<std::uint8_t>(rawWord & FieldMask));
}

CommandWord CommandWord::CreateModeCommand(
    const std::uint8_t terminalAddress,
    const bool transmit,
    const ModeCode modeCode)
{
    return CommandWord(
        terminalAddress,
        transmit,
        0U,
        static_cast<std::uint8_t>(modeCode));
}

std::uint16_t CommandWord::ToRaw() const
{
    return static_cast<std::uint16_t>(
        (static_cast<std::uint16_t>(terminalAddress_) << RtShift)
        | (static_cast<std::uint16_t>(transmit_ ? 1U : 0U) << TrShift)
        | (static_cast<std::uint16_t>(subAddress_) << SubAddressShift)
        | static_cast<std::uint16_t>(wordCountOrModeCode_));
}

std::uint8_t CommandWord::GetTerminalAddress() const noexcept
{
    return terminalAddress_;
}

bool CommandWord::IsTransmit() const noexcept
{
    return transmit_;
}

std::uint8_t CommandWord::GetSubAddress() const noexcept
{
    return subAddress_;
}

bool CommandWord::IsModeCode() const noexcept
{
    return subAddress_ == 0U || subAddress_ == 31U;
}

std::uint8_t CommandWord::GetDataWordCount() const noexcept
{
    if (IsModeCode())
    {
        return 0U;
    }

    return wordCountOrModeCode_ == 0U ? 32U : wordCountOrModeCode_;
}

std::uint8_t CommandWord::GetModeCode() const noexcept
{
    return wordCountOrModeCode_;
}

bool CommandWord::RequiresModeDataWord() const noexcept
{
    return IsModeCode() && wordCountOrModeCode_ >= 16U;
}

bool CommandWord::MatchesModeCode(const ModeCode modeCode) const noexcept
{
    return IsModeCode() && wordCountOrModeCode_ == static_cast<std::uint8_t>(modeCode);
}
}
