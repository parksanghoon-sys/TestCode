#pragma once

#include "Domain/TransferTypes.h"

namespace MilStd1553::Application
{
/// <summary>
/// 네이티브 버스 어댑터의 최소 계약입니다.
/// </summary>
class IBusAdapter
{
public:
    /// <summary>
    /// 가상 소멸자입니다.
    /// </summary>
    virtual ~IBusAdapter() = default;

    /// <summary>
    /// 전송 요청을 수행합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <returns>전송 결과입니다.</returns>
    virtual Domain::TransferResult Send(const Domain::TransferRequest& request) = 0;

    /// <summary>
    /// 활성 버스를 변경합니다.
    /// </summary>
    /// <param name="busLine">새로운 활성 버스입니다.</param>
    virtual void SelectBus(Domain::BusLine busLine) = 0;
};

/// <summary>
/// 텔레메트리 수집 대상의 최소 계약입니다.
/// </summary>
class IBusEventSink
{
public:
    /// <summary>
    /// 가상 소멸자입니다.
    /// </summary>
    virtual ~IBusEventSink() = default;

    /// <summary>
    /// 텔레메트리 이벤트를 기록합니다.
    /// </summary>
    /// <param name="event">기록할 이벤트입니다.</param>
    virtual void Publish(const Domain::TelemetryEvent& event) = 0;
};

/// <summary>
/// 텔레메트리 이벤트를 영구 저장하는 최소 계약입니다.
/// </summary>
class IBusEventStore
{
public:
    /// <summary>
    /// 가상 소멸자입니다.
    /// </summary>
    virtual ~IBusEventStore() = default;

    /// <summary>
    /// 텔레메트리 이벤트를 영구 저장 매체에 append합니다.
    /// </summary>
    /// <param name="event">기록할 이벤트입니다.</param>
    virtual void Append(const Domain::TelemetryEvent& event) = 0;
};

/// <summary>
/// time-tag 공급자의 최소 계약입니다.
/// </summary>
class IClock
{
public:
    /// <summary>
    /// 가상 소멸자입니다.
    /// </summary>
    virtual ~IClock() = default;

    /// <summary>
    /// 현재 time-tag를 반환합니다.
    /// </summary>
    /// <returns>현재 microseconds 단위 time-tag입니다.</returns>
    virtual std::chrono::microseconds GetCurrentTimeTag() const = 0;
};
}
