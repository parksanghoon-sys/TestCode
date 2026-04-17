#pragma once

#include "Domain/BusTypes.h"
#include "Domain/CommandWord.h"
#include "Domain/StatusWord.h"

#include <chrono>
#include <cstdint>
#include <optional>
#include <string>
#include <vector>

namespace MilStd1553::Domain
{
/// <summary>
/// 16비트 데이터 워드를 표현합니다.
/// </summary>
struct DataWord final
{
    /// <summary>
    /// 원본 16비트 값을 보관합니다.
    /// </summary>
    std::uint16_t value{};
};

/// <summary>
/// 모니터링 가능한 메시지 프레임을 표현합니다.
/// </summary>
struct MessageFrame final
{
    /// <summary>
    /// 메시지를 시작한 커맨드 워드입니다.
    /// </summary>
    CommandWord commandWord;

    /// <summary>
    /// RT가 반환한 상태 워드입니다.
    /// </summary>
    std::optional<StatusWord> statusWord;

    /// <summary>
    /// 전송에 포함된 데이터 워드 목록입니다.
    /// </summary>
    std::vector<DataWord> dataWords;

    /// <summary>
    /// 메시지가 흐른 버스 라인입니다.
    /// </summary>
    BusLine busLine{BusLine::A};

    /// <summary>
    /// 프레임의 time-tag입니다.
    /// </summary>
    std::chrono::microseconds timeTag{0};
};

/// <summary>
/// 네이티브 어댑터로 전달하는 전송 요청입니다.
/// </summary>
struct TransferRequest final
{
    /// <summary>
    /// 송수신 의도를 표현하는 커맨드 워드입니다.
    /// </summary>
    CommandWord commandWord;

    /// <summary>
    /// 전송에 사용할 버스 라인입니다.
    /// </summary>
    BusLine busLine{BusLine::A};

    /// <summary>
    /// 송신 payload입니다.
    /// </summary>
    std::vector<DataWord> dataWords;
};

/// <summary>
/// 네이티브 어댑터가 반환하는 전송 결과입니다.
/// </summary>
struct TransferResult final
{
    /// <summary>
    /// 전송 결과 오류 코드입니다.
    /// </summary>
    ErrorCode errorCode{ErrorCode::None};

    /// <summary>
    /// RT 상태 워드입니다.
    /// </summary>
    std::optional<StatusWord> statusWord;

    /// <summary>
    /// 수신된 데이터 워드 목록입니다.
    /// </summary>
    std::vector<DataWord> dataWords;

    /// <summary>
    /// 어댑터가 제공한 time-tag입니다.
    /// </summary>
    std::chrono::microseconds timeTag{0};
};

/// <summary>
/// RT ↔ RT 전송 요청을 표현합니다.
/// </summary>
struct RtToRtTransferRequest final
{
    /// <summary>
    /// 목적지 RT의 receive 커맨드입니다.
    /// </summary>
    CommandWord destinationReceiveCommand;

    /// <summary>
    /// 소스 RT의 transmit 커맨드입니다.
    /// </summary>
    CommandWord sourceTransmitCommand;

    /// <summary>
    /// 전송에 사용할 버스 라인입니다.
    /// </summary>
    BusLine busLine{BusLine::A};
};

/// <summary>
/// RT ↔ RT 전송 결과를 표현합니다.
/// </summary>
struct RtToRtTransferResult final
{
    /// <summary>
    /// 소스 RT transmit 결과입니다.
    /// </summary>
    TransferResult sourceTransfer;

    /// <summary>
    /// 목적지 RT receive 결과입니다.
    /// </summary>
    std::optional<TransferResult> destinationTransfer;
};

/// <summary>
/// 버스 건강 상태를 표현합니다.
/// </summary>
struct HealthSnapshot final
{
    /// <summary>
    /// 현재 활성 버스입니다.
    /// </summary>
    BusLine activeBus{BusLine::A};

    /// <summary>
    /// 대기 버스입니다.
    /// </summary>
    BusLine standbyBus{BusLine::B};

    /// <summary>
    /// 누적 timeout 수입니다.
    /// </summary>
    std::uint32_t timeoutCount{0};

    /// <summary>
    /// 누적 retry 수입니다.
    /// </summary>
    std::uint32_t retryCount{0};

    /// <summary>
    /// degraded 상태 여부입니다.
    /// </summary>
    bool degraded{false};

    /// <summary>
    /// 기본 health snapshot을 생성합니다.
    /// </summary>
    /// <returns>기본 버스 상태입니다.</returns>
    static HealthSnapshot CreateDefault() noexcept
    {
        return HealthSnapshot{};
    }

    /// <summary>
    /// timeout 발생을 반영합니다.
    /// </summary>
    void RecordTimeout() noexcept
    {
        ++timeoutCount;
        degraded = true;
    }

    /// <summary>
    /// retry 수행을 반영합니다.
    /// </summary>
    void RecordRetry() noexcept
    {
        ++retryCount;
    }

    /// <summary>
    /// 활성 버스를 전환합니다.
    /// </summary>
    /// <param name="nextBus">새로운 활성 버스입니다.</param>
    void SwitchActiveBus(const BusLine nextBus) noexcept
    {
        if (activeBus == nextBus)
        {
            return;
        }

        standbyBus = activeBus;
        activeBus = nextBus;
    }
};

/// <summary>
/// Host와 BM이 공통으로 소비하는 텔레메트리 이벤트입니다.
/// </summary>
struct TelemetryEvent final
{
    /// <summary>
    /// 이벤트의 유형입니다.
    /// </summary>
    TelemetryEventType type{TelemetryEventType::MessageFrame};

    /// <summary>
    /// 이벤트가 발생한 시각입니다.
    /// </summary>
    std::chrono::microseconds timeTag{0};

    /// <summary>
    /// 이벤트 당시 활성 버스입니다.
    /// </summary>
    BusLine activeBus{BusLine::A};

    /// <summary>
    /// 사람이 읽을 수 있는 설명입니다.
    /// </summary>
    std::string description;

    /// <summary>
    /// 메시지 이벤트일 때의 프레임입니다.
    /// </summary>
    std::optional<MessageFrame> messageFrame;

    /// <summary>
    /// 메시지 프레임 이벤트를 생성합니다.
    /// </summary>
    /// <param name="frame">기록할 프레임입니다.</param>
    /// <returns>생성된 메시지 이벤트입니다.</returns>
    static TelemetryEvent CreateMessageEvent(const MessageFrame& frame)
    {
        TelemetryEvent event;
        event.type = TelemetryEventType::MessageFrame;
        event.timeTag = frame.timeTag;
        event.activeBus = frame.busLine;
        event.description = "메시지 프레임";
        event.messageFrame = frame;
        return event;
    }

    /// <summary>
    /// 수동 버스 전환 이벤트를 생성합니다.
    /// </summary>
    /// <param name="nextBus">전환된 활성 버스입니다.</param>
    /// <param name="timeTag">이벤트 시각입니다.</param>
    /// <returns>생성된 버스 전환 이벤트입니다.</returns>
    static TelemetryEvent CreateBusSwitchEvent(
        const BusLine nextBus,
        const std::chrono::microseconds timeTag)
    {
        TelemetryEvent event;
        event.type = TelemetryEventType::BusSwitch;
        event.timeTag = timeTag;
        event.activeBus = nextBus;
        event.description = nextBus == BusLine::A ? "활성 버스를 A로 전환" : "활성 버스를 B로 전환";
        return event;
    }
};
}
