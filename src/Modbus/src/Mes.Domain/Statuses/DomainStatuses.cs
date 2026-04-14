namespace Mes.Domain.Statuses;

/// <summary>
/// 생산 오더의 실행 상태를 나타냅니다.
/// </summary>
public enum ProductionOrderStatus
{
    Released,
    Dispatched,
    InProgress,
    PartiallyCompleted,
    Completed,
    Closed,
    Cancelled
}

/// <summary>
/// 공정 실행의 진행 상태를 나타냅니다.
/// </summary>
public enum OperationExecutionStatus
{
    Ready,
    Queued,
    Running,
    Paused,
    Hold,
    Rework,
    Done,
    Aborted
}

/// <summary>
/// WIP 단위의 현재 실행 상태를 나타냅니다.
/// </summary>
public enum WipUnitStatus
{
    Queued,
    InProcess,
    Hold,
    Rework,
    Scrapped,
    Completed
}

/// <summary>
/// 자재 Lot의 사용 가능 상태를 나타냅니다.
/// </summary>
public enum MaterialLotStatus
{
    Available,
    Issued,
    Consumed,
    Returned,
    Blocked
}

/// <summary>
/// 품질 기록의 판정 진행 상태를 나타냅니다.
/// </summary>
public enum QualityRecordStatus
{
    Pending,
    InInspection,
    Passed,
    Failed,
    Hold,
    Released
}

/// <summary>
/// 예외 승인 요청의 검토 상태를 나타냅니다.
/// </summary>
public enum OverrideRequestStatus
{
    Requested,
    Approved,
    Rejected
}
