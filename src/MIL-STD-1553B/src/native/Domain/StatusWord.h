#pragma once

#include <cstdint>

namespace MilStd1553::Domain
{
/// <summary>
/// MIL-STD-1553 상태 워드의 핵심 비트를 표현합니다.
/// </summary>
class StatusWord final
{
public:
    /// <summary>
    /// 필드 값으로 상태 워드를 생성합니다.
    /// </summary>
    /// <param name="terminalAddress">상태를 송신한 RT 주소입니다.</param>
    /// <param name="messageError">메시지 오류 비트입니다.</param>
    /// <param name="serviceRequest">서비스 요청 비트입니다.</param>
    /// <param name="broadcastCommandReceived">broadcast 수신 비트입니다.</param>
    /// <param name="busy">busy 비트입니다.</param>
    /// <param name="subsystemFlag">subsystem flag 비트입니다.</param>
    /// <param name="dynamicBusControlAccepted">dynamic bus control acceptance 비트입니다.</param>
    /// <param name="terminalFlag">terminal flag 비트입니다.</param>
    StatusWord(
        std::uint8_t terminalAddress,
        bool messageError,
        bool serviceRequest,
        bool broadcastCommandReceived,
        bool busy,
        bool subsystemFlag,
        bool dynamicBusControlAccepted,
        bool terminalFlag);

    /// <summary>
    /// 원본 16비트 값에서 상태 워드를 파싱합니다.
    /// </summary>
    /// <param name="rawWord">원본 16비트 상태 워드입니다.</param>
    /// <returns>파싱된 상태 워드입니다.</returns>
    static StatusWord FromRaw(std::uint16_t rawWord);

    /// <summary>
    /// 현재 필드를 원본 16비트 상태 값으로 직렬화합니다.
    /// </summary>
    /// <returns>직렬화된 16비트 상태 워드입니다.</returns>
    [[nodiscard]] std::uint16_t ToRaw() const;

    /// <summary>
    /// RT 주소를 반환합니다.
    /// </summary>
    /// <returns>RT 주소입니다.</returns>
    [[nodiscard]] std::uint8_t GetTerminalAddress() const noexcept;

    /// <summary>
    /// 메시지 오류 비트를 반환합니다.
    /// </summary>
    /// <returns>메시지 오류이면 true입니다.</returns>
    [[nodiscard]] bool HasMessageError() const noexcept;

    /// <summary>
    /// 서비스 요청 비트를 반환합니다.
    /// </summary>
    /// <returns>서비스 요청이 있으면 true입니다.</returns>
    [[nodiscard]] bool HasServiceRequest() const noexcept;

    /// <summary>
    /// broadcast 수신 비트를 반환합니다.
    /// </summary>
    /// <returns>broadcast 명령을 수신했으면 true입니다.</returns>
    [[nodiscard]] bool HasBroadcastCommandReceived() const noexcept;

    /// <summary>
    /// busy 비트를 반환합니다.
    /// </summary>
    /// <returns>busy 상태이면 true입니다.</returns>
    [[nodiscard]] bool IsBusy() const noexcept;

    /// <summary>
    /// subsystem flag 비트를 반환합니다.
    /// </summary>
    /// <returns>subsystem fault가 있으면 true입니다.</returns>
    [[nodiscard]] bool HasSubsystemFlag() const noexcept;

    /// <summary>
    /// dynamic bus control acceptance 비트를 반환합니다.
    /// </summary>
    /// <returns>bus control acceptance이면 true입니다.</returns>
    [[nodiscard]] bool HasDynamicBusControlAccepted() const noexcept;

    /// <summary>
    /// terminal flag 비트를 반환합니다.
    /// </summary>
    /// <returns>terminal fault가 있으면 true입니다.</returns>
    [[nodiscard]] bool HasTerminalFlag() const noexcept;

private:
    std::uint8_t terminalAddress_;
    bool messageError_;
    bool serviceRequest_;
    bool broadcastCommandReceived_;
    bool busy_;
    bool subsystemFlag_;
    bool dynamicBusControlAccepted_;
    bool terminalFlag_;
};
}
