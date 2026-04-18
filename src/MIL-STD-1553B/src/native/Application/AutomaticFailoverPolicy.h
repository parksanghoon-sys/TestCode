#pragma once

#include "Domain/TransferTypes.h"

namespace MilStd1553::Application
{
/// <summary>
/// 연속 timeout과 line fault를 기준으로 자동 failover 여부를 판단합니다.
/// </summary>
class AutomaticFailoverPolicy final
{
public:
    /// <summary>
    /// 자동 failover 정책을 생성합니다.
    /// </summary>
    /// <param name="timeoutThreshold">자동 failover를 발생시키는 연속 timeout 임계치입니다.</param>
    explicit AutomaticFailoverPolicy(const std::uint32_t timeoutThreshold = 2U) noexcept
        : timeoutThreshold_(timeoutThreshold)
    {
    }

    /// <summary>
    /// 현재 health 상태와 오류 코드를 기준으로 자동 failover 여부를 판단합니다.
    /// </summary>
    /// <param name="health">현재 health snapshot입니다.</param>
    /// <param name="errorCode">마지막 전송 오류 코드입니다.</param>
    /// <returns>자동 failover가 필요하면 true입니다.</returns>
    [[nodiscard]] bool ShouldFailover(
        const Domain::HealthSnapshot& health,
        const Domain::ErrorCode errorCode) const noexcept
    {
        if (errorCode == Domain::ErrorCode::LineFault)
        {
            return true;
        }

        return errorCode == Domain::ErrorCode::Timeout
            && health.consecutiveTimeoutCount >= timeoutThreshold_;
    }

private:
    std::uint32_t timeoutThreshold_{ 2U };
};
}
