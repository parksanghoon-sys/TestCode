using ITS.Serialization.Core;

namespace ITS.Serialization.Protocol
{
    /// <summary>
    /// 네트워크 메시지 타입 (전송할 데이터의 종류)
    ///
    /// 3가지 메시지 유형:
    /// - DeltaUpdate: 단일 변경사항 (가장 일반적, 실시간 동기화)
    /// - FullSync: 전체 상태 (초기 연결, 재동기화)
    /// - Batch: 여러 변경사항 묶음 (성능 최적화)
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 델타 업데이트 (증분 변경)
        ///
        /// 용도: 실시간으로 하나의 변경사항 전송
        /// 예시: Target 하나 제거, Altitude 값 하나 변경
        /// 사용: NetworkMessage.Delta 필드 사용
        /// </summary>
        DeltaUpdate = 0,

        /// <summary>
        /// 전체 상태 동기화 (초기 연결 시)
        ///
        /// 용도: 전체 MCRCData 객체 전송
        /// 예시: 초기 연결, 접속 끊김 후 재연결
        /// 사용: NetworkMessage.FullState 필드 사용
        /// 크기: DeltaUpdate보다 크지만 한 번만 전송
        /// </summary>
        FullSync = 1,

        /// <summary>
        /// 배치 커맨드 (여러 델타 한 번에 전송)
        ///
        /// 용도: 여러 변경사항을 묶어서 효율적으로 전송
        /// 예시: Target 제거 + Altitude 변경 + Speed 변경
        /// 사용: NetworkMessage.Batch 필드 사용
        /// 장점: 네트워크 왕복 횟수 감소, TCP 오버헤드 최소화
        /// </summary>
        Batch = 2
    }

    /// <summary>
    /// 네트워크 메시지 (Wrapper Pattern - 다형성 지원)
    ///
    /// 용도:
    /// - 서로 다른 타입의 메시지를 하나의 형식으로 통합
    /// - TCP 수신측이 Type 필드를 보고 어떤 필드를 사용할지 결정
    ///
    /// 패턴:
    /// - Union 타입 시뮬레이션 (C#에는 union이 없음)
    /// - Type에 따라 Delta, FullState, Batch 중 하나만 유효
    ///
    /// 사용 예시:
    /// <code>
    /// // 송신측
    /// var msg = new NetworkMessage
    /// {
    ///     Type = MessageType.DeltaUpdate,
    ///     Delta = new DeltaCommand { ... }
    /// };
    /// byte[] data = serializer.Serialize(msg);
    /// tcpClient.Send(data);
    ///
    /// // 수신측
    /// var msg = serializer.Deserialize<NetworkMessage>(data);
    /// switch (msg.Type)
    /// {
    ///     case MessageType.DeltaUpdate:
    ///         ApplyDelta(msg.Delta);
    ///         break;
    ///     case MessageType.FullSync:
    ///         var state = serializer.Deserialize<MCRCData>(msg.FullState);
    ///         ApplyFullState(state);
    ///         break;
    ///     case MessageType.Batch:
    ///         foreach (var cmd in msg.Batch.Commands)
    ///             ApplyDelta(cmd);
    ///         break;
    /// }
    /// </code>
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        /// <summary>
        /// 메시지 타입
        /// </summary>
        [SerializableMember(1)]
        public MessageType Type { get; set; }

        /// <summary>
        /// 델타 커맨드 (Type == DeltaUpdate 일 때)
        /// </summary>
        [SerializableMember(2)]
        public DeltaCommand Delta { get; set; }

        /// <summary>
        /// 전체 상태 페이로드 (Type == FullSync 일 때)
        /// </summary>
        [SerializableMember(3)]
        public byte[] FullState { get; set; }

        /// <summary>
        /// 배치 커맨드 (Type == Batch 일 때)
        /// </summary>
        [SerializableMember(4)]
        public DeltaBatch Batch { get; set; }

        public NetworkMessage()
        {
            Type = MessageType.DeltaUpdate;
        }

        public override string ToString()
        {
            return $"NetworkMessage[{Type}]";
        }
    }
}
