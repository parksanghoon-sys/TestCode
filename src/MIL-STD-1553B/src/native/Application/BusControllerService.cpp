#include "Application/BusControllerService.h"

namespace MilStd1553::Application
{
BusControllerService::BusControllerService(
    IBusAdapter& adapter,
    const IClock& clock,
    IBusEventSink& eventSink,
    Domain::HealthSnapshot initialHealth)
    : adapter_(adapter)
    , clock_(clock)
    , eventSink_(eventSink)
    , health_(initialHealth)
{
}

Domain::TransferResult BusControllerService::Execute(const Domain::TransferRequest& request)
{
    Domain::TransferRequest effectiveRequest = request;
    effectiveRequest.busLine = health_.activeBus;

    Domain::TransferResult result = adapter_.Send(effectiveRequest);
    if (result.errorCode == Domain::ErrorCode::Timeout)
    {
        health_.RecordTimeout();
        health_.RecordRetry();
        result = adapter_.Send(effectiveRequest);
    }

    if (result.errorCode == Domain::ErrorCode::None)
    {
        PublishMessageEvent(effectiveRequest, result);
    }

    return result;
}

Domain::RtToRtTransferResult BusControllerService::ExecuteRtToRtTransfer(
    const Domain::RtToRtTransferRequest& request)
{
    const Domain::TransferRequest sourceRequest{
        request.sourceTransmitCommand,
        request.busLine,
        {},
    };

    Domain::RtToRtTransferResult result{
        Execute(sourceRequest),
        std::nullopt,
    };

    if (result.sourceTransfer.errorCode != Domain::ErrorCode::None)
    {
        return result;
    }

    const Domain::TransferRequest destinationRequest{
        request.destinationReceiveCommand,
        request.busLine,
        result.sourceTransfer.dataWords,
    };

    result.destinationTransfer = Execute(destinationRequest);
    return result;
}

void BusControllerService::SwitchBus(const Domain::BusLine nextBus)
{
    health_.SwitchActiveBus(nextBus);
    adapter_.SelectBus(nextBus);
    eventSink_.Publish(Domain::TelemetryEvent::CreateBusSwitchEvent(nextBus, clock_.GetCurrentTimeTag()));
}

const Domain::HealthSnapshot& BusControllerService::GetHealthSnapshot() const noexcept
{
    return health_;
}

void BusControllerService::PublishMessageEvent(
    const Domain::TransferRequest& request,
    const Domain::TransferResult& result) const
{
    Domain::MessageFrame frame{
        request.commandWord,
        result.statusWord,
        result.dataWords,
        request.busLine,
        ResolveTimeTag(result),
    };

    eventSink_.Publish(Domain::TelemetryEvent::CreateMessageEvent(frame));
}

std::chrono::microseconds BusControllerService::ResolveTimeTag(
    const Domain::TransferResult& result) const
{
    if (result.timeTag.count() > 0)
    {
        return result.timeTag;
    }

    return clock_.GetCurrentTimeTag();
}
}
