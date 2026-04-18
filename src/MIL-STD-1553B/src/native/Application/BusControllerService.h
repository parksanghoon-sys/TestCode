#pragma once

#include "Application/AutomaticFailoverPolicy.h"
#include "Application/Contracts.h"

#include <optional>
#include <string_view>

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
        Domain::HealthSnapshot initialHealth,
        AutomaticFailoverPolicy failoverPolicy = AutomaticFailoverPolicy{});

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
    /// 지정한 메시지 설명으로 전송을 수행합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <param name="messageDescription">메시지 이벤트 설명입니다.</param>
    /// <param name="minimumTimeTag">성공 결과에 적용할 최소 time-tag입니다.</param>
    /// <returns>전송 결과입니다.</returns>
    [[nodiscard]] Domain::TransferResult ExecuteCore(
        const Domain::TransferRequest& request,
        std::string_view messageDescription,
        std::optional<std::chrono::microseconds> minimumTimeTag = std::nullopt);

    /// <summary>
    /// 전송 결과를 텔레메트리 이벤트로 변환합니다.
    /// </summary>
    /// <param name="request">원본 전송 요청입니다.</param>
    /// <param name="result">전송 결과입니다.</param>
    /// <param name="description">메시지 이벤트 설명입니다.</param>
    void PublishMessageEvent(
        const Domain::TransferRequest& request,
        const Domain::TransferResult& result,
        std::string_view description = {}) const;

    /// <summary>
    /// 메시지 프레임에 기록할 실제 데이터 워드를 선택합니다.
    /// </summary>
    /// <param name="request">원본 전송 요청입니다.</param>
    /// <param name="result">전송 결과입니다.</param>
    /// <returns>버스에 실린 데이터 워드 목록입니다.</returns>
    [[nodiscard]] static std::vector<Domain::DataWord> ResolveFrameDataWords(
        const Domain::TransferRequest& request,
        const Domain::TransferResult& result);

    /// <summary>
    /// 사용할 time-tag를 선택합니다.
    /// </summary>
    /// <param name="result">전송 결과입니다.</param>
    /// <returns>이벤트에 기록할 time-tag입니다.</returns>
    [[nodiscard]] std::chrono::microseconds ResolveTimeTag(
        const Domain::TransferResult& result) const;

    /// <summary>
    /// 자동 failover를 수행하고 한 번 더 재전송합니다.
    /// </summary>
    /// <param name="request">원본 전송 요청입니다.</param>
    /// <param name="errorCode">자동 failover를 유발한 오류 코드입니다.</param>
    /// <param name="messageDescription">성공 시 메시지 이벤트 설명입니다.</param>
    /// <returns>자동 failover 이후의 최종 전송 결과입니다.</returns>
    [[nodiscard]] Domain::TransferResult ExecuteAfterAutomaticFailover(
        const Domain::TransferRequest& request,
        Domain::ErrorCode errorCode,
        std::string_view messageDescription);

    /// <summary>
    /// 자동 failover 사유 설명을 생성합니다.
    /// </summary>
    /// <param name="errorCode">자동 failover를 유발한 오류 코드입니다.</param>
    /// <returns>이벤트 설명 문자열입니다.</returns>
    [[nodiscard]] static std::string BuildAutoFailoverDescription(Domain::ErrorCode errorCode);

    IBusAdapter& adapter_;
    const IClock& clock_;
    IBusEventSink& eventSink_;
    Domain::HealthSnapshot health_;
    AutomaticFailoverPolicy failoverPolicy_;
};
}
