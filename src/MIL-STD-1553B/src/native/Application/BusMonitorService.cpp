#include "Application/BusMonitorService.h"

namespace MilStd1553::Application
{
BusMonitorService::BusMonitorService(IBusEventStore* persistentStore)
    : persistentStore_(persistentStore)
{
}

void BusMonitorService::Publish(const Domain::TelemetryEvent& event)
{
    events_.push_back(event);

    if (persistentStore_ != nullptr)
    {
        persistentStore_->Append(event);
    }
}

const std::vector<Domain::TelemetryEvent>& BusMonitorService::GetEvents() const noexcept
{
    return events_;
}
}
