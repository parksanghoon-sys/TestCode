using SimulateDeviceCommand.Enums;
using SimulateDeviceCommand.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulateDeviceCommand.Interfaces;

// =============================================
// 핵심 인터페이스들 (Strategy, Command 패턴)
// =============================================

// Command Pattern - 명령 인터페이스
public interface IDeviceCommand
{
    string Name { get; }
    CommandState State { get; }
    int MaxRetries { get; }
    TimeSpan Timeout { get; }
    Func<DeviceResponse, bool> ResponseValidator { get; }  // 커맨드별 검증 함수

    Task<bool> ExecuteAsync(IDeviceCommunicator communicator, CancellationToken cancellationToken);
    void Reset();
    DeviceMessage GetMessage();  // 바이너리 메시지 반환으로 변경
}

// Strategy Pattern - 응답 검증 전략
public interface IResponseValidator
{
    bool IsValidResponse(DeviceResponse response, object expectedCriteria);
}

// Strategy Pattern - 재시도 전략
public interface IRetryStrategy
{
    Task DelayAsync(int retryCount, CancellationToken cancellationToken);
    bool ShouldRetry(int currentRetryCount, int maxRetries, DeviceResponse lastResponse);
}
