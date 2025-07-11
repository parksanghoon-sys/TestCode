using SimulateDeviceCommand.Interfaces;
using System.Text.Json;

namespace SimulateDeviceCommand.Services;

// JSON 메시지 직렬화
public class JsonMessageSerializer : IMessageSerializer
{
    public byte[] Serialize<T>(T message)
    {
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    public T Deserialize<T>(byte[] data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json);
    }
}
