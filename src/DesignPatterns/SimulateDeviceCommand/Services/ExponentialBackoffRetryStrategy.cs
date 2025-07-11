using SimulateDeviceCommand.Enums;
using SimulateDeviceCommand.Interfaces;
using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Services;

public class ExponentialBackoffRetryStrategy : IRetryStrategy
{
    private readonly TimeSpan _baseDelay;
    public ExponentialBackoffRetryStrategy(TimeSpan baseDealy)
    {
        _baseDelay = baseDealy;
    }

    public async Task DelayAsync(int retryCount, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, retryCount));
        await Task.Delay(delay, cancellationToken);
    }

    public bool ShouldRetry(int currentRetryCount, int maxRetries, DeviceResponse lastResponse)
    {
        return currentRetryCount < maxRetries &&
              (lastResponse.Type == ResponseType.Fail || lastResponse.Type == ResponseType.Timeout);
    }
}