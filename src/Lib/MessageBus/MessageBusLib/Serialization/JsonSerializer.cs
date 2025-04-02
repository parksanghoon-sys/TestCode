using System.Text;

namespace MessageBusLib.Serialization;

/// <summary>
/// JSON 기반 직렬화 구현
/// </summary>
public class JsonSerializer : ISerializer
{
    private readonly SerializationOptions _options;

    public JsonSerializer(SerializationOptions options = null)
    {
        _options = options ?? new SerializationOptions();
    }

    /// <summary>
    /// 객체를 JSON으로 직렬화
    /// </summary>
    public byte[] Serialize<T>(T obj)
    {
        if (obj == null) return null;

        // 문자열인 경우 UTF-8 인코딩 사용
        if (obj is string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        // 이미 바이트 배열인 경우 그대로 반환
        if (obj is byte[] bytes)
        {
            return bytes;
        }

        // 그 외 객체는 JSON 직렬화
        string json = System.Text.Json.JsonSerializer.Serialize(obj, _options.JsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// JSON 데이터를 객체로 역직렬화
    /// </summary>
    public T Deserialize<T>(byte[] data)
    {
        if (data == null || data.Length == 0) return default;

        // 문자열로 역직렬화하는 경우
        if (typeof(T) == typeof(string))
        {
            return (T)(object)Encoding.UTF8.GetString(data);
        }

        // 바이트 배열로 역직렬화하는 경우
        if (typeof(T) == typeof(byte[]))
        {
            return (T)(object)data;
        }

        // 그 외 객체는 JSON 역직렬화
        string json = Encoding.UTF8.GetString(data);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options.JsonOptions);
    }

    /// <summary>
    /// 타입 정보를 포함하여 객체를 직렬화
    /// </summary>
    public byte[] SerializeWithType(object obj)
    {
        if (obj == null) return null;

        // 문자열인 경우
        if (obj is string str)
        {
            var wrapper = new TypedData(typeof(string).AssemblyQualifiedName, Encoding.UTF8.GetBytes(str));
            return Serialize(wrapper);
        }

        // 바이트 배열인 경우
        if (obj is byte[] bytes)
        {
            var wrapper = new TypedData(typeof(byte[]).AssemblyQualifiedName, bytes);
            return Serialize(wrapper);
        }

        // 그 외 객체
        var data = new TypedData(obj.GetType().AssemblyQualifiedName, Serialize(obj));

        return Serialize(data);
    }

    /// <summary>
    /// 타입 정보가 포함된 데이터를 원래 타입의 객체로 역직렬화
    /// </summary>
    public object DeserializeWithType(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        // 타입 정보 읽기
        var wrapper = Deserialize<TypedData>(data);

        if (wrapper == null || string.IsNullOrEmpty(wrapper.TypeName) || wrapper.Data == null)
        {
            return null;
        }

        // 원래 타입으로 역직렬화
        Type type = Type.GetType(wrapper.TypeName);

        if (type == null)
        {
            throw new InvalidOperationException($"타입을 찾을 수 없습니다: {wrapper.TypeName}");
        }

        if (type == typeof(string))
        {
            return Encoding.UTF8.GetString(wrapper.Data);
        }

        if (type == typeof(byte[]))
        {
            return wrapper.Data;
        }

        // 제네릭 Deserialize 메서드 동적 호출
        var method = typeof(JsonSerializer).GetMethod(nameof(Deserialize));
        var genericMethod = method.MakeGenericMethod(type);
        return genericMethod.Invoke(this, new object[] { wrapper.Data });
    }
}
