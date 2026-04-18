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

std::size_t GetBusIndex(const Domain::BusLine busLine)
{
    return busLine == Domain::BusLine::A ? 0U : 1U;
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

void SimulatorBusAdapter::SetVectorWord(
    const std::uint8_t rtAddress,
    const Domain::DataWord vectorWord)
{
    auto& terminal = EnsureTerminal(rtAddress);
    terminal.vectorWord = vectorWord;
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

std::optional<Domain::DataWord> SimulatorBusAdapter::GetLastSynchronizationWord(
    const std::uint8_t rtAddress) const
{
    const auto* terminal = FindTerminal(rtAddress);
    if (terminal == nullptr)
    {
        return std::nullopt;
    }

    return terminal->lastSynchronizationWord;
}

void SimulatorBusAdapter::SetLineFault(
    const Domain::BusLine busLine,
    const bool enabled)
{
    lineFaults_[GetBusIndex(busLine)] = enabled;
}

Domain::TransferResult SimulatorBusAdapter::Send(const Domain::TransferRequest& request)
{
    auto& terminal = EnsureTerminal(request.commandWord.GetTerminalAddress());
    if (lineFaults_[GetBusIndex(request.busLine)])
    {
        return Domain::TransferResult{
            Domain::ErrorCode::LineFault,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    Domain::TransferResult result;
    if (request.commandWord.IsModeCode())
    {
        result = HandleModeCommand(request, terminal);
    }
    else if (request.commandWord.IsTransmit())
    {
        result = HandleTransmitCommand(request, terminal);
    }
    else
    {
        result = HandleReceiveCommand(request, terminal);
    }

    if (result.errorCode == Domain::ErrorCode::None)
    {
        RecordAcceptedCommand(terminal, request.commandWord);
    }

    return result;
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
        ResetTerminal(rtAddress, iterator->second);
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
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    return Domain::TransferResult{
        Domain::ErrorCode::None,
        BuildEffectiveStatusWord(terminal),
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
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    terminal.subAddressData[request.commandWord.GetSubAddress()] = request.dataWords;
    return Domain::TransferResult{
        Domain::ErrorCode::None,
        BuildEffectiveStatusWord(terminal),
        {},
        NextTimeTag(),
    };
}

Domain::TransferResult SimulatorBusAdapter::HandleModeCommand(
    const Domain::TransferRequest& request,
    RemoteTerminalState& terminal)
{
    if (request.commandWord.MatchesModeCode(Domain::ModeCode::TransmitStatusWord)
        && request.commandWord.IsTransmit())
    {
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::TransmitBitWord)
        && request.commandWord.IsTransmit())
    {
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            { terminal.bitWord },
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::Synchronize)
        && !request.commandWord.IsTransmit())
    {
        if (!request.dataWords.empty())
        {
            return Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                BuildEffectiveStatusWord(terminal),
                {},
                NextTimeTag(),
            };
        }

        terminal.lastSynchronizationWord.reset();
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::SynchronizeWithDataWord)
        && !request.commandWord.IsTransmit())
    {
        if (request.dataWords.size() != 1U)
        {
            return Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                BuildEffectiveStatusWord(terminal),
                {},
                NextTimeTag(),
            };
        }

        terminal.lastSynchronizationWord = request.dataWords.front();
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::TransmitVectorWord)
        && request.commandWord.IsTransmit())
    {
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            { terminal.vectorWord },
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::TransmitLastCommandWord)
        && request.commandWord.IsTransmit())
    {
        if (!terminal.lastAcceptedCommandWordRaw.has_value())
        {
            return Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                BuildEffectiveStatusWord(terminal),
                {},
                NextTimeTag(),
            };
        }

        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            { Domain::DataWord{ *terminal.lastAcceptedCommandWordRaw } },
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::InhibitTerminalFlag)
        && !request.commandWord.IsTransmit())
    {
        if (!request.dataWords.empty())
        {
            return Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                BuildEffectiveStatusWord(terminal),
                {},
                NextTimeTag(),
            };
        }

        terminal.terminalFlagInhibited = true;
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::OverrideInhibitTerminalFlag)
        && !request.commandWord.IsTransmit())
    {
        if (!request.dataWords.empty())
        {
            return Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                BuildEffectiveStatusWord(terminal),
                {},
                NextTimeTag(),
            };
        }

        terminal.terminalFlagInhibited = false;
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    if (request.commandWord.MatchesModeCode(Domain::ModeCode::ResetRemoteTerminal)
        && !request.commandWord.IsTransmit())
    {
        if (!request.dataWords.empty())
        {
            return Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                BuildEffectiveStatusWord(terminal),
                {},
                NextTimeTag(),
            };
        }

        ResetTerminal(request.commandWord.GetTerminalAddress(), terminal);
        return Domain::TransferResult{
            Domain::ErrorCode::None,
            BuildEffectiveStatusWord(terminal),
            {},
            NextTimeTag(),
        };
    }

    return Domain::TransferResult{
        Domain::ErrorCode::UnsupportedModeCode,
        BuildEffectiveStatusWord(terminal),
        {},
        NextTimeTag(),
    };
}

Domain::StatusWord SimulatorBusAdapter::BuildEffectiveStatusWord(
    const RemoteTerminalState& terminal)
{
    return Domain::StatusWord(
        terminal.statusWord.GetTerminalAddress(),
        terminal.statusWord.HasMessageError(),
        terminal.statusWord.HasServiceRequest(),
        terminal.statusWord.HasBroadcastCommandReceived(),
        terminal.statusWord.IsBusy(),
        terminal.statusWord.HasSubsystemFlag(),
        terminal.statusWord.HasDynamicBusControlAccepted(),
        terminal.statusWord.HasTerminalFlag() && !terminal.terminalFlagInhibited);
}

void SimulatorBusAdapter::RecordAcceptedCommand(
    RemoteTerminalState& terminal,
    const Domain::CommandWord& commandWord)
{
    terminal.lastAcceptedCommandWordRaw = commandWord.ToRaw();
}

void SimulatorBusAdapter::ResetTerminal(
    const std::uint8_t rtAddress,
    RemoteTerminalState& terminal)
{
    terminal.statusWord = Domain::StatusWord(
        rtAddress,
        false,
        false,
        false,
        false,
        false,
        false,
        false);
    terminal.subAddressData.clear();
    terminal.bitWord = Domain::DataWord{ 0 };
    terminal.vectorWord = Domain::DataWord{ 0 };
    terminal.lastSynchronizationWord.reset();
    terminal.lastAcceptedCommandWordRaw.reset();
    terminal.terminalFlagInhibited = false;
}

std::chrono::microseconds SimulatorBusAdapter::NextTimeTag() noexcept
{
    const auto timeTag = nextTimeTag_;
    nextTimeTag_ += std::chrono::microseconds{ 25 };
    return timeTag;
}
}
