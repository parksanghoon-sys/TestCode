namespace MessageBusLib.Messages;

/// <summary>
/// 메시지 헤더 (추가 메타데이터 저장)
/// </summary>
[Serializable]
public class MessageHeaders
{
    /// <summary>
    /// 헤더 값 딕셔너리
    /// </summary>
    public Dictionary<string, string> Values { get; private set; }

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public MessageHeaders()
    {
        Values = new Dictionary<string, string>();
    }

    /// <summary>
    /// 헤더 값 설정
    /// </summary>
    public void SetHeader(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        Values[key] = value;
    }

    /// <summary>
    /// 헤더 값 가져오기
    /// </summary>
    public string GetHeader(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        return Values.TryGetValue(key, out string value) ? value : null;
    }

    /// <summary>
    /// 헤더 값 존재 여부 확인
    /// </summary>
    public bool ContainsHeader(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        return Values.ContainsKey(key);
    }

    /// <summary>
    /// 헤더 값 제거
    /// </summary>
    public void RemoveHeader(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        Values.Remove(key);
    }

    /// <summary>
    /// 헤더 값 개수
    /// </summary>
    public int Count => Values.Count;
}
