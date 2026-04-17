#pragma once

#include "Domain/BusTypes.h"

#include <cstdint>

namespace MilStd1553::Domain
{
/// <summary>
/// MIL-STD-1553 커맨드 워드의 핵심 필드를 표현합니다.
/// </summary>
class CommandWord final
{
public:
    /// <summary>
    /// 필드 값으로 커맨드 워드를 생성합니다.
    /// </summary>
    /// <param name="terminalAddress">RT 주소입니다.</param>
    /// <param name="transmit">RT 송신 명령 여부입니다.</param>
    /// <param name="subAddress">서브어드레스 또는 mode 구분 값입니다.</param>
    /// <param name="wordCountOrModeCode">워드 수 또는 mode code입니다.</param>
    CommandWord(
        std::uint8_t terminalAddress,
        bool transmit,
        std::uint8_t subAddress,
        std::uint8_t wordCountOrModeCode);

    /// <summary>
    /// 원본 16비트 값에서 커맨드 워드를 파싱합니다.
    /// </summary>
    /// <param name="rawWord">원본 16비트 커맨드 워드입니다.</param>
    /// <returns>파싱된 커맨드 워드입니다.</returns>
    static CommandWord FromRaw(std::uint16_t rawWord);

    /// <summary>
    /// Mode Code 커맨드 워드를 생성합니다.
    /// </summary>
    /// <param name="terminalAddress">대상 RT 주소입니다.</param>
    /// <param name="transmit">Mode Code의 T/R 방향입니다.</param>
    /// <param name="modeCode">생성할 Mode Code입니다.</param>
    /// <returns>생성된 Mode Code 커맨드 워드입니다.</returns>
    static CommandWord CreateModeCommand(
        std::uint8_t terminalAddress,
        bool transmit,
        ModeCode modeCode);

    /// <summary>
    /// 현재 필드를 원본 16비트 값으로 직렬화합니다.
    /// </summary>
    /// <returns>직렬화된 16비트 커맨드 워드입니다.</returns>
    [[nodiscard]] std::uint16_t ToRaw() const;

    /// <summary>
    /// RT 주소를 반환합니다.
    /// </summary>
    /// <returns>RT 주소입니다.</returns>
    [[nodiscard]] std::uint8_t GetTerminalAddress() const noexcept;

    /// <summary>
    /// RT 송신 명령 여부를 반환합니다.
    /// </summary>
    /// <returns>RT 송신 명령이면 true입니다.</returns>
    [[nodiscard]] bool IsTransmit() const noexcept;

    /// <summary>
    /// 서브어드레스 또는 mode 구분 값을 반환합니다.
    /// </summary>
    /// <returns>서브어드레스 또는 mode 구분 값입니다.</returns>
    [[nodiscard]] std::uint8_t GetSubAddress() const noexcept;

    /// <summary>
    /// mode code 사용 여부를 반환합니다.
    /// </summary>
    /// <returns>mode code 경로이면 true입니다.</returns>
    [[nodiscard]] bool IsModeCode() const noexcept;

    /// <summary>
    /// 데이터 워드 수를 반환합니다.
    /// </summary>
    /// <returns>표준 규칙이 반영된 데이터 워드 수입니다.</returns>
    [[nodiscard]] std::uint8_t GetDataWordCount() const noexcept;

    /// <summary>
    /// mode code 값을 반환합니다.
    /// </summary>
    /// <returns>5비트 mode code 값입니다.</returns>
    [[nodiscard]] std::uint8_t GetModeCode() const noexcept;

    /// <summary>
    /// mode code가 단일 데이터 워드를 요구하는지 반환합니다.
    /// </summary>
    /// <returns>단일 데이터 워드가 필요하면 true입니다.</returns>
    [[nodiscard]] bool RequiresModeDataWord() const noexcept;

    /// <summary>
    /// 지정한 Mode Code와 일치하는지 반환합니다.
    /// </summary>
    /// <param name="modeCode">확인할 Mode Code입니다.</param>
    /// <returns>동일한 Mode Code이면 true입니다.</returns>
    [[nodiscard]] bool MatchesModeCode(ModeCode modeCode) const noexcept;

private:
    std::uint8_t terminalAddress_;
    bool transmit_;
    std::uint8_t subAddress_;
    std::uint8_t wordCountOrModeCode_;
};
}
