#pragma once

#include "Application/Contracts.h"

namespace MilStd1553::Application
{
/// <summary>
/// BC 관점의 전송과 health 상태 갱신을 담당합니다.
/// </summary>
class BusControllerService final
{
public:
    /// <summary>
    /// BC 서비스를 생성합니다.
    /// </summary>
    /// <param name="adapter">송수신 어댑터입니다.</param>
    /// <param name="clock">time-tag 공급자입니다.</param>
    /// <param name="eventSink">텔레메트리 기록 대상입니다.</param>
    /// <param name="initialHealth">초기 health 상태입니다.</param>
    BusControllerService(
        IBusAdapter& adapter,
        const IClock& clock,
        IBusEventSink& eventSink,
        Domain::HealthSnapshot initialHealth);

    /// <summary>
    /// 단일 전송을 수행하고 필요 시 한 번 재시도합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <returns>최종 전송 결과입니다.</returns>
    Domain::TransferResult Execute(const Domain::TransferRequest& request);

    /// <summary>
    /// RT ↔ RT 전송을 오케스트레이션합니다.
    /// </summary>
    /// <param name="request">RT ↔ RT 전송 요청입니다.</param>
    /// <returns>소스와 목적지 결과가 포함된 전송 결과입니다.</returns>
    Domain::RtToRtTransferResult ExecuteRtToRtTransfer(
        const Domain::RtToRtTransferRequest& request);

    /// <summary>
    /// 활성 버스를 수동으로 전환합니다.
    /// </summary>
    /// <param name="nextBus">전환할 버스입니다.</param>
    void SwitchBus(Domain::BusLine nextBus);

    /// <summary>
    /// 현재 health 상태를 반환합니다.
    /// </summary>
    /// <returns>현재 health 상태입니다.</returns>
    [[nodiscard]] const Domain::HealthSnapshot& GetHealthSnapshot() const noexcept;

private:
    /// <summary>
    /// 전송 결과를 텔레메트리 이벤트로 변환합니다.
    /// </summary>
    /// <param name="request">원본 전송 요청입니다.</param>
    /// <param name="result">전송 결과입니다.</param>
    void PublishMessageEvent(
        const Domain::TransferRequest& request,
        const Domain::TransferResult& result) const;

    /// <summary>
    /// 사용할 time-tag를 선택합니다.
    /// </summary>
    /// <param name="result">전송 결과입니다.</param>
    /// <returns>이벤트에 기록할 time-tag입니다.</returns>
    [[nodiscard]] std::chrono::microseconds ResolveTimeTag(
        const Domain::TransferResult& result) const;

    IBusAdapter& adapter_;
    const IClock& clock_;
    IBusEventSink& eventSink_;
    Domain::HealthSnapshot health_;
};
}
