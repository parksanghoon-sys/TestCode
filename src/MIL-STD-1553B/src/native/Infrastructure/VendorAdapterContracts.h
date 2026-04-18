#pragma once

#include "Domain/TransferTypes.h"

#include <chrono>
#include <cstdint>
#include <optional>
#include <string>

namespace MilStd1553::Infrastructure
{
/// <summary>
/// 벤더 SDK channel open 시 필요한 공통 설정입니다.
/// </summary>
struct VendorChannelConfiguration final
{
    /// <summary>
    /// 대상 채널 식별자입니다.
    /// </summary>
    int channelId{ 0 };

    /// <summary>
    /// 초기 활성 버스입니다.
    /// </summary>
    Domain::BusLine initialBus{ Domain::BusLine::A };

    /// <summary>
    /// BC 기능 사용 여부입니다.
    /// </summary>
    bool enableBusController{ true };

    /// <summary>
    /// RT 기능 사용 여부입니다.
    /// </summary>
    bool enableRemoteTerminal{ false };

    /// <summary>
    /// BM 기능 사용 여부입니다.
    /// </summary>
    bool enableBusMonitor{ false };

    /// <summary>
    /// RT 역할을 함께 여는 경우의 RT 주소입니다.
    /// </summary>
    std::optional<std::uint8_t> remoteTerminalAddress;

    /// <summary>
    /// 기본 응답 timeout입니다.
    /// </summary>
    std::chrono::microseconds responseTimeout{ 1000 };
};

/// <summary>
/// 벤더 SDK status code의 공통 의미를 나타냅니다.
/// </summary>
enum class VendorSdkStatusCode : int
{
    Success = 0,
    Timeout = 1001,
    InvalidWord = 1002,
    UnsupportedModeCode = 1003,
    LineFault = 1004,
    ChannelNotOpen = 2001,
    DeviceFailure = 2002,
};

/// <summary>
/// 벤더 어댑터 capability 요약입니다.
/// </summary>
struct VendorAdapterCapabilities final
{
    /// <summary>
    /// BC 지원 여부입니다.
    /// </summary>
    bool supportsBusController{ true };

    /// <summary>
    /// RT 지원 여부입니다.
    /// </summary>
    bool supportsRemoteTerminal{ false };

    /// <summary>
    /// BM 지원 여부입니다.
    /// </summary>
    bool supportsBusMonitor{ false };

    /// <summary>
    /// 하드웨어 assist 기반 자동 failover 지원 여부입니다.
    /// </summary>
    bool supportsAutomaticFailoverAssist{ false };

    /// <summary>
    /// 하드웨어 time-tag 지원 여부입니다.
    /// </summary>
    bool supportsTimeTag{ true };

    /// <summary>
    /// line fault 감지 여부입니다.
    /// </summary>
    bool supportsLineFaultDetection{ false };
};

/// <summary>
/// open/close/select bus 같은 채널 제어 결과입니다.
/// </summary>
struct VendorOperationResult final
{
    /// <summary>
    /// 벤더 SDK 상태 코드입니다.
    /// </summary>
    VendorSdkStatusCode statusCode{ VendorSdkStatusCode::Success };

    /// <summary>
    /// 진단 메시지입니다.
    /// </summary>
    std::string diagnostic;

    /// <summary>
    /// 성공 여부를 반환합니다.
    /// </summary>
    /// <returns>성공이면 true입니다.</returns>
    [[nodiscard]] bool IsSuccess() const noexcept
    {
        return statusCode == VendorSdkStatusCode::Success;
    }
};

/// <summary>
/// 벤더 SDK 전송 결과입니다.
/// </summary>
struct VendorTransferResult final
{
    /// <summary>
    /// 벤더 SDK 상태 코드입니다.
    /// </summary>
    VendorSdkStatusCode statusCode{ VendorSdkStatusCode::Success };

    /// <summary>
    /// 수신 상태 워드입니다.
    /// </summary>
    std::optional<Domain::StatusWord> statusWord;

    /// <summary>
    /// 수신 데이터 워드 목록입니다.
    /// </summary>
    std::vector<Domain::DataWord> dataWords;

    /// <summary>
    /// 벤더 SDK 또는 어댑터가 제공한 time-tag입니다.
    /// </summary>
    std::chrono::microseconds timeTag{ 0 };

    /// <summary>
    /// 진단 메시지입니다.
    /// </summary>
    std::string diagnostic;

    /// <summary>
    /// 성공 여부를 반환합니다.
    /// </summary>
    /// <returns>성공이면 true입니다.</returns>
    [[nodiscard]] bool IsSuccess() const noexcept
    {
        return statusCode == VendorSdkStatusCode::Success;
    }
};

/// <summary>
/// 실제 벤더 SDK 채널을 감싼 가장 바깥 계약입니다.
/// </summary>
class IVendorChannel
{
public:
    /// <summary>
    /// 가상 소멸자입니다.
    /// </summary>
    virtual ~IVendorChannel() = default;

    /// <summary>
    /// channel capability를 조회합니다.
    /// </summary>
    /// <returns>capability 요약입니다.</returns>
    [[nodiscard]] virtual VendorAdapterCapabilities QueryCapabilities() const = 0;

    /// <summary>
    /// 채널을 엽니다.
    /// </summary>
    /// <param name="configuration">적용할 채널 설정입니다.</param>
    /// <returns>채널 제어 결과입니다.</returns>
    virtual VendorOperationResult Open(const VendorChannelConfiguration& configuration) = 0;

    /// <summary>
    /// 채널을 닫습니다.
    /// </summary>
    /// <returns>채널 제어 결과입니다.</returns>
    virtual VendorOperationResult Close() = 0;

    /// <summary>
    /// 전송 요청을 벤더 SDK에 전달합니다.
    /// </summary>
    /// <param name="request">전송 요청입니다.</param>
    /// <returns>벤더 전송 결과입니다.</returns>
    virtual VendorTransferResult SubmitTransfer(const Domain::TransferRequest& request) = 0;

    /// <summary>
    /// 활성 버스를 변경합니다.
    /// </summary>
    /// <param name="busLine">선택할 활성 버스입니다.</param>
    /// <returns>채널 제어 결과입니다.</returns>
    virtual VendorOperationResult SelectBus(Domain::BusLine busLine) = 0;
};

/// <summary>
/// 벤더 status code를 도메인 오류 코드로 변환하는 계약입니다.
/// </summary>
class IVendorErrorMapper
{
public:
    /// <summary>
    /// 가상 소멸자입니다.
    /// </summary>
    virtual ~IVendorErrorMapper() = default;

    /// <summary>
    /// 벤더 status code를 도메인 오류 코드로 변환합니다.
    /// </summary>
    /// <param name="statusCode">변환할 벤더 status code입니다.</param>
    /// <returns>도메인 오류 코드입니다.</returns>
    [[nodiscard]] virtual Domain::ErrorCode MapTransferError(
        VendorSdkStatusCode statusCode) const noexcept = 0;
};

/// <summary>
/// 기본 벤더 오류 매핑 구현입니다.
/// </summary>
class DefaultVendorErrorMapper final : public IVendorErrorMapper
{
public:
    /// <summary>
    /// 벤더 status code를 기본 도메인 오류 코드로 변환합니다.
    /// </summary>
    /// <param name="statusCode">변환할 벤더 status code입니다.</param>
    /// <returns>도메인 오류 코드입니다.</returns>
    [[nodiscard]] Domain::ErrorCode MapTransferError(
        VendorSdkStatusCode statusCode) const noexcept override;
};
}
