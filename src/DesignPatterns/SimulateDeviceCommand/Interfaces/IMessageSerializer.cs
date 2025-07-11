using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Interfaces;

// Strategy Pattern - 메시지 직렬화 전략
public interface IMessageSerializer
{
    byte[] Serialize<T>(T message);
    T Deserialize<T>(byte[] data);
}
// 바이너리 메시지 직렬화 인터페이스
public interface IBinaryMessageSerializer
{
    byte[] Serialize(DeviceMessage message);
    DeviceMessage Deserialize(byte[] data);
    bool ValidateMessage(byte[] data);
}
