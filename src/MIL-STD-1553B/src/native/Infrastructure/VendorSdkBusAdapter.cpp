#include "Infrastructure/VendorSdkBusAdapter.h"

#include <utility>

namespace MilStd1553::Infrastructure
{
Domain::ErrorCode DefaultVendorErrorMapper::MapTransferError(
    const VendorSdkStatusCode statusCode) const noexcept
{
    switch (statusCode)
    {
    case VendorSdkStatusCode::Success:
        return Domain::ErrorCode::None;
    case VendorSdkStatusCode::Timeout:
        return Domain::ErrorCode::Timeout;
    case VendorSdkStatusCode::InvalidWord:
        return Domain::ErrorCode::InvalidWord;
    case VendorSdkStatusCode::UnsupportedModeCode:
        return Domain::ErrorCode::UnsupportedModeCode;
    case VendorSdkStatusCode::LineFault:
        return Domain::ErrorCode::LineFault;
    case VendorSdkStatusCode::ChannelNotOpen:
    case VendorSdkStatusCode::DeviceFailure:
    default:
        return Domain::ErrorCode::AdapterFailure;
    }
}

VendorSdkBusAdapter::VendorSdkBusAdapter(
    IVendorChannel& vendorChannel,
    const IVendorErrorMapper& errorMapper,
    VendorChannelConfiguration configuration)
    : vendorChannel_(vendorChannel)
    , errorMapper_(errorMapper)
    , configuration_(std::move(configuration))
    , capabilities_(vendorChannel.QueryCapabilities())
{
}

VendorOperationResult VendorSdkBusAdapter::Open()
{
    if (isOpen_)
    {
        return VendorOperationResult{};
    }

    auto result = vendorChannel_.Open(configuration_);
    isOpen_ = result.IsSuccess();
    return result;
}

VendorOperationResult VendorSdkBusAdapter::Close()
{
    if (!isOpen_)
    {
        return VendorOperationResult{};
    }

    auto result = vendorChannel_.Close();
    if (result.IsSuccess())
    {
        isOpen_ = false;
    }

    return result;
}

bool VendorSdkBusAdapter::IsOpen() const noexcept
{
    return isOpen_;
}

const VendorAdapterCapabilities& VendorSdkBusAdapter::GetCapabilities() const noexcept
{
    return capabilities_;
}

Domain::TransferResult VendorSdkBusAdapter::Send(const Domain::TransferRequest& request)
{
    const auto openResult = EnsureOpen();
    if (!openResult.IsSuccess())
    {
        return Domain::TransferResult{
            Domain::ErrorCode::AdapterFailure,
            std::nullopt,
            {},
            std::chrono::microseconds{ 0 },
        };
    }

    return MapTransferResult(vendorChannel_.SubmitTransfer(request));
}

void VendorSdkBusAdapter::SelectBus(const Domain::BusLine busLine)
{
    configuration_.initialBus = busLine;
    if (!isOpen_)
    {
        return;
    }

    const auto result = vendorChannel_.SelectBus(busLine);
    if (!result.IsSuccess())
    {
        isOpen_ = false;
    }
}

VendorOperationResult VendorSdkBusAdapter::EnsureOpen()
{
    if (isOpen_)
    {
        return VendorOperationResult{};
    }

    return Open();
}

Domain::TransferResult VendorSdkBusAdapter::MapTransferResult(
    const VendorTransferResult& vendorResult) const
{
    return Domain::TransferResult{
        errorMapper_.MapTransferError(vendorResult.statusCode),
        vendorResult.statusWord,
        vendorResult.dataWords,
        vendorResult.timeTag,
    };
}
}
