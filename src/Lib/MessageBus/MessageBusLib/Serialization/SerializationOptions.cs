using System.Text.Json;

namespace MessageBusLib.Serialization;

/// <summary>
/// 직렬화 옵션
/// </summary>
public class SerializationOptions
{
    /// <summary>
    /// JSON 직렬화 옵션
    /// </summary>
    public JsonSerializerOptions JsonOptions { get; set; }

    public SerializationOptions()
    {
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };
    }
}
