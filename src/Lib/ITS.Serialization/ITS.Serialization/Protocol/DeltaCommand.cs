using ITS.Serialization.Core;

namespace ITS.Serialization.Protocol
{
    /// <summary>
    /// 델타 업데이트 커맨드 (ITS 시스템의 델타 프로토콜 구현)
    ///
    /// 용도:
    /// - 객체의 부분 변경사항을 표현하는 커맨드
    /// - 전체 객체를 보내지 않고 변경된 부분만 전송 (네트워크 효율성)
    /// - ITS 시스템의 TargetList(Remove,2) 같은 텍스트 프로토콜을 구조화
    ///
    /// 사용 예시:
    /// <code>
    /// // 1. 리스트에서 항목 제거: TargetList.Remove(2)
    /// var removeCmd = new DeltaCommand
    /// {
    ///     Path = "TargetList",
    ///     Command = CommandType.Remove,
    ///     Index = 2
    /// };
    ///
    /// // 2. 리스트에 항목 추가: ExtAircraftList.Add(aircraft)
    /// var addCmd = new DeltaCommand
    /// {
    ///     Path = "ExtAircraftList",
    ///     Command = CommandType.Add,
    ///     ID = aircraft.ID,
    ///     Payload = serializer.Serialize(aircraft)  // 전체 객체 직렬화
    /// };
    ///
    /// // 3. 속성 값 변경: ExtAircraftList[0].Altitude = 15000
    /// var updateCmd = new DeltaCommand
    /// {
    ///     Path = "ExtAircraftList[0].Altitude",
    ///     Command = CommandType.Update,
    ///     Payload = serializer.Serialize(15000.0)  // 새 값만 직렬화
    /// };
    /// </code>
    ///
    /// 기존 ITS 프로토콜과의 관계:
    /// - 기존: "TargetList(Remove,2)" (텍스트, 파싱 필요, 크기 큼)
    /// - 신규: DeltaCommand { Path="TargetList", Command=Remove, Index=2 } (바이너리, 파싱 불필요, 크기 작음)
    /// </summary>
    [Serializable]
    public class DeltaCommand
    {
        /// <summary>
        /// 변경 대상 경로
        ///
        /// 경로 형식:
        /// - "TargetList" - 최상위 속성
        /// - "FlightList[0].Altitude" - 리스트의 특정 인덱스 항목의 속성
        /// - "ExtAircraftList<3>.WaypointList" - ID로 식별되는 항목의 속성
        ///
        /// 주의:
        /// - ITS 기존 프로토콜과 호환되도록 동일한 경로 형식 사용
        /// - 파싱은 수신측에서 처리 (이 라이브러리는 경로 문자열만 전달)
        /// </summary>
        [SerializableMember(1)]
        public string Path { get; set; }

        /// <summary>
        /// 커맨드 타입
        ///
        /// - Add: 리스트에 새 항목 추가
        /// - Remove: 리스트에서 항목 제거
        /// - Update: 속성 값 변경
        /// - Set: 리스트의 특정 항목 교체
        /// - Reset: 리스트 전체 초기화
        /// </summary>
        [SerializableMember(2)]
        public CommandType Command { get; set; }

        /// <summary>
        /// 리스트 인덱스 (Remove, Add 시 사용)
        ///
        /// - Remove: 제거할 항목의 인덱스
        /// - Add: 삽입할 위치 (-1이면 끝에 추가)
        /// - 기타: 사용 안 함 (-1)
        /// </summary>
        [SerializableMember(3)]
        public int Index { get; set; }

        /// <summary>
        /// 항목 ID (IndexedModel 사용 시)
        ///
        /// ITS 시스템의 IndexedModel:
        /// - 리스트의 각 항목을 ID로 식별
        /// - ExtAircraftList<3> → ID=3인 항목
        /// - Add 시 새 항목의 ID 지정
        /// - 사용 안 하면 -1
        /// </summary>
        [SerializableMember(4)]
        public int ID { get; set; }

        /// <summary>
        /// 직렬화된 페이로드 (실제 데이터)
        ///
        /// 내용:
        /// - Add/Set: 추가/교체할 객체 전체 (BinarySerializer.Serialize로 직렬화)
        /// - Update: 변경할 새 값 (int, double, string 등)
        /// - Remove/Reset: null (데이터 불필요)
        ///
        /// 예시:
        /// <code>
        /// // 객체 추가
        /// Payload = serializer.Serialize(new ExtAircraft { ... });
        ///
        /// // 값 변경
        /// Payload = serializer.Serialize(15000.0);
        ///
        /// // 항목 제거
        /// Payload = null;
        /// </code>
        /// </summary>
        [SerializableMember(5)]
        public byte[] Payload { get; set; }

        /// <summary>
        /// DeltaCommand 기본 생성자
        ///
        /// 기본값:
        /// - Path: 빈 문자열
        /// - Command: Update
        /// - Index: -1 (사용 안 함)
        /// - ID: -1 (사용 안 함)
        /// </summary>
        public DeltaCommand()
        {
            Path = string.Empty;
            Command = CommandType.Update;
            Index = -1;
            ID = -1;
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        ///
        /// 형식: "DeltaCommand[Command] Path (Index=N, ID=N, PayloadSize=N)"
        /// 예시: "DeltaCommand[Remove] TargetList (Index=2, ID=-1, PayloadSize=0)"
        /// </summary>
        /// <returns>커맨드 정보 문자열</returns>
        public override string ToString()
        {
            return $"DeltaCommand[{Command}] {Path} (Index={Index}, ID={ID}, PayloadSize={Payload?.Length ?? 0})";
        }
    }
}
