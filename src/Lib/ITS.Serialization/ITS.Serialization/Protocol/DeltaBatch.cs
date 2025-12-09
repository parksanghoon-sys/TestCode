using ITS.Serialization.Core;
using System.Collections.Generic;

namespace ITS.Serialization.Protocol
{
    /// <summary>
    /// 배치 델타 커맨드 (여러 변경사항을 한 번에 전송)
    ///
    /// 용도:
    /// - 여러 DeltaCommand를 하나의 네트워크 패킷으로 묶어서 전송
    /// - 네트워크 왕복 횟수 감소 → 성능 향상 (TCP 오버헤드 최소화)
    /// - FlightStatusManager의 50ms 타이머와 유사한 배치 전략
    ///
    /// 사용 시나리오:
    /// <code>
    /// // 시나리오 1: 여러 변경사항을 축적 후 일괄 전송
    /// var batch = new DeltaBatch();
    ///
    /// // Target 3개 제거
    /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Remove, Index = 2 });
    /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Remove, Index = 5 });
    /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Remove, Index = 8 });
    ///
    /// // ExtAircraft 1개 추가
    /// batch.Commands.Add(new DeltaCommand
    /// {
    ///     Path = "ExtAircraftList",
    ///     Command = CommandType.Add,
    ///     ID = 101,
    ///     Payload = serializer.Serialize(aircraft)
    /// });
    ///
    /// // Altitude 2개 업데이트
    /// batch.Commands.Add(new DeltaCommand
    /// {
    ///     Path = "ExtAircraftList[0].Altitude",
    ///     Command = CommandType.Update,
    ///     Payload = serializer.Serialize(15000.0)
    /// });
    /// batch.Commands.Add(new DeltaCommand
    /// {
    ///     Path = "ExtAircraftList[1].Altitude",
    ///     Command = CommandType.Update,
    ///     Payload = serializer.Serialize(18000.0)
    /// });
    ///
    /// // 배치 타임스탬프 설정 (Unix milliseconds)
    /// batch.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    ///
    /// // NetworkMessage로 감싸서 전송
    /// var msg = new NetworkMessage
    /// {
    ///     Type = MessageType.Batch,
    ///     Batch = batch
    /// };
    /// byte[] data = serializer.Serialize(msg);
    /// tcpClient.Send(data);
    /// </code>
    ///
    /// 성능 효과:
    /// - 개별 전송 시: 6개 DeltaCommand → 6개 TCP 패킷 → 오버헤드 큼
    /// - 배치 전송 시: 6개 DeltaCommand → 1개 TCP 패킷 → 오버헤드 83% 감소
    ///
    /// 실제 사례 (FlightStatusManager):
    /// - PropertyChanged: 매 변경마다 flag 설정
    /// - 50ms Timer: 축적된 변경사항을 한 번에 전송
    /// - 결과: 네트워크 패킷 93% 감소
    ///
    /// 배치 vs 개별 전송 비교:
    /// ┌─────────────────────────────────────────────────────────────────┐
    /// │ 개별 전송 (6개 DeltaCommand)                                     │
    /// ├─────────────────────────────────────────────────────────────────┤
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaCommand(50) = 100B   │
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaCommand(50) = 100B   │
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaCommand(50) = 100B   │
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaCommand(50) = 100B   │
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaCommand(50) = 100B   │
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaCommand(50) = 100B   │
    /// │ 합계: 600 bytes                                                  │
    /// └─────────────────────────────────────────────────────────────────┘
    ///
    /// ┌─────────────────────────────────────────────────────────────────┐
    /// │ 배치 전송 (DeltaBatch)                                           │
    /// ├─────────────────────────────────────────────────────────────────┤
    /// │ TCP Header(40) + NetworkMessage(10) + DeltaBatch {              │
    /// │   Timestamp(8) +                                                │
    /// │   DeltaCommand(50) +                                            │
    /// │   DeltaCommand(50) +                                            │
    /// │   DeltaCommand(50) +                                            │
    /// │   DeltaCommand(50) +                                            │
    /// │   DeltaCommand(50) +                                            │
    /// │   DeltaCommand(50)                                              │
    /// │ }                                                               │
    /// │ 합계: 358 bytes (40% 감소)                                       │
    /// └─────────────────────────────────────────────────────────────────┘
    ///
    /// 수신측 처리:
    /// <code>
    /// var msg = serializer.Deserialize<NetworkMessage>(data);
    /// if (msg.Type == MessageType.Batch)
    /// {
    ///     var batch = msg.Batch;
    ///     Console.WriteLine($"Received {batch.Commands.Count} commands @ {batch.Timestamp}");
    ///
    ///     // 순서대로 모든 커맨드 적용
    ///     foreach (var cmd in batch.Commands)
    ///     {
    ///         ApplyDeltaCommand(cmd);
    ///     }
    /// }
    /// </code>
    ///
    /// 주의사항:
    /// 1. 커맨드 순서가 중요함 (Remove → Add 순서 변경 시 인덱스 오류)
    /// 2. 배치가 너무 커지면 단일 TCP 패킷 크기(~1500 bytes) 초과 → 분할 전송
    /// 3. Timestamp는 순서 보장용 (out-of-order 감지)
    ///
    /// 권장 사용 전략:
    /// - 고빈도 업데이트 (초당 20회 이상): 배치 사용 ★★★
    /// - 중빈도 업데이트 (초당 5~20회): 배치 사용 ★★
    /// - 저빈도 업데이트 (초당 5회 미만): 개별 전송 (지연시간 최소화)
    /// </summary>
    [Serializable]
    public class DeltaBatch
    {
        /// <summary>
        /// 커맨드 리스트
        ///
        /// 순서:
        /// - 추가된 순서대로 적용됨 (FIFO)
        /// - Remove → Add 순서 주의 (인덱스 변경)
        ///
        /// 예시:
        /// <code>
        /// // 잘못된 순서
        /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Add, Index = 5 });
        /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Remove, Index = 3 });
        /// // → Remove로 인해 인덱스 5가 4로 변경됨 → Add가 잘못된 위치에 삽입
        ///
        /// // 올바른 순서
        /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Remove, Index = 3 });
        /// batch.Commands.Add(new DeltaCommand { Path = "TargetList", Command = CommandType.Add, Index = 4 });
        /// // → Remove 후 인덱스 재조정 → Add가 올바른 위치에 삽입
        /// </code>
        /// </summary>
        [SerializableMember(1)]
        public List<DeltaCommand> Commands { get; set; }

        /// <summary>
        /// 타임스탬프 (Unix milliseconds)
        ///
        /// 용도:
        /// - 배치 생성 시각 기록
        /// - out-of-order 패킷 감지 (네트워크 지연/재전송)
        /// - 성능 측정 (송신-수신 지연시간)
        ///
        /// 생성 방법:
        /// <code>
        /// // C#
        /// batch.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        ///
        /// // 또는
        /// batch.Timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        /// </code>
        ///
        /// 수신측 처리:
        /// <code>
        /// var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        /// var latency = currentTime - batch.Timestamp;
        /// if (latency > 100) // 100ms 이상 지연
        /// {
        ///     Console.WriteLine($"High latency: {latency}ms");
        /// }
        /// </code>
        ///
        /// out-of-order 감지:
        /// <code>
        /// long lastTimestamp = 0;
        /// if (batch.Timestamp < lastTimestamp)
        /// {
        ///     Console.WriteLine("Out-of-order batch detected, discarding...");
        ///     return; // 오래된 배치 무시
        /// }
        /// lastTimestamp = batch.Timestamp;
        /// </code>
        /// </summary>
        [SerializableMember(2)]
        public long Timestamp { get; set; }

        /// <summary>
        /// DeltaBatch 기본 생성자
        ///
        /// 초기화:
        /// - Commands: 빈 리스트 (Add로 커맨드 추가)
        /// - Timestamp: 0 (송신 직전에 설정)
        /// </summary>
        public DeltaBatch()
        {
            Commands = new List<DeltaCommand>();
            Timestamp = 0;
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        ///
        /// 형식: "DeltaBatch[N commands] @ timestamp"
        /// 예시: "DeltaBatch[6 commands] @ 1735123456789"
        /// </summary>
        /// <returns>배치 정보 문자열</returns>
        public override string ToString()
        {
            return $"DeltaBatch[{Commands?.Count ?? 0} commands] @ {Timestamp}";
        }
    }
}
