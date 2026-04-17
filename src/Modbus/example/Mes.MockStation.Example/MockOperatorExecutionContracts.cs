namespace Mes.MockStation.Example;

/// <summary>
/// mock station SQLite 예제를 시드할 때 필요한 입력 경로와 기준 시각을 묶습니다.
/// </summary>
public sealed record MockOperatorExecutionSeedRequest
{
    /// <summary>
    /// 시드할 SQLite 데이터베이스 파일 경로입니다.
    /// </summary>
    public string DatabaseFilePath { get; init; } = string.Empty;

    /// <summary>
    /// 예제 실행에 사용할 manifest JSON 파일 경로입니다.
    /// </summary>
    public string ManifestFilePath { get; init; } = string.Empty;

    /// <summary>
    /// 예제 시드 기준 시각입니다.
    /// </summary>
    public DateTimeOffset SeededAt { get; init; }
}

/// <summary>
/// mock station SQLite 시드 결과를 묶습니다.
/// </summary>
public sealed record MockOperatorExecutionSeedResult
{
    /// <summary>
    /// 생성된 예제 manifest입니다.
    /// </summary>
    public MockOperatorExecutionScenarioManifest Manifest { get; init; } = new();
}

/// <summary>
/// example 폴더에서 API와 WPF를 함께 띄울 때 참조하는 고정 시나리오 메타데이터입니다.
/// </summary>
public sealed record MockOperatorExecutionScenarioManifest
{
    /// <summary>
    /// 예제 시나리오 이름입니다.
    /// </summary>
    public string ScenarioName { get; init; } = string.Empty;

    /// <summary>
    /// 시나리오가 바인딩할 기본 station 식별자입니다.
    /// </summary>
    public string StationId { get; init; } = string.Empty;

    /// <summary>
    /// WPF 예제가 기본으로 사용할 actor 식별자입니다.
    /// </summary>
    public string DefaultActorId { get; init; } = string.Empty;

    /// <summary>
    /// 시나리오 시드 기준 시각입니다.
    /// </summary>
    public DateTimeOffset SeededAt { get; init; }

    /// <summary>
    /// 생성된 SQLite 데이터베이스 파일의 절대 경로입니다.
    /// </summary>
    public string DatabaseFilePath { get; init; } = string.Empty;

    /// <summary>
    /// 생성된 manifest 파일의 절대 경로입니다.
    /// </summary>
    public string ManifestFilePath { get; init; } = string.Empty;

    /// <summary>
    /// `start-operation`을 확인하기 위한 station-assigned queued 공정 예제입니다.
    /// </summary>
    public MockOperatorExecutionOperationExample QueuedOperation { get; init; } = new();

    /// <summary>
    /// `record-material-consumption`과 `complete-operation`을 확인하기 위한 running 공정 예제입니다.
    /// </summary>
    public MockOperatorExecutionOperationExample RunningOperation { get; init; } = new();
}

/// <summary>
/// example 폴더에서 수동 또는 자동 smoke 실행에 사용하는 단일 공정 예제 정보입니다.
/// </summary>
public sealed record MockOperatorExecutionOperationExample
{
    /// <summary>
    /// 공정이 속한 생산오더 식별자입니다.
    /// </summary>
    public string ProductionOrderId { get; init; } = string.Empty;

    /// <summary>
    /// 공정 실행 식별자입니다.
    /// </summary>
    public string OperationExecutionId { get; init; } = string.Empty;

    /// <summary>
    /// 공정 순번입니다.
    /// </summary>
    public int OperationSequence { get; init; }

    /// <summary>
    /// 공정 authoritative 수량 단위입니다.
    /// </summary>
    public string QuantityUnit { get; init; } = string.Empty;

    /// <summary>
    /// 예제에 연결된 기본 WIP 식별자입니다.
    /// </summary>
    public string? WipUnitId { get; init; }

    /// <summary>
    /// 예제에 연결된 기본 자재 lot 식별자입니다.
    /// </summary>
    public string? MaterialLotId { get; init; }

    /// <summary>
    /// 예제에 연결된 기본 자재 코드입니다.
    /// </summary>
    public string? MaterialCode { get; init; }

    /// <summary>
    /// 예제에 연결된 자재 수량 단위입니다.
    /// </summary>
    public string? MaterialQuantityUnit { get; init; }
}
