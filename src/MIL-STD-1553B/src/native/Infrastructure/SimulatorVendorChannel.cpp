#include "Infrastructure/SimulatorVendorChannel.h"

namespace MilStd1553::Infrastructure
{
VendorAdapterCapabilities SimulatorVendorChannel::QueryCapabilities() const
{
    return capabilities_;
}

VendorOperationResult SimulatorVendorChannel::Open(const VendorChannelConfiguration& configuration)
{
    lastConfiguration_ = configuration;
    selectedBus_ = configuration.initialBus;
    simulator_.SelectBus(selectedBus_);
    isOpen_ = true;
    return VendorOperationResult{};
}

VendorOperationResult SimulatorVendorChannel::Close()
{
    isOpen_ = false;
    return VendorOperationResult{};
}

VendorTransferResult SimulatorVendorChannel::SubmitTransfer(const Domain::TransferRequest& request)
{
    if (!isOpen_)
    {
        return VendorTransferResult{
            VendorSdkStatusCode::ChannelNotOpen,
            std::nullopt,
            {},
            std::chrono::microseconds{ 0 },
            "channel is not open",
        };
    }

    auto normalizedRequest = request;
    normalizedRequest.busLine = selectedBus_;

    const auto result = simulator_.Send(normalizedRequest);
    return VendorTransferResult{
        MapErrorCode(result.errorCode),
        result.statusWord,
        result.dataWords,
        result.timeTag,
        {},
    };
}

VendorOperationResult SimulatorVendorChannel::SelectBus(const Domain::BusLine busLine)
{
    if (!isOpen_)
    {
        return VendorOperationResult{
            VendorSdkStatusCode::ChannelNotOpen,
            "channel is not open",
        };
    }

    selectedBus_ = busLine;
    simulator_.SelectBus(busLine);
    return VendorOperationResult{};
}

void SimulatorVendorChannel::SetSubAddressData(
    const std::uint8_t rtAddress,
    const std::uint8_t subAddress,
    std::vector<Domain::DataWord> dataWords)
{
    simulator_.SetSubAddressData(rtAddress, subAddress, std::move(dataWords));
}

void SimulatorVendorChannel::SetStatusWord(
    const std::uint8_t rtAddress,
    const Domain::StatusWord& statusWord)
{
    simulator_.SetStatusWord(rtAddress, statusWord);
}

void SimulatorVendorChannel::SetBitWord(
    const std::uint8_t rtAddress,
    const Domain::DataWord bitWord)
{
    simulator_.SetBitWord(rtAddress, bitWord);
}

void SimulatorVendorChannel::SetVectorWord(
    const std::uint8_t rtAddress,
    const Domain::DataWord vectorWord)
{
    simulator_.SetVectorWord(rtAddress, vectorWord);
}

void SimulatorVendorChannel::SetLineFault(
    const Domain::BusLine busLine,
    const bool enabled)
{
    simulator_.SetLineFault(busLine, enabled);
}

std::vector<Domain::DataWord> SimulatorVendorChannel::GetSubAddressData(
    const std::uint8_t rtAddress,
    const std::uint8_t subAddress) const
{
    return simulator_.GetSubAddressData(rtAddress, subAddress);
}

std::optional<Domain::DataWord> SimulatorVendorChannel::GetLastSynchronizationWord(
    const std::uint8_t rtAddress) const
{
    return simulator_.GetLastSynchronizationWord(rtAddress);
}

bool SimulatorVendorChannel::IsOpen() const noexcept
{
    return isOpen_;
}

Domain::BusLine SimulatorVendorChannel::GetSelectedBus() const noexcept
{
    return selectedBus_;
}

const std::optional<VendorChannelConfiguration>& SimulatorVendorChannel::GetLastConfiguration() const noexcept
{
    return lastConfiguration_;
}

VendorSdkStatusCode SimulatorVendorChannel::MapErrorCode(const Domain::ErrorCode errorCode) noexcept
{
    switch (errorCode)
    {
    case Domain::ErrorCode::None:
        return VendorSdkStatusCode::Success;
    case Domain::ErrorCode::Timeout:
        return VendorSdkStatusCode::Timeout;
    case Domain::ErrorCode::InvalidWord:
        return VendorSdkStatusCode::InvalidWord;
    case Domain::ErrorCode::UnsupportedModeCode:
        return VendorSdkStatusCode::UnsupportedModeCode;
    case Domain::ErrorCode::LineFault:
        return VendorSdkStatusCode::LineFault;
    case Domain::ErrorCode::AdapterFailure:
    default:
        return VendorSdkStatusCode::DeviceFailure;
    }
}
}
