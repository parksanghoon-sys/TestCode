using System.Text.Json;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;
using Mes.Infrastructure.OperatorExecution.Sqlite;

namespace Mes.MockStation.Example;

/// <summary>
/// 실제 장비 없이도 operator-execution BFF와 WPF shell을 확인할 수 있도록
/// 고정된 SQLite mock station 시나리오를 생성합니다.
/// </summary>
public static class MockOperatorExecutionScenarioSeeder
{
    private const string ScenarioName = "mock-operator-station";
    private const string StationId = "ST-EXAMPLE-01";
    private const string DefaultActorId = "operator.mock.example";
    private const string QueuedOrderId = "PO-EXAMPLE-START";
    private const string QueuedOperationId = "OP-EXAMPLE-START";
    private const string RunningOrderId = "PO-EXAMPLE-RUN";
    private const string RunningOperationId = "OP-EXAMPLE-RUN";
    private const string RunningWipUnitId = "WIP-EXAMPLE-01";
    private const string RunningMaterialLotId = "LOT-EXAMPLE-01";

    /// <summary>
    /// example 폴더에서 사용할 기본 mock station 시나리오를 SQLite 파일로 시드합니다.
    /// </summary>
    /// <param name="request">시드 대상 경로와 기준 시각입니다.</param>
    /// <returns>생성된 manifest를 포함한 시드 결과입니다.</returns>
    public static MockOperatorExecutionSeedResult SeedSqlite(MockOperatorExecutionSeedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedRequest = NormalizeRequest(request);
        var scenario = CreateScenario(normalizedRequest.SeededAt);
        var store = new SqliteOperatorExecutionStore(new SqliteOperatorExecutionStoreOptions
        {
            DatabaseFilePath = normalizedRequest.DatabaseFilePath
        });

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = scenario.ProductionOrders,
            OperationExecutions = scenario.OperationExecutions,
            WipUnits = scenario.WipUnits,
            MaterialLots = scenario.MaterialLots,
            MaterialRequirements = scenario.MaterialRequirements
        });

        var manifest = CreateManifest(normalizedRequest, scenario);
        WriteManifest(normalizedRequest.ManifestFilePath, manifest);

        return new MockOperatorExecutionSeedResult
        {
            Manifest = manifest
        };
    }

    /// <summary>
    /// 입력 경로와 기준 시각을 실행 가능한 절대 경로 기준으로 정규화합니다.
    /// </summary>
    /// <param name="request">정규화할 원본 시드 요청입니다.</param>
    /// <returns>절대 경로 기준으로 정리된 시드 요청입니다.</returns>
    private static MockOperatorExecutionSeedRequest NormalizeRequest(MockOperatorExecutionSeedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DatabaseFilePath))
        {
            throw new ArgumentException("Database file path is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ManifestFilePath))
        {
            throw new ArgumentException("Manifest file path is required.", nameof(request));
        }

        var databaseFilePath = Path.GetFullPath(request.DatabaseFilePath);
        var manifestFilePath = Path.GetFullPath(request.ManifestFilePath);
        EnsureParentDirectoryExists(databaseFilePath);
        EnsureParentDirectoryExists(manifestFilePath);

        return new MockOperatorExecutionSeedRequest
        {
            DatabaseFilePath = databaseFilePath,
            ManifestFilePath = manifestFilePath,
            SeededAt = request.SeededAt == default
                ? new DateTimeOffset(2026, 4, 17, 13, 0, 0, TimeSpan.Zero)
                : request.SeededAt
        };
    }

    /// <summary>
    /// WPF와 Experience API 예제에서 함께 사용할 고정 mock station 도메인 상태를 만듭니다.
    /// </summary>
    /// <param name="seededAt">시나리오 기준 시각입니다.</param>
    /// <returns>SQLite 시드와 manifest 작성에 필요한 시나리오 데이터입니다.</returns>
    private static MockOperatorExecutionScenarioData CreateScenario(DateTimeOffset seededAt)
    {
        var queuedOrder = CreateProductionOrder(QueuedOrderId, "ITEM-EXAMPLE-START", seededAt.AddHours(-3));
        var queuedOperation = CreateStationQueuedOperation(
            queuedOrder.Id.ToString(),
            QueuedOperationId,
            StationId,
            10,
            "EA");
        queuedOrder.AttachOperation(queuedOperation.Id);

        var runningOrder = CreateProductionOrder(RunningOrderId, "ITEM-EXAMPLE-RUN", seededAt.AddHours(-2));
        var runningOperation = CreateRunningOperation(
            runningOrder.Id.ToString(),
            RunningOperationId,
            StationId,
            20,
            seededAt.AddMinutes(-45));
        runningOrder.AttachOperation(runningOperation.Id);

        var runningWipUnit = new WipUnit(new WipUnitId(RunningWipUnitId), "ITEM-EXAMPLE-RUN");
        runningWipUnit.StartProcessing(runningOperation.Id);

        var runningMaterialLot = MaterialLot.Create(
            new MaterialLotId(RunningMaterialLotId),
            "MAT-EXAMPLE-RUN",
            new MeasuredQuantity(12m, "EA"));

        var materialRequirements = CreateMaterialRequirements(
            queuedOperation,
            runningOperation,
            seededAt.AddMinutes(-30));

        return new MockOperatorExecutionScenarioData
        {
            ProductionOrders = [queuedOrder, runningOrder],
            OperationExecutions = [queuedOperation, runningOperation],
            WipUnits = [runningWipUnit],
            MaterialLots = [runningMaterialLot],
            MaterialRequirements = materialRequirements
        };
    }

    /// <summary>
    /// 두 개의 예제 공정에 대응하는 required-material snapshot을 생성합니다.
    /// </summary>
    /// <param name="queuedOperation">station-assigned queued 공정입니다.</param>
    /// <param name="runningOperation">이미 running 상태인 공정입니다.</param>
    /// <param name="projectedAt">snapshot 생성 기준 시각입니다.</param>
    /// <returns>큐 projection에 노출할 자재 requirement 목록입니다.</returns>
    private static IReadOnlyList<OperationMaterialRequirementSnapshot> CreateMaterialRequirements(
        OperationExecution queuedOperation,
        OperationExecution runningOperation,
        DateTimeOffset projectedAt)
    {
        var projector = new OperationMaterialRequirementProjector();
        var queuedRequirements = projector.ProjectFromOperationAttachment(
            new ProjectOperationMaterialRequirementsRequest(
                queuedOperation.Id,
                projectedAt,
                [new OperationMaterialRequirementInput(1, "MAT-EXAMPLE-START", new MeasuredQuantity(4m, "EA"), "BOM-EXAMPLE-START")]));
        var runningRequirements = projector.ProjectFromOperationAttachment(
            new ProjectOperationMaterialRequirementsRequest(
                runningOperation.Id,
                projectedAt,
                [new OperationMaterialRequirementInput(1, "MAT-EXAMPLE-RUN", new MeasuredQuantity(3m, "EA"), "BOM-EXAMPLE-RUN")]));

        return queuedRequirements
            .Concat(runningRequirements)
            .ToList();
    }

    /// <summary>
    /// release된 생산오더 aggregate를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">생산오더 식별자입니다.</param>
    /// <param name="itemCode">생산 품목 코드입니다.</param>
    /// <param name="releasedAt">오더 release 시각입니다.</param>
    /// <returns>release 상태의 생산오더 aggregate입니다.</returns>
    private static ProductionOrder CreateProductionOrder(
        string productionOrderId,
        string itemCode,
        DateTimeOffset releasedAt)
    {
        return ProductionOrder.Release(
            new ProductionOrderId(productionOrderId),
            itemCode,
            "ROUTE-EXAMPLE-A",
            releasedAt);
    }

    /// <summary>
    /// station queue에 바로 보이도록 station이 지정된 queued 공정을 복원합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="stationId">할당된 station 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="quantityUnit">공정 authoritative 수량 단위입니다.</param>
    /// <returns>station-assigned queued 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateStationQueuedOperation(
        string productionOrderId,
        string operationExecutionId,
        string stationId,
        int operationSequence,
        string quantityUnit)
    {
        return OperationExecution.Restore(new OperationExecutionRestoreState(
            new OperationExecutionId(operationExecutionId),
            new ProductionOrderId(productionOrderId),
            operationSequence,
            quantityUnit,
            OperationExecutionStatus.Queued,
            new StationId(stationId),
            null,
            null,
            null,
            null,
            null,
            null,
            MeasuredQuantity.Zero(quantityUnit),
            MeasuredQuantity.Zero(quantityUnit)));
    }

    /// <summary>
    /// station에서 바로 자재 투입과 완료를 시험할 수 있도록 running 공정을 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 생산오더 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="stationId">실행 station 식별자입니다.</param>
    /// <param name="operationSequence">공정 순번입니다.</param>
    /// <param name="startedAt">공정 시작 시각입니다.</param>
    /// <returns>running 상태의 공정 실행 aggregate입니다.</returns>
    private static OperationExecution CreateRunningOperation(
        string productionOrderId,
        string operationExecutionId,
        string stationId,
        int operationSequence,
        DateTimeOffset startedAt)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            new ProductionOrderId(productionOrderId),
            operationSequence,
            "EA");
        operation.QueueForExecution();
        operation.Start(new StationId(stationId), startedAt);
        return operation;
    }

    /// <summary>
    /// 시드 결과를 example 스크립트와 smoke test가 읽을 수 있는 manifest로 변환합니다.
    /// </summary>
    /// <param name="request">정규화된 시드 요청입니다.</param>
    /// <param name="scenario">이미 구성된 mock station 시나리오입니다.</param>
    /// <returns>example 실행에 필요한 manifest입니다.</returns>
    private static MockOperatorExecutionScenarioManifest CreateManifest(
        MockOperatorExecutionSeedRequest request,
        MockOperatorExecutionScenarioData scenario)
    {
        return new MockOperatorExecutionScenarioManifest
        {
            ScenarioName = ScenarioName,
            StationId = StationId,
            DefaultActorId = DefaultActorId,
            SeededAt = request.SeededAt,
            DatabaseFilePath = request.DatabaseFilePath,
            ManifestFilePath = request.ManifestFilePath,
            QueuedOperation = new MockOperatorExecutionOperationExample
            {
                ProductionOrderId = QueuedOrderId,
                OperationExecutionId = QueuedOperationId,
                OperationSequence = 10,
                QuantityUnit = "EA",
                MaterialCode = "MAT-EXAMPLE-START",
                MaterialQuantityUnit = "EA"
            },
            RunningOperation = new MockOperatorExecutionOperationExample
            {
                ProductionOrderId = RunningOrderId,
                OperationExecutionId = RunningOperationId,
                OperationSequence = 20,
                QuantityUnit = "EA",
                WipUnitId = RunningWipUnitId,
                MaterialLotId = RunningMaterialLotId,
                MaterialCode = scenario.MaterialLots[0].MaterialCode,
                MaterialQuantityUnit = scenario.MaterialLots[0].AvailableQuantity.Unit
            }
        };
    }

    /// <summary>
    /// 생성된 manifest를 JSON 파일로 기록합니다.
    /// </summary>
    /// <param name="manifestFilePath">기록할 manifest 파일 경로입니다.</param>
    /// <param name="manifest">기록할 manifest 본문입니다.</param>
    private static void WriteManifest(string manifestFilePath, MockOperatorExecutionScenarioManifest manifest)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        File.WriteAllText(
            manifestFilePath,
            JsonSerializer.Serialize(manifest, options));
    }

    /// <summary>
    /// 파일 경로의 상위 디렉터리를 보장합니다.
    /// </summary>
    /// <param name="filePath">상위 디렉터리를 만들 파일 경로입니다.</param>
    private static void EnsureParentDirectoryExists(string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("The file path must include a parent directory.");
        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// SQLite 시드에 필요한 중간 시나리오 객체를 묶습니다.
    /// </summary>
    private sealed record MockOperatorExecutionScenarioData
    {
        /// <summary>
        /// 시드할 생산오더 목록입니다.
        /// </summary>
        public IReadOnlyList<ProductionOrder> ProductionOrders { get; init; } = [];

        /// <summary>
        /// 시드할 공정 실행 목록입니다.
        /// </summary>
        public IReadOnlyList<OperationExecution> OperationExecutions { get; init; } = [];

        /// <summary>
        /// 시드할 WIP 목록입니다.
        /// </summary>
        public IReadOnlyList<WipUnit> WipUnits { get; init; } = [];

        /// <summary>
        /// 시드할 자재 lot 목록입니다.
        /// </summary>
        public IReadOnlyList<MaterialLot> MaterialLots { get; init; } = [];

        /// <summary>
        /// queue projection에 필요한 자재 requirement snapshot 목록입니다.
        /// </summary>
        public IReadOnlyList<OperationMaterialRequirementSnapshot> MaterialRequirements { get; init; } = [];
    }
}
