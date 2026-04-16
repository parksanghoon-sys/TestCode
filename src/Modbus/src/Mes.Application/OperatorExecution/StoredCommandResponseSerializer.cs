using System.Text.Json;

namespace Mes.Application.OperatorExecution;

/// <summary>
/// command receipt 재생에 사용할 응답 payload 직렬화기를 제공합니다.
/// </summary>
public sealed class StoredCommandResponseSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 응답 계약을 receipt 저장용 JSON 문자열로 직렬화합니다.
    /// </summary>
    /// <typeparam name="TResponse">응답 계약 형식입니다.</typeparam>
    /// <param name="response">직렬화할 응답입니다.</param>
    /// <returns>저장 가능한 JSON 문자열입니다.</returns>
    public string Serialize<TResponse>(TResponse response)
    {
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    /// <summary>
    /// receipt 에 저장된 JSON 문자열을 응답 계약으로 역직렬화합니다.
    /// </summary>
    /// <typeparam name="TResponse">역직렬화할 응답 계약 형식입니다.</typeparam>
    /// <param name="responseJson">저장된 JSON 문자열입니다.</param>
    /// <returns>역직렬화된 응답 계약입니다.</returns>
    public TResponse Deserialize<TResponse>(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            throw new InvalidOperationException("Stored command response JSON cannot be empty.");
        }

        var response = JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
        return response ?? throw new InvalidOperationException("Stored command response JSON could not be deserialized.");
    }
}
