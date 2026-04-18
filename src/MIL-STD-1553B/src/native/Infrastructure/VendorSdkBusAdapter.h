#pragma once

#include "Application/Contracts.h"
#include "Infrastructure/VendorAdapterContracts.h"

namespace MilStd1553::Infrastructure
{
/// <summary>
/// 벤더 SDK 채널을 `Application::IBusAdapter`로 연결하는 bridge adapter입니다.
/// </summary>
class VendorSdkBusAdapter final : public Application::IBusAdapter
{
public:
    /// <summary>
    /// 벤더 SDK bridge adapter를 생성합니다.
    /// </summary>
    /// <param name="vendorChannel">실제 SDK channel 구현입니다.</param>
    /// <param name="errorMapper">벙더 status code 매퍼입니다.</param>
    /// <param name="configuration">초기 채널 설정입니다.</param>
    VendorSdkBusAdapter(
        IVendorChannel& vendorChannel,
        const IVendorErrorMapper& errorMapper,
        VendorChannelConfiguration configuration);

    /// <summary>
    /// channel을 명시적으로 엽니다.
    /// </summary>
    /// <returns>open 결과입니다.</returns>
    VendorOperationResult Open();

    /// <summary>
    /// channel을 명시적으로 닫습니다.
    /// </summary>
    /// <returns>close 결과입니다.</returns>
    VendorOperationResult Close();

    /// <summary>
    /// 현재 channel open 여부를 반환합니다.
    /// </summary>
    /// <returns>열려 있으면 true입니다.</returns>
    [[nodiscard]] bool IsOpen() const noexcept;

    /// <summary>
    /// cached capability를 반환합니다.
    /// </summary>
    /// <returns>capability 요약입니다.</returns>
    [[nodiscard]] const VendorAdapterCapabilities& GetCapabilities() const noexcept;

    /// <summary>
    /// 전송 요청을 수행합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <returns>도메인 전송 결과입니다.</returns>
    Domain::TransferResult Send(const Domain::TransferRequest& request) override;

    /// <summary>
    /// 활성 버스를 변경합니다.
    /// </summary>
    /// <param name="busLine">선택할 활성 버스입니다.</param>
    void SelectBus(Domain::BusLine busLine) override;

private:
    /// <summary>
    /// 전송 전에 channel open을 보장합니다.
    /// </summary>
    /// <returns>open 결과입니다.</returns>
    VendorOperationResult EnsureOpen();

    /// <summary>
    /// 벤더 전송 결과를 도메인 전송 결과로 변환합니다.
    /// </summary>
    /// <param name="vendorResult">벵더 전송 결과입니다.</param>
    /// <returns>도메인 전송 결과입니다.</returns>
    [[nodiscard]] Domain::TransferResult MapTransferResult(
        const VendorTransferResult& vendorResult) const;

    IVendorChannel& vendorChannel_;
    const IVendorErrorMapper& errorMapper_;
    VendorChannelConfiguration configuration_;
    VendorAdapterCapabilities capabilities_{};
    bool isOpen_{ false };
};
}
