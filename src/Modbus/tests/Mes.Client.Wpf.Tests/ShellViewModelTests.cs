using System.Text.Json;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Client.Wpf.Configuration;
using Mes.Client.Wpf.OperatorExecution;
using Mes.Client.Wpf.Shell;
using Microsoft.Extensions.Options;

namespace Mes.Client.Wpf.Tests;

/// <summary>
/// 스테이션 셸 view model의 세션, 선택, 명령 실행 흐름을 검증합니다.
/// </summary>
public sealed class ShellViewModelTests
{
    /// <summary>
    /// 다른 스테이션으로 다시 바인딩하면 이전 작업 큐 스냅샷이 즉시 정리되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task BindStation_WhenRebindingAfterQueueLoad_ClearsSnapshotState()
    {
        var snapshotTakenAt = new DateTimeOffset(2026, 4, 17, 9, 30, 0, TimeSpan.Zero);
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (request, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Success(
                        new GetStationWorkQueueResponseContract(
                            request.StationId,
                            snapshotTakenAt,
                            [CreateQueueItem(request.StationId, "Queued")])))
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = "ST-1001";
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);

        Assert.Single(viewModel.QueueItems);
        Assert.Contains("ST-1001", viewModel.QueueSnapshotCaption);

        viewModel.StationId = "ST-2002";
        viewModel.BindStationCommand.Execute(null);

        Assert.Empty(viewModel.QueueItems);
        Assert.Equal("작업 큐 미조회", viewModel.QueueSnapshotCaption);
        Assert.Contains("ST-2002", viewModel.SessionCaption);
        Assert.Equal("info", viewModel.MessageSeverity);
    }

    /// <summary>
    /// 시작 명령이 공통 command context 정책을 사용해 전송되고 후속 refresh까지 반영되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperation_WhenSelectedQueuedItemExists_SendsCommandAndRefreshesSnapshot()
    {
        var stationId = "ST-1001";
        var queuedItem = CreateQueueItem(stationId, "Queued");
        var runningItem = CreateQueueItem(stationId, "Running", operationQuantityUnit: "KG");
        var refreshResponses = new Queue<GetStationWorkQueueResponseContract>(
            [
                new GetStationWorkQueueResponseContract(
                    stationId,
                    new DateTimeOffset(2026, 4, 17, 9, 30, 0, TimeSpan.Zero),
                    [queuedItem]),
                new GetStationWorkQueueResponseContract(
                    stationId,
                    new DateTimeOffset(2026, 4, 17, 9, 31, 0, TimeSpan.Zero),
                    [runningItem])
            ]);

        StartOperationCommandContract? capturedCommand = null;
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Success(
                        refreshResponses.Dequeue())),
            StartOperationAsyncHandler = (command, _) =>
            {
                capturedCommand = command;
                return Task.FromResult(
                    OperatorExecutionStationClientResult<StartOperationResponseContract>.Success(
                        new StartOperationResponseContract(
                            true,
                            command.CommandId,
                            new DateTimeOffset(2026, 4, 17, 9, 30, 10, TimeSpan.Zero),
                            command.Payload.OperationExecutionId,
                            "Running",
                            new DateTimeOffset(2026, 4, 17, 9, 30, 12, TimeSpan.Zero))));
            }
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = stationId;
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);
        viewModel.SelectedQueueItem = viewModel.QueueItems.Single();

        await ExecuteAsyncCommandAndWaitAsync(viewModel.StartOperationCommand, viewModel);

        Assert.NotNull(capturedCommand);
        Assert.Equal(queuedItem.ProductionOrderId, capturedCommand!.Payload.ProductionOrderId);
        Assert.Equal(queuedItem.OperationExecutionId, capturedCommand.Payload.OperationExecutionId);
        Assert.Equal(queuedItem.OperationSequence, capturedCommand.Payload.OperationSequence);
        Assert.Null(capturedCommand.Payload.QuantityUnit);
        Assert.Equal("operator.demo", capturedCommand.ActorId);
        Assert.Equal(BffChannelValues.Wpf, capturedCommand.Channel);
        Assert.Equal(stationId, capturedCommand.StationId);
        Assert.Equal($"wpf:{stationId}:{queuedItem.OperationExecutionId}", capturedCommand.CorrelationId);
        Assert.Equal(
            $"wpf:{OperatorExecutionCommandTypes.StartOperation}:{stationId}:{queuedItem.OperationExecutionId}",
            capturedCommand.IdempotencyKey);
        Assert.Equal("작업 시작 접수 완료", viewModel.MessageTitle);
        Assert.Single(viewModel.QueueItems);
        Assert.Equal("Running", viewModel.QueueItems.Single().Status);
        Assert.NotNull(viewModel.SelectedQueueItem);
        Assert.Equal("Running", viewModel.SelectedQueueItem!.Status);
    }

    /// <summary>
    /// 자재 스캔 검증 명령이 WIP와 lot 기준으로 전송되고 authoritative lot 정보를 입력 필드에 반영하는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task RecordMaterialScan_WhenSelectedRunningItemExists_ValidatesAndAppliesAuthoritativeMaterialFields()
    {
        var stationId = "ST-1001";
        var requiredMaterials = new[]
        {
            new RequiredMaterialContract("MAT-RED", new MeasuredQuantityContract(5m, "KG"))
        };
        var runningItem = CreateQueueItem(stationId, "Running", requiredMaterials);
        RecordMaterialScanCommandContract? capturedCommand = null;
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Success(
                        new GetStationWorkQueueResponseContract(
                            stationId,
                            new DateTimeOffset(2026, 4, 17, 9, 33, 0, TimeSpan.Zero),
                            [runningItem]))),
            RecordMaterialScanAsyncHandler = (command, _) =>
            {
                capturedCommand = command;
                return Task.FromResult(
                    OperatorExecutionStationClientResult<RecordMaterialScanResponseContract>.Success(
                        new RecordMaterialScanResponseContract(
                            true,
                            command.CommandId,
                            new DateTimeOffset(2026, 4, 17, 9, 33, 20, TimeSpan.Zero),
                            command.Payload.OperationExecutionId,
                            command.Payload.WipUnitId,
                            command.Payload.MaterialLotId,
                            "MAT-RED",
                            new MeasuredQuantityContract(18m, "KG"))));
            }
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = stationId;
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);
        viewModel.SelectedQueueItem = viewModel.QueueItems.Single();

        Assert.False(viewModel.RecordMaterialScanCommand.CanExecute(null));

        viewModel.MaterialConsumptionWipUnitId = "WIP-1001";
        viewModel.MaterialConsumptionMaterialLotId = "LOT-1001";

        Assert.True(viewModel.RecordMaterialScanCommand.CanExecute(null));

        await ExecuteAsyncCommandAndWaitAsync(viewModel.RecordMaterialScanCommand, viewModel);

        Assert.NotNull(capturedCommand);
        Assert.Equal(runningItem.OperationExecutionId, capturedCommand!.Payload.OperationExecutionId);
        Assert.Equal("WIP-1001", capturedCommand.Payload.WipUnitId);
        Assert.Equal("LOT-1001", capturedCommand.Payload.MaterialLotId);
        Assert.Equal("MAT-RED", capturedCommand.Payload.MaterialCode);
        Assert.Equal($"wpf:{stationId}:{runningItem.OperationExecutionId}", capturedCommand.CorrelationId);
        Assert.StartsWith(
            $"wpf:{OperatorExecutionCommandTypes.RecordMaterialScan}:{stationId}:{runningItem.OperationExecutionId}:",
            capturedCommand.IdempotencyKey,
            StringComparison.Ordinal);
        Assert.Equal("자재 스캔 검증 완료", viewModel.MessageTitle);
        Assert.Equal("MAT-RED", viewModel.MaterialConsumptionMaterialCode);
        Assert.Equal("KG", viewModel.MaterialConsumptionQuantityUnit);
    }

    /// <summary>
    /// 자재 투입 명령이 기본 자재 값을 프리필하고 멀티샷 idempotency 범위로 전송되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task RecordMaterialConsumption_WhenSelectedRunningItemExists_PrefillsDefaultsAndRefreshesSnapshot()
    {
        var stationId = "ST-1001";
        var requiredMaterials = new[]
        {
            new RequiredMaterialContract("MAT-RED", new MeasuredQuantityContract(5m, "KG"))
        };
        var runningItem = CreateQueueItem(stationId, "Running", requiredMaterials);
        var refreshResponses = new Queue<GetStationWorkQueueResponseContract>(
            [
                new GetStationWorkQueueResponseContract(
                    stationId,
                    new DateTimeOffset(2026, 4, 17, 9, 35, 0, TimeSpan.Zero),
                    [runningItem]),
                new GetStationWorkQueueResponseContract(
                    stationId,
                    new DateTimeOffset(2026, 4, 17, 9, 36, 0, TimeSpan.Zero),
                    [runningItem])
            ]);

        RecordMaterialConsumptionCommandContract? capturedCommand = null;
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Success(
                        refreshResponses.Dequeue())),
            RecordMaterialConsumptionAsyncHandler = (command, _) =>
            {
                capturedCommand = command;
                return Task.FromResult(
                    OperatorExecutionStationClientResult<RecordMaterialConsumptionResponseContract>.Success(
                        new RecordMaterialConsumptionResponseContract(
                            true,
                            command.CommandId,
                            new DateTimeOffset(2026, 4, 17, 9, 35, 30, TimeSpan.Zero),
                            command.Payload.MaterialLotId,
                            new MeasuredQuantityContract(17.5m, "KG"),
                            true)));
            }
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = stationId;
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);
        viewModel.SelectedQueueItem = viewModel.QueueItems.Single();

        Assert.Equal("MAT-RED", viewModel.MaterialConsumptionMaterialCode);
        Assert.Equal("KG", viewModel.MaterialConsumptionQuantityUnit);
        Assert.False(viewModel.RecordMaterialConsumptionCommand.CanExecute(null));

        viewModel.MaterialConsumptionWipUnitId = "WIP-1001";
        viewModel.MaterialConsumptionMaterialLotId = "LOT-1001";
        viewModel.MaterialConsumptionQuantityText = "2.5";

        Assert.True(viewModel.RecordMaterialConsumptionCommand.CanExecute(null));

        await ExecuteAsyncCommandAndWaitAsync(viewModel.RecordMaterialConsumptionCommand, viewModel);

        Assert.NotNull(capturedCommand);
        Assert.Equal(runningItem.OperationExecutionId, capturedCommand!.Payload.OperationExecutionId);
        Assert.Equal("WIP-1001", capturedCommand.Payload.WipUnitId);
        Assert.Equal("LOT-1001", capturedCommand.Payload.MaterialLotId);
        Assert.Equal("MAT-RED", capturedCommand.Payload.MaterialCode);
        Assert.Equal(2.5m, capturedCommand.Payload.Quantity.Value);
        Assert.Equal("KG", capturedCommand.Payload.Quantity.Unit);
        Assert.Equal($"wpf:{stationId}:{runningItem.OperationExecutionId}", capturedCommand.CorrelationId);
        Assert.StartsWith(
            $"wpf:{OperatorExecutionCommandTypes.RecordMaterialConsumption}:{stationId}:{runningItem.OperationExecutionId}:",
            capturedCommand.IdempotencyKey,
            StringComparison.Ordinal);
        Assert.Equal("자재 투입 접수 완료", viewModel.MessageTitle);
        Assert.Single(viewModel.QueueItems);
        Assert.NotNull(viewModel.SelectedQueueItem);
        Assert.Equal("MAT-RED", viewModel.MaterialConsumptionMaterialCode);
        Assert.Equal("KG", viewModel.MaterialConsumptionQuantityUnit);
        Assert.Equal(string.Empty, viewModel.MaterialConsumptionWipUnitId);
        Assert.Equal(string.Empty, viewModel.MaterialConsumptionMaterialLotId);
        Assert.Equal(string.Empty, viewModel.MaterialConsumptionQuantityText);
    }

    /// <summary>
    /// 완료 입력이 유효하지 않으면 완료 명령이 비활성화되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperation_WhenGoodQuantityIsInvalid_DisablesCommand()
    {
        var stationId = "ST-1001";
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Success(
                        new GetStationWorkQueueResponseContract(
                            stationId,
                            new DateTimeOffset(2026, 4, 17, 9, 30, 0, TimeSpan.Zero),
                            [CreateQueueItem(stationId, "Running")])))
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = stationId;
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);
        viewModel.SelectedQueueItem = viewModel.QueueItems.Single();
        viewModel.CompletionGoodQuantityText = "abc";

        Assert.False(viewModel.CompleteOperationCommand.CanExecute(null));
    }

    /// <summary>
    /// 완료 명령이 입력 수량과 단위를 전송하고 성공 후 선택 상태를 비우는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperation_WhenSelectedRunningItemExists_SendsQuantitiesAndClearsCompletedSelection()
    {
        var stationId = "ST-1001";
        var runningItem = CreateQueueItem(stationId, "Running", operationQuantityUnit: "KG");
        var refreshResponses = new Queue<GetStationWorkQueueResponseContract>(
            [
                new GetStationWorkQueueResponseContract(
                    stationId,
                    new DateTimeOffset(2026, 4, 17, 9, 40, 0, TimeSpan.Zero),
                    [runningItem]),
                new GetStationWorkQueueResponseContract(
                    stationId,
                    new DateTimeOffset(2026, 4, 17, 9, 42, 0, TimeSpan.Zero),
                    [])
            ]);

        CompleteOperationCommandContract? capturedCommand = null;
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Success(
                        refreshResponses.Dequeue())),
            CompleteOperationAsyncHandler = (command, _) =>
            {
                capturedCommand = command;
                return Task.FromResult(
                    OperatorExecutionStationClientResult<CompleteOperationResponseContract>.Success(
                        new CompleteOperationResponseContract(
                            true,
                            command.CommandId,
                            new DateTimeOffset(2026, 4, 17, 9, 41, 0, TimeSpan.Zero),
                            command.Payload.OperationExecutionId,
                            "Done",
                            new DateTimeOffset(2026, 4, 17, 9, 41, 5, TimeSpan.Zero),
                            "pending-projection")));
            }
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = stationId;
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);
        viewModel.SelectedQueueItem = viewModel.QueueItems.Single();
        Assert.Equal("KG", viewModel.CompletionQuantityUnit);
        viewModel.CompletionGoodQuantityText = "12.5";
        viewModel.CompletionScrapQuantityText = "0.25";

        await ExecuteAsyncCommandAndWaitAsync(viewModel.CompleteOperationCommand, viewModel);

        Assert.NotNull(capturedCommand);
        Assert.Equal(runningItem.OperationExecutionId, capturedCommand!.Payload.OperationExecutionId);
        Assert.Equal(12.5m, capturedCommand.Payload.GoodQuantity.Value);
        Assert.Equal("KG", capturedCommand.Payload.GoodQuantity.Unit);
        Assert.NotNull(capturedCommand.Payload.ScrapQuantity);
        Assert.Equal(0.25m, capturedCommand.Payload.ScrapQuantity!.Value);
        Assert.Equal("KG", capturedCommand.Payload.ScrapQuantity.Unit);
        Assert.Equal(CompletionModeValues.Manual, capturedCommand.Payload.CompletionMode);
        Assert.Equal("작업 완료 접수 완료", viewModel.MessageTitle);
        Assert.Empty(viewModel.QueueItems);
        Assert.Null(viewModel.SelectedQueueItem);
    }

    /// <summary>
    /// warning 성격의 BFF 실패가 severity와 패널 색상까지 반영되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task RefreshQueue_WhenConflictFailureReturned_PropagatesWarningSeverity()
    {
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) =>
                Task.FromResult(
                    OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>.Fail(
                        new OperatorExecutionStationClientFailure(
                            409,
                            new BffProblemDetails
                            {
                                Detail = "이미 완료된 작업입니다.",
                                Extensions = new Dictionary<string, JsonElement>
                                {
                                    ["errorCode"] = JsonSerializer.SerializeToElement("operator_execution.conflict")
                                }
                            },
                            null,
                            null)))
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = "ST-1001";
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);

        Assert.Equal("warning", viewModel.MessageSeverity);
        Assert.Equal("상태 충돌", viewModel.MessageTitle);
        Assert.Equal("#FFFDF6E5", viewModel.MessagePanelBackground);
        Assert.Equal("#FFE5CE8C", viewModel.MessagePanelBorderBrush);
    }

    /// <summary>
    /// 예상하지 못한 예외가 async command 경계를 벗어나지 않고 작업자 메시지로 전환되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task RefreshQueue_WhenUnexpectedExceptionThrown_ConvertsToClientErrorMessage()
    {
        var stationClient = new StubStationClient
        {
            GetStationWorkQueueAsyncHandler = (_, _) => throw new InvalidOperationException("테스트용 예외")
        };
        var viewModel = CreateViewModel(stationClient);

        viewModel.StationId = "ST-1001";
        viewModel.BindStationCommand.Execute(null);
        await ExecuteAsyncCommandAndWaitAsync(viewModel.RefreshQueueCommand, viewModel);

        Assert.Equal("error", viewModel.MessageSeverity);
        Assert.Equal("클라이언트 오류", viewModel.MessageTitle);
        Assert.Contains("테스트용 예외", viewModel.MessageDetail);
    }

    /// <summary>
    /// 테스트용 view model을 생성합니다.
    /// </summary>
    /// <param name="stationClient">사용할 station client입니다.</param>
    /// <returns>테스트용 view model입니다.</returns>
    private static ShellViewModel CreateViewModel(IOperatorExecutionStationClient stationClient)
    {
        var options = Options.Create(
            new OperatorExecutionStationClientOptions
            {
                BaseAddress = "http://localhost:51398/",
                DefaultStationId = "ST-0000",
                DefaultActorId = "operator.demo",
                DefaultCompletionQuantityUnit = "EA"
            });
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 17, 8, 0, 0, TimeSpan.Zero));

        return new ShellViewModel(
            stationClient,
            new OperatorExecutionProblemDisplayPolicy(),
            new StationCommandContextFactory(options, timeProvider),
            options,
            timeProvider);
    }

    /// <summary>
    /// 비동기 명령을 실행하고 busy 상태가 해제될 때까지 기다립니다.
    /// </summary>
    /// <param name="command">실행할 비동기 명령입니다.</param>
    /// <param name="viewModel">busy 상태를 확인할 대상 view model입니다.</param>
    /// <returns>대기 작업입니다.</returns>
    private static async Task ExecuteAsyncCommandAndWaitAsync(
        System.Windows.Input.ICommand command,
        ShellViewModel viewModel)
    {
        command.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy);
    }

    /// <summary>
    /// 지정한 조건이 만족될 때까지 짧게 대기합니다.
    /// </summary>
    /// <param name="condition">만족되어야 하는 조건입니다.</param>
    /// <returns>대기 작업입니다.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var startedAt = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - startedAt > TimeSpan.FromSeconds(3))
            {
                throw new TimeoutException("조건이 제한 시간 안에 충족되지 않았습니다.");
            }

            await Task.Delay(20);
        }
    }

    /// <summary>
    /// 테스트용 작업 큐 항목 계약을 생성합니다.
    /// </summary>
    /// <param name="stationId">항목에 대응하는 스테이션 식별자입니다.</param>
    /// <param name="status">항목 상태입니다.</param>
    /// <param name="requiredMaterials">요구 자재 목록입니다.</param>
    /// <returns>테스트용 작업 큐 항목입니다.</returns>
    private static WorkQueueItemContract CreateQueueItem(
        string stationId,
        string status,
        IReadOnlyList<RequiredMaterialContract>? requiredMaterials = null,
        string operationQuantityUnit = "EA")
    {
        return new WorkQueueItemContract(
            "PO-1001",
            "OP-1001",
            10,
            stationId,
            status,
            operationQuantityUnit,
            requiredMaterials ?? [],
            "open");
    }

    /// <summary>
    /// 고정된 시간을 반환하는 테스트용 시간 공급자입니다.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// 시간 공급자를 초기화합니다.
        /// </summary>
        /// <param name="utcNow">반환할 UTC 시각입니다.</param>
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        /// <summary>
        /// 고정된 UTC 시각을 반환합니다.
        /// </summary>
        /// <returns>고정된 UTC 시각입니다.</returns>
        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        /// <summary>
        /// 로컬 시간대를 반환합니다.
        /// </summary>
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    /// <summary>
    /// 테스트용 station client 구현입니다.
    /// </summary>
    private sealed class StubStationClient : IOperatorExecutionStationClient
    {
        /// <summary>
        /// 작업 큐 조회 핸들러를 가져오거나 설정합니다.
        /// </summary>
        public Func<GetStationWorkQueueRequestContract, CancellationToken, Task<OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>>> GetStationWorkQueueAsyncHandler { get; init; } =
            (_, _) => throw new InvalidOperationException("GetStationWorkQueueAsyncHandler가 설정되지 않았습니다.");

        /// <summary>
        /// 시작 명령 핸들러를 가져오거나 설정합니다.
        /// </summary>
        public Func<StartOperationCommandContract, CancellationToken, Task<OperatorExecutionStationClientResult<StartOperationResponseContract>>> StartOperationAsyncHandler { get; init; } =
            (_, _) => throw new InvalidOperationException("StartOperationAsyncHandler가 설정되지 않았습니다.");

        /// <summary>
        /// 자재 투입 명령 핸들러를 가져오거나 설정합니다.
        /// </summary>
        public Func<RecordMaterialScanCommandContract, CancellationToken, Task<OperatorExecutionStationClientResult<RecordMaterialScanResponseContract>>> RecordMaterialScanAsyncHandler { get; init; } =
            (_, _) => throw new InvalidOperationException("RecordMaterialScanAsyncHandler가 설정되지 않았습니다.");

        /// <summary>
        /// ?먯옱 ?ъ엯 紐낅졊 ?몃뱾?щ? 媛?몄삤嫄곕굹 ?ㅼ젙?⑸땲??
        /// </summary>
        public Func<RecordMaterialConsumptionCommandContract, CancellationToken, Task<OperatorExecutionStationClientResult<RecordMaterialConsumptionResponseContract>>> RecordMaterialConsumptionAsyncHandler { get; init; } =
            (_, _) => throw new InvalidOperationException("RecordMaterialConsumptionAsyncHandler가 설정되지 않았습니다.");

        /// <summary>
        /// 완료 명령 핸들러를 가져오거나 설정합니다.
        /// </summary>
        public Func<CompleteOperationCommandContract, CancellationToken, Task<OperatorExecutionStationClientResult<CompleteOperationResponseContract>>> CompleteOperationAsyncHandler { get; init; } =
            (_, _) => throw new InvalidOperationException("CompleteOperationAsyncHandler가 설정되지 않았습니다.");

        /// <summary>
        /// 스테이션 작업 큐를 조회합니다.
        /// </summary>
        /// <param name="request">조회 요청입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>조회 결과입니다.</returns>
        public Task<OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>> GetStationWorkQueueAsync(
            GetStationWorkQueueRequestContract request,
            CancellationToken cancellationToken = default)
        {
            return GetStationWorkQueueAsyncHandler(request, cancellationToken);
        }

        /// <summary>
        /// 공정 시작 명령을 전송합니다.
        /// </summary>
        /// <param name="command">시작 명령입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>시작 명령 결과입니다.</returns>
        public Task<OperatorExecutionStationClientResult<StartOperationResponseContract>> StartOperationAsync(
            StartOperationCommandContract command,
            CancellationToken cancellationToken = default)
        {
            return StartOperationAsyncHandler(command, cancellationToken);
        }

        /// <summary>
        /// 자재 투입 명령을 전송합니다.
        /// </summary>
        /// <param name="command">자재 투입 명령입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>자재 투입 명령 결과입니다.</returns>
        public Task<OperatorExecutionStationClientResult<RecordMaterialScanResponseContract>> RecordMaterialScanAsync(
            RecordMaterialScanCommandContract command,
            CancellationToken cancellationToken = default)
        {
            return RecordMaterialScanAsyncHandler(command, cancellationToken);
        }

        /// <summary>
        /// ?먯옱 ?ъ엯 紐낅졊???꾩넚?⑸땲??
        /// </summary>
        /// <param name="command">?먯옱 ?ъ엯 紐낅졊?낅땲??</param>
        /// <param name="cancellationToken">痍⑥냼 ?좏겙?낅땲??</param>
        /// <returns>?먯옱 ?ъ엯 紐낅졊 寃곌낵?낅땲??</returns>
        public Task<OperatorExecutionStationClientResult<RecordMaterialConsumptionResponseContract>> RecordMaterialConsumptionAsync(
            RecordMaterialConsumptionCommandContract command,
            CancellationToken cancellationToken = default)
        {
            return RecordMaterialConsumptionAsyncHandler(command, cancellationToken);
        }

        /// <summary>
        /// 공정 완료 명령을 전송합니다.
        /// </summary>
        /// <param name="command">완료 명령입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>완료 명령 결과입니다.</returns>
        public Task<OperatorExecutionStationClientResult<CompleteOperationResponseContract>> CompleteOperationAsync(
            CompleteOperationCommandContract command,
            CancellationToken cancellationToken = default)
        {
            return CompleteOperationAsyncHandler(command, cancellationToken);
        }
    }
}
