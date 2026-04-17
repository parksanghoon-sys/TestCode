#include "Infrastructure/SimulatorBusAdapter.h"

namespace MilStd1553::Infrastructure
{
namespace
{
Domain::StatusWord NormalizeStatusWord(
    const std::uint8_t rtAddress,
    const Domain::StatusWord& statusWord)
{
    return Domain::StatusWord(
        rtAddress,
        statusWord.HasMessageError(),
        statusWord.HasServiceRequest(),
        statusWord.HasBroadcastCommandReceived(),
        statusWord.IsBusy(),
        statusWord.HasSubsystemFlag(),
        statusWord.HasDynamicBusControlAccepted(),
        statusWord.HasTerminalFlag());
}
}

SimulatorBusAdapter::SimulatorBusAdapter() = default;

void SimulatorBusAdapter::SetSubAddressData(
    const std::uint8_t rtAddress,
    const std::uint8_t subAddress,
    std::vector<Domain::DataWord> dataWords)
{
    auto& terminal = EnsureTerminal(rtAddress);
    terminal.subAddressData[subAddress] = std::move(dataWords);
}

void SimulatorBusAdapter::SetStatusWord(
    const std::uint8_t rtAddress,
    const Domain::StatusWord& statusWord)
{
    auto& terminal = EnsureTerminal(rtAddress);
    terminal.statusWord = NormalizeStatusWord(rtAddress, statusWord);
}

void SimulatorBusAdapter::SetBitWord(
    const std::uint8_t rtAddress,
    const Domain::DataWord bitWord)
{
    auto& terminal = EnsureTerminal(rtAddress);
    terminal.bitWord = bitWord;
}

std::vector<Domain::DataWord> SimulatorBusAdapter::GetSubAddressData(
    const std::uint8_t rtAddress,
    const std::uint8_t subAddress) const
{
    const auto* terminal = FindTerminal(rtAddress);
    if (terminal == nullptr)
    {
        return {};
    }

    const auto iterator = terminal->subAddressData.find(subAddress);
    if (iterator == terminal->subAddressData.end())
    {
        return {};
    }

    return iterator->second;
}

Domain::TransferResult SimulatorBusAdapter::Send(const Domain::TransferRequest& request)
{
    auto& terminal = EnsureTerminal(request.commandWord.GetTerminalAddress());
    if (request.commandWord.IsModeCode())
    {
        return HandleModeCommand(request, terminal);
    }

    if (request.commandWord.IsTransmit())
    {
        return HandleTransmitCommand(request, terminal);
    }

    return HandleReceiveCommand(request, terminal);
}

void SimulatorBusAdapter::SelectBus(const Domain::BusLine busLine)
{
    selectedBus_ = busLine;
}

SimulatorBusAdapter::RemoteTerminalState& SimulatorBusAdapter::EnsureTerminal(
    const std::uint8_t rtAddress)
{
    const auto [iterator, inserted] = terminals_.try_emplace(rtAddress);
    if (inserted)
    {
        iterator->second.statusWord = Domain::StatusWord(
            rtAddress,
            false,
            false,
            false,
            false,
            false,
            false,
            false);
    }

    return iterator->second;
}

const SimulatorBusAdapter::RemoteTerminalState* SimulatorBusAdapter::FindTerminal(
    const std::uint8_t rtAddress) const noexcept
{
    const auto iterator = terminals_.find(rtAddress);
    if (iterator == terminals_.end())
    {
        return nullptr;
    }

    return &iterator->second;
}

Domain::TransferResult SimulatorBusAdapter::HandleTransmitCommand(
    const Domain::TransferRequest& request,
    const RemoteTerminalState& terminal)
{
    const auto iterator = terminal.subAddressData.find(request.commandWord.GetSubAddress());
    const auto expectedCount = request.commandWord.GetDataWordCount();
    if (iterator == terminal.subAddressData.end()
        || iterator->second.size() < static_cast<std::size_t>(expectedCount))
    {
        return Domain::TransferResult{
            Domain::ErrorCode::InvalidWord,
            terminal.statusWord,
            {},
            NextTimeTag(),
        };
    }

    return Domain::TransferResult{
        Domain::ErrorCode::None,
        terminal.statusWord,
        std::vector<Domain::DataWord>(
            iterator->second.begin(),
            iterator->second.begin() + expectedCount),
        NextTimeTag(),
    };
}

Domain::TransferResult SimulatorBusAdapter::HandleReceiveCommand(
    const Domain::TransferRequest& request,
    RemoteTerminalState& terminal)
{
    const auto expectedCount = request.commandWord.GetDataWordCount();
    if (request.dataWords.size() != static_cast<std::size_t>(expectedCount))
    {
        return Domain::TransferResult{
            Domain::ErrorCode::InvalidWord,
            terminal.statusWord,
            {},
            NextTimeTag(),
        };
    }

    terminal.subAddressData[request.commandWord.GetSubAddress()] = request.dataWords;
    return Domain::TransferResult{
        Domain::ErrorCode::None,
        terminal.statusWord,
        {},
        NextTimeTag(),
    };
}

Domain::TransferResult SimulatorBusAdapter::HandleModeCommand(
    const Domain::TransferRequest& request,
    const RemoteTerminalState& terminal)
{
    if (request.commandWord.MatchesModeCode(Domain::ModeCode::TransmitStatusWord)
        && request.commandWord.IsTransmit())
    {
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            terminal.statusWord,
            {},
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::TransmitBitWord)
        && request.commandWord.IsTransmit())
    {
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            terminal.statusWord,
            { terminal.bitWord },
            NextTimeTag(),
        };
    }

    return Domain::TransferResult{
        Domain::ErrorCode::UnsupportedModeCode,
        terminal.statusWord,
        {},
        NextTimeTag(),
    };
}

std::chrono::microseconds SimulatorBusAdapter::NextTimeTag() noexcept
{
    const auto timeTag = nextTimeTag_;
    nextTimeTag_ += std::chrono::microseconds{ 25 };
    return timeTag;
}
}
