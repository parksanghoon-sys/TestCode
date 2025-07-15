using SimulateDeviceCommand.Interfaces;
using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Services;

public class BinaryMessageSerializer : IBinaryMessageSerializer
{
    public byte[] Serialize(DeviceMessage message)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(message.Cmd);
            writer.Write(message.Length);
            writer.Write(message.Data);
            writer.Write(message.Checksum);
            return stream.ToArray();
        }
    }

    public DeviceMessage Deserialize(byte[] data)
    {
        if (data.Length < 4) // 최소 크기: cmd(1) + length(1) + checksum(2)
            throw new ArgumentException("Invalid message length");

        using (var stream = new MemoryStream(data))
        using (var reader = new BinaryReader(stream))
        {
            var cmd = reader.ReadByte();
            var length = reader.ReadByte();

            var messageData = new byte[length];
            if (length > 0)
            {
                messageData = reader.ReadBytes(length);
            }

            var checksum = reader.ReadInt16();

            return new DeviceMessage
            {
                Cmd = cmd,
                Length = length,
                Data = messageData,
                Checksum = checksum
            };
        }
    }

    public bool ValidateMessage(byte[] data)
    {
        try
        {
            var message = Deserialize(data);
            return message.IsChecksumValid();
        }
        catch
        {
            return false;
        }
    }
}
