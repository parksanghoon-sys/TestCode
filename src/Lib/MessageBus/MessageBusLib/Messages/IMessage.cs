using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBusLib.Messages;
/// <summary>
/// 메시지 인터페이스
/// </summary>
public interface IMessage
{
    /// <summary>
    /// 메시지 고유 ID
    /// </summary>
    string MessageId { get; }

    /// <summary>
    /// 메시지 토픽
    /// </summary>
    string Topic { get; }

    /// <summary>
    /// 메시지 데이터
    /// </summary>
    byte[] Data { get; }

    /// <summary>
    /// 메시지 생성 시간
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// 발신자 프로세스 ID
    /// </summary>
    int SenderId { get; }

    /// <summary>
    /// 메시지 데이터를 지정된 타입으로 역직렬화
    /// </summary>
    T GetData<T>();
}
