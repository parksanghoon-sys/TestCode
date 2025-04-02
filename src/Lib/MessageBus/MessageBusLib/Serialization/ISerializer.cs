namespace MessageBusLib.Serialization;

/// <summary>
/// 직렬화 인터페이스
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// 객체를 바이트 배열로 직렬화
    /// </summary>
    byte[] Serialize<T>(T obj);

    /// <summary>
    /// 바이트 배열을 객체로 역직렬화
    /// </summary>
    T Deserialize<T>(byte[] data);

    /// <summary>
    /// 타입 정보를 포함하여 객체를 바이트 배열로 직렬화
    /// </summary>
    byte[] SerializeWithType(object obj);

    /// <summary>
    /// 타입 정보가 포함된 바이트 배열을 원래 타입의 객체로 역직렬화
    /// </summary>
    object DeserializeWithType(byte[] data);
}
