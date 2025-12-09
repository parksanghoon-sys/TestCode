namespace ITS.Serialization.Protocol
{
    /// <summary>
    /// 델타 커맨드 타입 (변경 작업 종류)
    ///
    /// ITS 델타 프로토콜에서 지원하는 5가지 기본 작업:
    /// - Update: 속성 값 변경
    /// - Add: 리스트에 항목 추가
    /// - Remove: 리스트에서 항목 제거
    /// - Set: 리스트의 특정 항목 교체
    /// - Reset: 리스트 전체 초기화
    ///
    /// underlying type은 int (4 bytes)
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// 속성 업데이트
        ///
        /// 사용 예시:
        /// - ExtAircraftList[0].Altitude = 15000
        /// - Target.Status = Tracking
        ///
        /// DeltaCommand 설정:
        /// - Path: 변경할 속성 경로
        /// - Payload: 새 값 직렬화
        /// - Index, ID: 사용 안 함 (-1)
        /// </summary>
        Update = 0,

        /// <summary>
        /// 리스트 추가
        ///
        /// 사용 예시:
        /// - ExtAircraftList.Add(aircraft)
        /// - WaypointList.Insert(1, waypoint)
        ///
        /// DeltaCommand 설정:
        /// - Path: 리스트 경로
        /// - Payload: 추가할 객체 직렬화
        /// - Index: 삽입 위치 (-1이면 끝에 추가)
        /// - ID: IndexedModel 사용 시 새 항목 ID
        /// </summary>
        Add = 1,

        /// <summary>
        /// 리스트 제거
        ///
        /// 사용 예시:
        /// - TargetList.RemoveAt(2)
        /// - ExtAircraftList.Remove(aircraft)
        ///
        /// DeltaCommand 설정:
        /// - Path: 리스트 경로
        /// - Index: 제거할 항목 인덱스
        /// - Payload, ID: 사용 안 함
        /// </summary>
        Remove = 2,

        /// <summary>
        /// 리스트 갱신 (기존 항목 교체)
        ///
        /// 사용 예시:
        /// - TargetList[3] = newTarget
        ///
        /// DeltaCommand 설정:
        /// - Path: 리스트 경로
        /// - Index: 교체할 항목 인덱스
        /// - Payload: 새 객체 직렬화
        /// - ID: 사용 안 함
        /// </summary>
        Set = 3,

        /// <summary>
        /// 리스트 초기화 (전체 삭제)
        ///
        /// 사용 예시:
        /// - TargetList.Clear()
        ///
        /// DeltaCommand 설정:
        /// - Path: 리스트 경로
        /// - Index, ID, Payload: 사용 안 함
        /// </summary>
        Reset = 4
    }
}
