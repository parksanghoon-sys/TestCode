using MessageBusLib.Exceptions;
using MessageBusLib.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace MessageBusLib.Messages;


/// <summary>
/// 프로세스 간 메시지 전송을 위한 메시지 클래스
/// </summary>
[Serializable]
public class Message : IMessage
{
    private static readonly ISerializer _serializer = new JsonSerializer();

    /// <summary>
    /// 메시지 고유 ID
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// 메시지 타입 (토픽으로 사용)
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// 메시지 데이터
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// 메시지 생성 시간
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 발신자 프로세스 ID
    /// </summary>
    public int SenderId { get; set; }

    /// <summary>
    /// 메시지 헤더 (추가 메타데이터)
    /// </summary>
    public MessageHeaders Headers { get; set; }

    /// <summary>
    /// 기본 생성자
    /// </summary>
    public Message()
    {
        Timestamp = DateTime.Now;
        MessageId = Guid.NewGuid().ToString();
        SenderId = Process.GetCurrentProcess().Id;
        Headers = new MessageHeaders();
    }

    /// <summary>
    /// 토픽과 바이트 데이터로 메시지 생성
    /// </summary>
    public Message(string topic, byte[] data) : this()
    {
        Topic = topic;
        Data = data;
    }

    /// <summary>
    /// 토픽과 객체로 메시지 생성 (객체는 직렬화됨)
    /// </summary>
    public Message(string topic, object data) : this()
    {
        Topic = topic;
        Data = SerializeData(data);
    }

    /// <summary>
    /// 토픽과 객체, 메시지 ID로 메시지 생성
    /// </summary>
    public Message(string topic, object data, string messageId) : this(topic, data)
    {
        MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
    }

    /// <summary>
    /// 메시지 데이터를 지정된 타입으로 역직렬화
    /// </summary>
    public T GetData<T>()
    {
        return DeserializeData<T>(Data);
    }

    /// <summary>
    /// 객체를 바이트 배열로 직렬화
    /// </summary>
    private static byte[] SerializeData(object data)
    {
        try
        {
            return _serializer.SerializeWithType(data);
        }
        catch (Exception ex)
        {
            throw new MessageSerializationException("메시지 직렬화 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 바이트 배열을 지정된 타입으로 역직렬화
    /// </summary>
    private static T DeserializeData<T>(byte[] data)
    {
        if (data == null) return default;

        try
        {
            // 타입 정보를 포함하여 역직렬화된 객체가 T 타입인 경우
            var obj = _serializer.DeserializeWithType(data);

            if (obj is T typedObj)
            {
                return typedObj;
            }

            // 직접 T 타입으로 역직렬화 시도
            return _serializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            throw new MessageSerializationException("메시지 역직렬화 중 오류 발생", ex);
        }
    }
}
