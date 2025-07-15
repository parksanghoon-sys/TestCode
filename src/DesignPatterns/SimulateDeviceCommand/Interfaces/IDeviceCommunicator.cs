using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Interfaces;

// 장비 통신 인터페이스
public interface IDeviceCommunicator
{
    Task<DeviceResponse> SendAsync(byte[] data, TimeSpan timeout, CancellationToken cancellationToken);
    bool IsConnected { get; }
}
