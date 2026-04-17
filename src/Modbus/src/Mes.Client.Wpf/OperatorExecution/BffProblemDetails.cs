using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// Experience API가 반환하는 problem details payload를 표현합니다.
/// </summary>
public sealed class BffProblemDetails
{
    /// <summary>
    /// problem type URI를 가져오거나 설정합니다.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// problem 제목을 가져오거나 설정합니다.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// HTTP 상태 코드를 가져오거나 설정합니다.
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// problem 상세 메시지를 가져오거나 설정합니다.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// 표준 필드 외 확장 데이터를 가져오거나 설정합니다.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; set; }

    /// <summary>
    /// 확장 데이터에 포함된 canonical error code를 반환합니다.
    /// </summary>
    /// <returns>error code가 있으면 문자열을, 없으면 <see langword="null"/>을 반환합니다.</returns>
    public string? GetErrorCode()
    {
        return GetExtensionString("errorCode");
    }

    /// <summary>
    /// 지정한 확장 필드를 문자열로 읽어옵니다.
    /// </summary>
    /// <param name="name">읽을 확장 필드 이름입니다.</param>
    /// <returns>값이 있으면 문자열을, 없으면 <see langword="null"/>을 반환합니다.</returns>
    public string? GetExtensionString(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Extensions is null || !Extensions.TryGetValue(name, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}
