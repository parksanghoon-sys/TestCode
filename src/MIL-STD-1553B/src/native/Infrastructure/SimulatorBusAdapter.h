#pragma once

#include "Application/Contracts.h"

#include <chrono>
#include <unordered_map>

namespace MilStd1553::Infrastructure
{
/// <summary>
/// 시뮬레이터 기반으로 RT 송수신과 일부 Mode Code를 재현하는 어댑터입니다.
/// </summary>
class SimulatorBusAdapter final : public Application::IBusAdapter
{
public:
    /// <summary>
    /// 시뮬레이터 어댑터를 생성합니다.
    /// </summary>
    SimulatorBusAdapter();

    /// <summary>
    /// RT 송신용 데이터 버퍼를 설정합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <param name="subAddress">대상 서브어드레스입니다.</param>
    /// <param name="dataWords">설정할 데이터 워드 목록입니다.</param>
    void SetSubAddressData(
        std::uint8_t rtAddress,
        std::uint8_t subAddress,
        std::vector<Domain::DataWord> dataWords);

    /// <summary>
    /// RT 기본 상태 워드를 설정합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <param name="statusWord">설정할 상태 워드입니다.</param>
    void SetStatusWord(
        std::uint8_t rtAddress,
        const Domain::StatusWord& statusWord);

    /// <summary>
    /// RT BIT 워드를 설정합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <param name="bitWord">설정할 BIT 워드입니다.</param>
    void SetBitWord(
        std::uint8_t rtAddress,
        Domain::DataWord bitWord);

    /// <summary>
    /// 현재 저장된 RT 데이터 버퍼를 반환합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <param name="subAddress">대상 서브어드레스입니다.</param>
    /// <returns>저장된 데이터 워드 목록입니다.</returns>
    [[nodiscard]] std::vector<Domain::DataWord> GetSubAddressData(
        std::uint8_t rtAddress,
        std::uint8_t subAddress) const;

    /// <summary>
    /// 전송 요청을 수행합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <returns>시뮬레이터 결과입니다.</returns>
    Domain::TransferResult Send(const Domain::TransferRequest& request) override;

    /// <summary>
    /// 활성 버스를 변경합니다.
    /// </summary>
    /// <param name="busLine">새로운 활성 버스입니다.</param>
    void SelectBus(Domain::BusLine busLine) override;

private:
    /// <summary>
    /// 시뮬레이터 내부 RT 상태를 나타냅니다.
    /// </summary>
    struct RemoteTerminalState final
    {
        Domain::StatusWord statusWord{ 0, false, false, false, false, false, false, false };
        std::unordered_map<std::uint8_t, std::vector<Domain::DataWord>> subAddressData;
        Domain::DataWord bitWord{ 0 };
    };

    /// <summary>
    /// RT 상태를 보장하며 반환합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <returns>RT 상태입니다.</returns>
    RemoteTerminalState& EnsureTerminal(std::uint8_t rtAddress);

    /// <summary>
    /// RT 상태를 조회합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <returns>RT 상태 포인터입니다.</returns>
    [[nodiscard]] const RemoteTerminalState* FindTerminal(std::uint8_t rtAddress) const noexcept;

    /// <summary>
    /// 일반 transmit 커맨드를 처리합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <param name="terminal">대상 RT 상태입니다.</param>
    /// <returns>전송 결과입니다.</returns>
    [[nodiscard]] Domain::TransferResult HandleTransmitCommand(
        const Domain::TransferRequest& request,
        const RemoteTerminalState& terminal);

    /// <summary>
    /// 일반 receive 커맨드를 처리합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <param name="terminal">대상 RT 상태입니다.</param>
    /// <returns>전송 결과입니다.</returns>
    [[nodiscard]] Domain::TransferResult HandleReceiveCommand(
        const Domain::TransferRequest& request,
        RemoteTerminalState& terminal);

    /// <summary>
    /// 일부 Mode Code를 처리합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <param name="terminal">대상 RT 상태입니다.</param>
    /// <returns>전송 결과입니다.</returns>
    [[nodiscard]] Domain::TransferResult HandleModeCommand(
        const Domain::TransferRequest& request,
        const RemoteTerminalState& terminal);

    /// <summary>
    /// 다음 time-tag를 반환합니다.
    /// </summary>
    /// <returns>현재 요청에 사용할 time-tag입니다.</returns>
    [[nodiscard]] std::chrono::microseconds NextTimeTag() noexcept;

    std::unordered_map<std::uint8_t, RemoteTerminalState> terminals_;
    Domain::BusLine selectedBus_{ Domain::BusLine::A };
    std::chrono::microseconds nextTimeTag_{ 100 };
};
}
