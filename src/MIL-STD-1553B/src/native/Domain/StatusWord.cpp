#include "Domain/StatusWord.h"

namespace MilStd1553::Domain
{
namespace
{
constexpr std::uint16_t RtShift = 11;
constexpr std::uint16_t MessageErrorShift = 10;
constexpr std::uint16_t ServiceRequestShift = 8;
constexpr std::uint16_t BroadcastReceivedShift = 4;
constexpr std::uint16_t BusyShift = 3;
constexpr std::uint16_t SubsystemFlagShift = 2;
constexpr std::uint16_t DynamicBusControlShift = 1;
constexpr std::uint16_t TerminalFlagShift = 0;
constexpr std::uint16_t FieldMask = 0x1FU;
}

StatusWord::StatusWord(
    const std::uint8_t terminalAddress,
    const bool messageError,
    const bool serviceRequest,
    const bool broadcastCommandReceived,
    const bool busy,
    const bool subsystemFlag,
    const bool dynamicBusControlAccepted,
    const bool terminalFlag)
    : terminalAddress_(static_cast<std::uint8_t>(terminalAddress & FieldMask))
    , messageError_(messageError)
    , serviceRequest_(serviceRequest)
    , broadcastCommandReceived_(broadcastCommandReceived)
    , busy_(busy)
    , subsystemFlag_(subsystemFlag)
    , dynamicBusControlAccepted_(dynamicBusControlAccepted)
    , terminalFlag_(terminalFlag)
{
}

StatusWord StatusWord::FromRaw(const std::uint16_t rawWord)
{
    return StatusWord(
        static_cast<std::uint8_t>((rawWord >> RtShift) & FieldMask),
        ((rawWord >> MessageErrorShift) & 0x1U) != 0U,
        ((rawWord >> ServiceRequestShift) & 0x1U) != 0U,
        ((rawWord >> BroadcastReceivedShift) & 0x1U) != 0U,
        ((rawWord >> BusyShift) & 0x1U) != 0U,
        ((rawWord >> SubsystemFlagShift) & 0x1U) != 0U,
        ((rawWord >> DynamicBusControlShift) & 0x1U) != 0U,
        ((rawWord >> TerminalFlagShift) & 0x1U) != 0U);
}

std::uint16_t StatusWord::ToRaw() const
{
    return static_cast<std::uint16_t>(
        (static_cast<std::uint16_t>(terminalAddress_) << RtShift)
        | (static_cast<std::uint16_t>(messageError_ ? 1U : 0U) << MessageErrorShift)
        | (static_cast<std::uint16_t>(serviceRequest_ ? 1U : 0U) << ServiceRequestShift)
        | (static_cast<std::uint16_t>(broadcastCommandReceived_ ? 1U : 0U) << BroadcastReceivedShift)
        | (static_cast<std::uint16_t>(busy_ ? 1U : 0U) << BusyShift)
        | (static_cast<std::uint16_t>(subsystemFlag_ ? 1U : 0U) << SubsystemFlagShift)
        | (static_cast<std::uint16_t>(dynamicBusControlAccepted_ ? 1U : 0U) << DynamicBusControlShift)
        | (static_cast<std::uint16_t>(terminalFlag_ ? 1U : 0U) << TerminalFlagShift));
}

std::uint8_t StatusWord::GetTerminalAddress() const noexcept
{
    return terminalAddress_;
}

bool StatusWord::HasMessageError() const noexcept
{
    return messageError_;
}

bool StatusWord::HasServiceRequest() const noexcept
{
    return serviceRequest_;
}

bool StatusWord::HasBroadcastCommandReceived() const noexcept
{
    return broadcastCommandReceived_;
}

bool StatusWord::IsBusy() const noexcept
{
    return busy_;
}

bool StatusWord::HasSubsystemFlag() const noexcept
{
    return subsystemFlag_;
}

bool StatusWord::HasDynamicBusControlAccepted() const noexcept
{
    return dynamicBusControlAccepted_;
}

bool StatusWord::HasTerminalFlag() const noexcept
{
    return terminalFlag_;
}
}
