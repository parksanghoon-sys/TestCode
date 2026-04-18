#include "Application/BusControllerService.h"

#include <utility>

namespace MilStd1553::Application
{
namespace
{
constexpr auto kRtToRtMinimumDestinationGap = std::chrono::microseconds{ 40 };
}

BusControllerService::BusControllerService(
    IBusAdapter& adapter,
    const IClock& clock,
    IBusEventSink& eventSink,
    Domain::HealthSnapshot initialHealth,
    AutomaticFailoverPolicy failoverPolicy)
    : adapter_(adapter)
    , clock_(clock)
    , eventSink_(eventSink)
    , health_(initialHealth)
    , failoverPolicy_(failoverPolicy)
{
}

Domain::TransferResult BusControllerService::Execute(const Domain::TransferRequest& request)
{
    return ExecuteCore(request, {});
}

Domain::TransferResult BusControllerService::ExecuteCore(
    const Domain::TransferRequest& request,
    const std::string_view messageDescription,
    const std::optional<std::chrono::microseconds> minimumTimeTag)
{
    Domain::TransferRequest effectiveRequest = request;
    effectiveRequest.busLine = health_.activeBus;

    const auto finalizeSuccess = [this, &effectiveRequest, messageDescription, minimumTimeTag](
                                     Domain::TransferResult successfulResult)
    {
        successfulResult.timeTag = ResolveTimeTag(successfulResult);
        if (minimumTimeTag.has_value() && successfulResult.timeTag < minimumTimeTag.value())
        {
            successfulResult.timeTag = minimumTimeTag.value();
        }

        health_.RecordSuccessfulTransfer();
        PublishMessageEvent(effectiveRequest, successfulResult, messageDescription);
        return successfulResult;
    };

    Domain::TransferResult result = adapter_.Send(effectiveRequest);
    if (result.errorCode == Domain::ErrorCode::None)
    {
        return finalizeSuccess(std::move(result));
    }

    if (result.errorCode == Domain::ErrorCode::Timeout)
    {
        health_.RecordTimeout();
        health_.RecordRetry();
        result = adapter_.Send(effectiveRequest);
        if (result.errorCode == Domain::ErrorCode::Timeout)
        {
            health_.RecordTimeout();
        }
    }
    else if (result.errorCode == Domain::ErrorCode::LineFault)
    {
        health_.MarkDegraded();
    }

    if (result.errorCode == Domain::ErrorCode::None)
    {
        return finalizeSuccess(std::move(result));
    }

    if (failoverPolicy_.ShouldFailover(health_, result.errorCode))
    {
        return ExecuteAfterAutomaticFailover(request, result.errorCode, messageDescription);
    }

    return result;
}

Domain::RtToRtTransferResult BusControllerService::ExecuteRtToRtTransfer(
    const Domain::RtToRtTransferRequest& request)
{
    if (!request.sourceTransmitCommand.IsTransmit()
        || request.destinationReceiveCommand.IsTransmit()
        || request.sourceTransmitCommand.IsModeCode()
        || request.destinationReceiveCommand.IsModeCode()
        || request.sourceTransmitCommand.GetDataWordCount()
            != request.destinationReceiveCommand.GetDataWordCount())
    {
        return Domain::RtToRtTransferResult{
            Domain::TransferResult{
                Domain::ErrorCode::InvalidWord,
                std::nullopt,
                {},
                std::chrono::microseconds{ 0 } },
            std::nullopt,
        };
    }

    const Domain::TransferRequest sourceRequest{
        request.sourceTransmitCommand,
        request.busLine,
        {},
    };

    Domain::RtToRtTransferResult result{
        ExecuteCore(sourceRequest, "RT↔RT 소스 RT transmit 단계"),
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

    result.destinationTransfer = ExecuteCore(
        destinationRequest,
        "RT↔RT 목적지 RT receive 단계",
        result.sourceTransfer.timeTag + kRtToRtMinimumDestinationGap);
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
    const Domain::TransferResult& result,
    const std::string_view description) const
{
    Domain::MessageFrame frame{
        request.commandWord,
        result.statusWord,
        ResolveFrameDataWords(request, result),
        request.busLine,
        ResolveTimeTag(result),
    };

    if (description.empty())
    {
        eventSink_.Publish(Domain::TelemetryEvent::CreateMessageEvent(frame));
        return;
    }

    eventSink_.Publish(Domain::TelemetryEvent::CreateMessageEvent(frame, std::string(description)));
}

std::vector<Domain::DataWord> BusControllerService::ResolveFrameDataWords(
    const Domain::TransferRequest& request,
    const Domain::TransferResult& result)
{
    if (request.commandWord.IsTransmit())
    {
        return result.dataWords;
    }

    return request.dataWords;
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

Domain::TransferResult BusControllerService::ExecuteAfterAutomaticFailover(
    const Domain::TransferRequest& request,
    const Domain::ErrorCode errorCode,
    const std::string_view messageDescription)
{
    const auto nextBus = health_.standbyBus;
    health_.RecordAutomaticFailover(nextBus);
    adapter_.SelectBus(nextBus);
    eventSink_.Publish(Domain::TelemetryEvent::CreateAutoFailoverEvent(
        nextBus,
        BuildAutoFailoverDescription(errorCode),
        clock_.GetCurrentTimeTag()));

    Domain::TransferRequest failoverRequest = request;
    failoverRequest.busLine = health_.activeBus;

    auto result = adapter_.Send(failoverRequest);
    if (result.errorCode == Domain::ErrorCode::Timeout)
    {
        health_.RecordTimeout();
    }
    else if (result.errorCode == Domain::ErrorCode::LineFault)
    {
        health_.MarkDegraded();
    }
    else if (result.errorCode == Domain::ErrorCode::None)
    {
        health_.RecordSuccessfulTransfer();
        PublishMessageEvent(failoverRequest, result, messageDescription);
    }

    return result;
}

std::string BusControllerService::BuildAutoFailoverDescription(const Domain::ErrorCode errorCode)
{
    switch (errorCode)
    {
    case Domain::ErrorCode::LineFault:
        return "line fault로 standby bus로 자동 전환";
    case Domain::ErrorCode::Timeout:
        return "연속 timeout으로 standby bus로 자동 전환";
    default:
        return "오류로 standby bus로 자동 전환";
    }
}
}
