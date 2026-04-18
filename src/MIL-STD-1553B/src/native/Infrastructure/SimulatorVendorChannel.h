#pragma once

#include "Infrastructure/SimulatorBusAdapter.h"
#include "Infrastructure/VendorAdapterContracts.h"

namespace MilStd1553::Infrastructure
{
/// <summary>
/// `SimulatorBusAdapter`를 `IVendorChannel` 계약으로 감싸는 concrete channel입니다.
/// </summary>
class SimulatorVendorChannel final : public IVendorChannel
{
public:
    /// <summary>
    /// 시뮬레이터 기반 vendor channel을 생성합니다.
    /// </summary>
    SimulatorVendorChannel() = default;

    /// <summary>
    /// 시뮬레이터 channel capability를 반환합니다.
    /// </summary>
    /// <returns>시뮬레이터 capability 요약입니다.</returns>
    [[nodiscard]] VendorAdapterCapabilities QueryCapabilities() const override;

    /// <summary>
    /// 시뮬레이터 channel을 엽니다.
    /// </summary>
    /// <param name="configuration">적용할 channel 설정입니다.</param>
    /// <returns>channel open 결과입니다.</returns>
    VendorOperationResult Open(const VendorChannelConfiguration& configuration) override;

    /// <summary>
    /// 시뮬레이터 channel을 닫습니다.
    /// </summary>
    /// <returns>channel close 결과입니다.</returns>
    VendorOperationResult Close() override;

    /// <summary>
    /// 현재 selected bus를 기준으로 전송 요청을 수행합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <returns>vendor 전송 결과입니다.</returns>
    VendorTransferResult SubmitTransfer(const Domain::TransferRequest& request) override;

    /// <summary>
    /// 시뮬레이터 channel의 selected bus를 변경합니다.
    /// </summary>
    /// <param name="busLine">새로운 활성 bus입니다.</param>
    /// <returns>channel 제어 결과입니다.</returns>
    VendorOperationResult SelectBus(Domain::BusLine busLine) override;

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
    /// RT vector word를 설정합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <param name="vectorWord">설정할 vector word입니다.</param>
    void SetVectorWord(
        std::uint8_t rtAddress,
        Domain::DataWord vectorWord);

    /// <summary>
    /// 지정한 bus 라인에 line fault를 설정합니다.
    /// </summary>
    /// <param name="busLine">설정할 bus 라인입니다.</param>
    /// <param name="enabled">활성화 여부입니다.</param>
    void SetLineFault(
        Domain::BusLine busLine,
        bool enabled);

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
    /// 마지막 synchronize data word를 반환합니다.
    /// </summary>
    /// <param name="rtAddress">대상 RT 주소입니다.</param>
    /// <returns>마지막 synchronize data word입니다.</returns>
    [[nodiscard]] std::optional<Domain::DataWord> GetLastSynchronizationWord(
        std::uint8_t rtAddress) const;

    /// <summary>
    /// 현재 channel open 여부를 반환합니다.
    /// </summary>
    /// <returns>열려 있으면 true입니다.</returns>
    [[nodiscard]] bool IsOpen() const noexcept;

    /// <summary>
    /// 현재 selected bus를 반환합니다.
    /// </summary>
    /// <returns>현재 selected bus입니다.</returns>
    [[nodiscard]] Domain::BusLine GetSelectedBus() const noexcept;

    /// <summary>
    /// 마지막 open configuration을 반환합니다.
    /// </summary>
    /// <returns>마지막 open configuration입니다.</returns>
    [[nodiscard]] const std::optional<VendorChannelConfiguration>& GetLastConfiguration() const noexcept;

private:
    /// <summary>
    /// 도메인 오류 코드를 vendor status code로 변환합니다.
    /// </summary>
    /// <param name="errorCode">변환할 도메인 오류 코드입니다.</param>
    /// <returns>vendor status code입니다.</returns>
    [[nodiscard]] static VendorSdkStatusCode MapErrorCode(Domain::ErrorCode errorCode) noexcept;

    SimulatorBusAdapter simulator_;
    VendorAdapterCapabilities capabilities_{
        true,
        true,
        true,
        false,
        true,
        true,
    };
    bool isOpen_{ false };
    Domain::BusLine selectedBus_{ Domain::BusLine::A };
    std::optional<VendorChannelConfiguration> lastConfiguration_;
};
}
