#pragma once

#include "Application/Contracts.h"

#include <vector>

namespace MilStd1553::Application
{
/// <summary>
/// BM 관점의 텔레메트리 이벤트를 메모리에 보관합니다.
/// </summary>
class BusMonitorService final : public IBusEventSink
{
public:
    /// <summary>
    /// BM 서비스를 생성합니다.
    /// </summary>
    /// <param name="persistentStore">선택적 영구 저장 sink입니다.</param>
    explicit BusMonitorService(IBusEventStore* persistentStore = nullptr);

    /// <summary>
    /// 텔레메트리 이벤트를 저장합니다.
    /// </summary>
    /// <param name="event">저장할 이벤트입니다.</param>
    void Publish(const Domain::TelemetryEvent& event) override;

    /// <summary>
    /// 저장된 이벤트 목록을 반환합니다.
    /// </summary>
    /// <returns>저장된 이벤트 목록입니다.</returns>
    [[nodiscard]] const std::vector<Domain::TelemetryEvent>& GetEvents() const noexcept;

private:
    IBusEventStore* persistentStore_{nullptr};
    std::vector<Domain::TelemetryEvent> events_;
};
}
