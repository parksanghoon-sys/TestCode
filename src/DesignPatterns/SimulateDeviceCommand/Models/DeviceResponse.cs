using SimulateDeviceCommand.Enums;

namespace SimulateDeviceCommand.Models;

public class DeviceResponse
{
    public ResponseType Type { get; set; }
    public string Message { get; set; }
    public byte[] RawData { get; set; }
    public DeviceMessage? ParsedMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
