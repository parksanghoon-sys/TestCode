using System.Collections.ObjectModel;
using System.Globalization;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Client.Wpf.Configuration;
using Mes.Client.Wpf.OperatorExecution;
using Mes.Client.Wpf.Support;
using Microsoft.Extensions.Options;

namespace Mes.Client.Wpf.Shell;

/// <summary>
/// 스테이션 셸의 바인딩 상태, 작업 큐 조회, 시작, 자재 투입, 완료 명령 흐름을 관리합니다.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly IOperatorExecutionStationClient _stationClient;
    private readonly OperatorExecutionProblemDisplayPolicy _problemDisplayPolicy;
    private readonly StationCommandContextFactory _commandContextFactory;
    private readonly TimeProvider _timeProvider;
    private readonly string _defaultCompletionQuantityUnit;
    private string _stationId;
    private bool _isBusy;
    private string _messageTitle = "준비 완료";
    private string _messageDetail = "스테이션을 바인딩한 뒤 작업 대기열을 불러오세요.";
    private string _messageSeverity = "info";
    private string _completionGoodQuantityText = string.Empty;
    private string _completionScrapQuantityText = string.Empty;
    private string _completionQuantityUnit;
    private string _materialConsumptionWipUnitId = string.Empty;
    private string _materialConsumptionMaterialLotId = string.Empty;
    private string _materialConsumptionMaterialCode = string.Empty;
    private string _materialConsumptionQuantityText = string.Empty;
    private string _materialConsumptionQuantityUnit = string.Empty;
    private string? _materialConsumptionActionToken;
    private StationSessionState? _boundSession;
    private WorkQueueItemViewModel? _selectedQueueItem;
    private DateTimeOffset? _snapshotTakenAt;
    private string? _snapshotStationId;

    /// <summary>
    /// 스테이션 셸 view model을 초기화합니다.
    /// </summary>
    /// <param name="stationClient">작업 큐 조회 및 명령 전송 클라이언트입니다.</param>
    /// <param name="problemDisplayPolicy">실패를 작업자 메시지로 바꾸는 정책입니다.</param>
    /// <param name="commandContextFactory">WPF 명령 공통 context 생성기입니다.</param>
    /// <param name="options">셸 구성 옵션입니다.</param>
    /// <param name="timeProvider">현재 시간 공급자입니다.</param>
    public ShellViewModel(
        IOperatorExecutionStationClient stationClient,
        OperatorExecutionProblemDisplayPolicy problemDisplayPolicy,
        StationCommandContextFactory commandContextFactory,
        IOptions<OperatorExecutionStationClientOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(stationClient);
        ArgumentNullException.ThrowIfNull(problemDisplayPolicy);
        ArgumentNullException.ThrowIfNull(commandContextFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _stationClient = stationClient;
        _problemDisplayPolicy = problemDisplayPolicy;
        _commandContextFactory = commandContextFactory;
        _timeProvider = timeProvider;

        var resolvedOptions = options.Value;
        ApiBaseAddress = resolvedOptions.ResolveBaseUri().ToString();
        _stationId = resolvedOptions.DefaultStationId?.Trim() ?? string.Empty;
        _defaultCompletionQuantityUnit = resolvedOptions.ResolveDefaultCompletionQuantityUnit();
        _completionQuantityUnit = _defaultCompletionQuantityUnit;

        QueueItems = [];
        BindStationCommand = new DelegateCommand(BindStation, CanBindStation);
        RefreshQueueCommand = new AsyncDelegateCommand(
            RefreshQueueAsync,
            CanRefreshQueue,
            HandleUnexpectedCommandFailure);
        StartOperationCommand = new AsyncDelegateCommand(
            StartOperationAsync,
            CanStartOperation,
            HandleUnexpectedCommandFailure);
        RecordMaterialConsumptionCommand = new AsyncDelegateCommand(
            RecordMaterialConsumptionAsync,
            CanRecordMaterialConsumption,
            HandleUnexpectedCommandFailure);
        CompleteOperationCommand = new AsyncDelegateCommand(
            CompleteOperationAsync,
            CanCompleteOperation,
            HandleUnexpectedCommandFailure);
    }

    /// <summary>
    /// 현재 연결 대상 API 기본 주소를 가져옵니다.
    /// </summary>
    public string ApiBaseAddress { get; }

    /// <summary>
    /// 작업자가 입력하는 현재 스테이션 식별자를 가져오거나 설정합니다.
    /// </summary>
    public string StationId
    {
        get => _stationId;
        set
        {
            if (SetProperty(ref _stationId, value))
            {
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 현재 작업 큐 컬렉션을 가져옵니다.
    /// </summary>
    public ObservableCollection<WorkQueueItemViewModel> QueueItems { get; }

    /// <summary>
    /// 현재 명령 대상으로 선택된 작업 큐 항목을 가져오거나 설정합니다.
    /// </summary>
    public WorkQueueItemViewModel? SelectedQueueItem
    {
        get => _selectedQueueItem;
        set
        {
            if (SetProperty(ref _selectedQueueItem, value))
            {
                ApplySelectedQueueItemDefaults(value);
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 현재 입력한 스테이션으로 세션을 바인딩하는 명령을 가져옵니다.
    /// </summary>
    public DelegateCommand BindStationCommand { get; }

    /// <summary>
    /// 바인딩된 스테이션의 작업 큐를 새로고침하는 명령을 가져옵니다.
    /// </summary>
    public AsyncDelegateCommand RefreshQueueCommand { get; }

    /// <summary>
    /// 선택된 작업을 시작하는 명령을 가져옵니다.
    /// </summary>
    public AsyncDelegateCommand StartOperationCommand { get; }

    /// <summary>
    /// 선택된 작업에 자재 투입을 기록하는 명령을 가져옵니다.
    /// </summary>
    public AsyncDelegateCommand RecordMaterialConsumptionCommand { get; }

    /// <summary>
    /// 선택된 작업을 완료하는 명령을 가져옵니다.
    /// </summary>
    public AsyncDelegateCommand CompleteOperationCommand { get; }

    /// <summary>
    /// 현재 셸이 통신 또는 명령 처리 중인지 여부를 반환합니다.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 작업자 메시지 제목을 가져오거나 설정합니다.
    /// </summary>
    public string MessageTitle
    {
        get => _messageTitle;
        private set => SetProperty(ref _messageTitle, value);
    }

    /// <summary>
    /// 작업자 메시지 상세 설명을 가져오거나 설정합니다.
    /// </summary>
    public string MessageDetail
    {
        get => _messageDetail;
        private set => SetProperty(ref _messageDetail, value);
    }

    /// <summary>
    /// 완료 처리에 사용할 양품 수량 입력값을 가져오거나 설정합니다.
    /// </summary>
    public string CompletionGoodQuantityText
    {
        get => _completionGoodQuantityText;
        set
        {
            if (SetProperty(ref _completionGoodQuantityText, value))
            {
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 완료 처리에 사용할 불량 수량 입력값을 가져오거나 설정합니다.
    /// </summary>
    public string CompletionScrapQuantityText
    {
        get => _completionScrapQuantityText;
        set
        {
            if (SetProperty(ref _completionScrapQuantityText, value))
            {
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 선택된 공정에서 투영된 완료 수량 단위를 가져옵니다.
    /// </summary>
    public string CompletionQuantityUnit
    {
        get => _completionQuantityUnit;
        private set
        {
            if (SetProperty(ref _completionQuantityUnit, value))
            {
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 자재 투입에 사용할 WIP 식별자를 가져오거나 설정합니다.
    /// </summary>
    public string MaterialConsumptionWipUnitId
    {
        get => _materialConsumptionWipUnitId;
        set
        {
            if (SetProperty(ref _materialConsumptionWipUnitId, value))
            {
                ResetMaterialConsumptionActionToken();
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 자재 투입에 사용할 자재 lot 식별자를 가져오거나 설정합니다.
    /// </summary>
    public string MaterialConsumptionMaterialLotId
    {
        get => _materialConsumptionMaterialLotId;
        set
        {
            if (SetProperty(ref _materialConsumptionMaterialLotId, value))
            {
                ResetMaterialConsumptionActionToken();
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 자재 투입에 사용할 자재 코드를 가져오거나 설정합니다.
    /// </summary>
    public string MaterialConsumptionMaterialCode
    {
        get => _materialConsumptionMaterialCode;
        set
        {
            if (SetProperty(ref _materialConsumptionMaterialCode, value))
            {
                ResetMaterialConsumptionActionToken();
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 자재 투입 수량 입력값을 가져오거나 설정합니다.
    /// </summary>
    public string MaterialConsumptionQuantityText
    {
        get => _materialConsumptionQuantityText;
        set
        {
            if (SetProperty(ref _materialConsumptionQuantityText, value))
            {
                ResetMaterialConsumptionActionToken();
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 자재 투입 수량 단위를 가져오거나 설정합니다.
    /// </summary>
    public string MaterialConsumptionQuantityUnit
    {
        get => _materialConsumptionQuantityUnit;
        set
        {
            if (SetProperty(ref _materialConsumptionQuantityUnit, value))
            {
                ResetMaterialConsumptionActionToken();
                RaiseDerivedStateChanged();
            }
        }
    }

    /// <summary>
    /// 현재 작업자 메시지 severity를 가져옵니다.
    /// </summary>
    public string MessageSeverity
    {
        get => _messageSeverity;
        private set
        {
            if (SetProperty(ref _messageSeverity, value))
            {
                RaiseMessageAppearanceChanged();
            }
        }
    }

    /// <summary>
    /// 현재 세션 상태를 요약한 캡션을 반환합니다.
    /// </summary>
    public string SessionCaption => _boundSession is null
        ? "바인딩된 스테이션 없음"
        : $"바인딩: {_boundSession.StationId} ({_boundSession.BoundAt:yyyy-MM-dd HH:mm:ss})";

    /// <summary>
    /// 마지막 스냅샷 시각을 요약한 캡션을 반환합니다.
    /// </summary>
    public string QueueSnapshotCaption => _snapshotTakenAt is null
        ? "작업 큐 미조회"
        : $"{_snapshotStationId ?? "알 수 없는 스테이션"} 기준 스냅샷 {_snapshotTakenAt:yyyy-MM-dd HH:mm:ss}";

    /// <summary>
    /// 현재 작업 큐 건수를 요약한 캡션을 반환합니다.
    /// </summary>
    public string QueueCountCaption => $"대기 항목 {QueueItems.Count}건";

    /// <summary>
    /// 통신 또는 명령 상태를 요약한 캡션을 반환합니다.
    /// </summary>
    public string BusyCaption => IsBusy ? "처리 중" : "대기 중";

    /// <summary>
    /// 현재 선택된 작업 요약을 반환합니다.
    /// </summary>
    public string SelectedOperationCaption => SelectedQueueItem is null
        ? "선택된 작업 없음"
        : $"{SelectedQueueItem.ProductionOrderId} / {SelectedQueueItem.OperationExecutionId}";

    /// <summary>
    /// 선택된 작업의 상태 요약을 반환합니다.
    /// </summary>
    public string SelectedOperationDetail => SelectedQueueItem is null
        ? "작업 큐에서 시작, 자재 투입, 완료를 진행할 작업을 선택하세요."
        : $"{SelectedQueueItem.StationId} · Seq {SelectedQueueItem.OperationSequence} · {SelectedQueueItem.Status} · Unit {SelectedQueueItem.OperationQuantityUnit} · Quality {SelectedQueueItem.QualityGateState}";

    /// <summary>
    /// 선택된 작업의 요구 자재 요약을 반환합니다.
    /// </summary>
    public string SelectedRequiredMaterialsCaption => SelectedQueueItem is null
        ? "요구 자재를 보려면 작업을 선택하세요."
        : $"요구 자재: {SelectedQueueItem.RequiredMaterialsSummary}";

    /// <summary>
    /// 명령 패널 상단에서 다음 동작을 안내하는 문구를 반환합니다.
    /// </summary>
    public string CommandPanelHint
    {
        get
        {
            if (SelectedQueueItem is null)
            {
                return "목록에서 명령 대상 공정을 선택하세요.";
            }

            if (SelectedQueueItem.CanStartOperation)
            {
                return "선택된 공정은 시작 가능합니다. 시작 후 같은 BFF 경로로 자재 투입과 완료를 이어서 기록합니다.";
            }

            if (SelectedQueueItem.CanRecordMaterialConsumption || SelectedQueueItem.CanCompleteOperation)
            {
                return "선택된 공정은 자재 투입과 완료가 가능합니다. 아래 입력 영역에서 필요한 수량과 lot를 기록하세요.";
            }

            return "현재 상태에서는 서버가 명령을 거절할 수 있습니다. 필요하면 작업 큐를 새로고침하세요.";
        }
    }

    /// <summary>
    /// 자재 투입 영역에서 필요한 다음 동작을 안내하는 문구를 반환합니다.
    /// </summary>
    public string MaterialConsumptionHint
    {
        get
        {
            if (SelectedQueueItem is null)
            {
                return "작업을 선택하면 요구 자재에서 기본 자재 코드와 단위를 채웁니다.";
            }

            if (!SelectedQueueItem.CanRecordMaterialConsumption)
            {
                return "현재 상태에서는 자재 투입을 기록할 수 없습니다. 공정을 먼저 시작하거나 상태를 확인하세요.";
            }

            if (SelectedQueueItem.HasRequiredMaterials)
            {
                return "요구 자재의 첫 항목을 기본 자재 코드와 단위로 채웠습니다. WIP, lot, 수량만 확인해도 됩니다.";
            }

            return "요구 자재가 비어 있어 자재 코드와 단위를 직접 입력해야 합니다.";
        }
    }

    /// <summary>
    /// 메시지 패널 배경색을 반환합니다.
    /// </summary>
    public string MessagePanelBackground => MessageSeverity switch
    {
        "error" => "#FFFDECEC",
        "warning" => "#FFFDF6E5",
        _ => "#FFEAF4FF"
    };

    /// <summary>
    /// 메시지 패널 테두리 색을 반환합니다.
    /// </summary>
    public string MessagePanelBorderBrush => MessageSeverity switch
    {
        "error" => "#FFE6A7A7",
        "warning" => "#FFE5CE8C",
        _ => "#FF9CC5F3"
    };

    /// <summary>
    /// 메시지 제목 글자색을 반환합니다.
    /// </summary>
    public string MessageTitleBrush => MessageSeverity switch
    {
        "error" => "#FFA12626",
        "warning" => "#FF7A5200",
        _ => "#FF0F4C81"
    };

    /// <summary>
    /// 메시지 본문 글자색을 반환합니다.
    /// </summary>
    public string MessageDetailBrush => MessageSeverity switch
    {
        "error" => "#FF8B3A3A",
        "warning" => "#FF805B10",
        _ => "#FF2D5F8B"
    };

    /// <summary>
    /// 현재 입력값으로 바인딩 명령을 실행할 수 있는지 판단합니다.
    /// </summary>
    /// <returns>바인딩이 가능하면 <see langword="true"/>를 반환합니다.</returns>
    private bool CanBindStation()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(StationId);
    }

    /// <summary>
    /// 현재 상태에서 작업 큐 새로고침 명령을 실행할 수 있는지 판단합니다.
    /// </summary>
    /// <returns>새로고침이 가능하면 <see langword="true"/>를 반환합니다.</returns>
    private bool CanRefreshQueue()
    {
        return !IsBusy && _boundSession is not null;
    }

    /// <summary>
    /// 현재 상태에서 시작 명령을 실행할 수 있는지 판단합니다.
    /// </summary>
    /// <returns>시작 명령 실행이 가능하면 <see langword="true"/>를 반환합니다.</returns>
    private bool CanStartOperation()
    {
        return !IsBusy
            && _boundSession is not null
            && SelectedQueueItem?.CanStartOperation == true;
    }

    /// <summary>
    /// 현재 상태에서 자재 투입 명령을 실행할 수 있는지 판단합니다.
    /// </summary>
    /// <returns>자재 투입 명령 실행이 가능하면 <see langword="true"/>를 반환합니다.</returns>
    private bool CanRecordMaterialConsumption()
    {
        return !IsBusy
            && _boundSession is not null
            && SelectedQueueItem?.CanRecordMaterialConsumption == true
            && TryCreateMaterialConsumptionPayload(out _);
    }

    /// <summary>
    /// 현재 상태에서 완료 명령을 실행할 수 있는지 판단합니다.
    /// </summary>
    /// <returns>완료 명령 실행이 가능하면 <see langword="true"/>를 반환합니다.</returns>
    private bool CanCompleteOperation()
    {
        return !IsBusy
            && _boundSession is not null
            && SelectedQueueItem?.CanCompleteOperation == true
            && TryParseCompletionQuantities(out _, out _);
    }

    /// <summary>
    /// 현재 입력한 스테이션으로 세션을 바인딩합니다.
    /// </summary>
    private void BindStation()
    {
        _boundSession = StationSessionState.Bind(StationId, _timeProvider.GetLocalNow());
        ClearQueueSnapshot();
        StationId = _boundSession.StationId;
        ApplyUserMessage(
            new StationUserMessage(
                "스테이션 바인딩 완료",
                $"{_boundSession.StationId} 기준으로 작업 큐를 조회할 준비가 됐습니다.",
                "info"));
        RaiseDerivedStateChanged();
    }

    /// <summary>
    /// 바인딩된 스테이션의 작업 큐를 조회합니다.
    /// </summary>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>비동기 조회 작업입니다.</returns>
    private async Task RefreshQueueAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            var refreshFailure = await TryRefreshQueueSnapshotAsync(cancellationToken);
            if (refreshFailure is null)
            {
                ApplyUserMessage(
                    new StationUserMessage(
                        "작업 큐 갱신 완료",
                        $"{_boundSession!.StationId} 작업 대기열을 최신 상태로 불러왔습니다.",
                        "info"));
            }
            else
            {
                ApplyUserMessage(refreshFailure);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 선택된 작업에 대한 시작 명령을 전송합니다.
    /// </summary>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>명령 처리 작업입니다.</returns>
    private async Task StartOperationAsync(CancellationToken cancellationToken)
    {
        if (_boundSession is null)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "스테이션 미바인딩",
                    "먼저 스테이션을 바인딩한 뒤 시작 명령을 보내세요.",
                    "warning"));
            return;
        }

        if (SelectedQueueItem is null)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "작업 미선택",
                    "시작할 작업을 작업 큐에서 먼저 선택하세요.",
                    "warning"));
            return;
        }

        if (!SelectedQueueItem.CanStartOperation)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "시작 불가 상태",
                    "선택된 작업은 현재 상태에서 시작 명령을 받을 수 없습니다.",
                    "warning"));
            return;
        }

        IsBusy = true;

        try
        {
            var command = new StartOperationCommandContract(
                _commandContextFactory.CreateForOperation(
                    OperatorExecutionCommandTypes.StartOperation,
                    _boundSession.StationId,
                    SelectedQueueItem.OperationExecutionId),
                new StartOperationPayloadContract(
                    SelectedQueueItem.ProductionOrderId,
                    SelectedQueueItem.OperationExecutionId,
                    SelectedQueueItem.OperationSequence,
                    null));

            var response = await _stationClient.StartOperationAsync(command, cancellationToken);
            if (!response.IsSuccess || response.Value is null)
            {
                ApplyUserMessage(_problemDisplayPolicy.Map(response.Failure ?? CreateUnknownClientFailure()));
                return;
            }

            var successMessage = new StationUserMessage(
                "작업 시작 접수 완료",
                $"{response.Value.OperationExecutionId}이(가) {response.Value.StartedAt:yyyy-MM-dd HH:mm:ss}에 {response.Value.Status} 상태로 기록됐습니다.",
                "info");
            await ApplyAcceptedCommandResultAsync(successMessage, SelectedQueueItem.OperationExecutionId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 선택된 작업에 대한 자재 투입 명령을 전송합니다.
    /// </summary>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>명령 처리 작업입니다.</returns>
    private async Task RecordMaterialConsumptionAsync(CancellationToken cancellationToken)
    {
        if (_boundSession is null)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "스테이션 미바인딩",
                    "먼저 스테이션을 바인딩한 뒤 자재 투입을 기록하세요.",
                    "warning"));
            return;
        }

        if (SelectedQueueItem is null)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "작업 미선택",
                    "자재를 투입할 작업을 작업 큐에서 먼저 선택하세요.",
                    "warning"));
            return;
        }

        if (!SelectedQueueItem.CanRecordMaterialConsumption)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "자재 투입 불가 상태",
                    "선택된 작업은 현재 상태에서 자재 투입 명령을 받을 수 없습니다.",
                    "warning"));
            return;
        }

        if (!TryCreateMaterialConsumptionPayload(out var payload))
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "자재 투입 입력 확인 필요",
                    "WIP, 자재 lot, 자재 코드, 수량, 단위를 모두 채우고 수량은 0보다 크게 입력하세요.",
                    "warning"));
            return;
        }

        IsBusy = true;

        try
        {
            var command = new RecordMaterialConsumptionCommandContract(
                _commandContextFactory.CreateForOperation(
                    OperatorExecutionCommandTypes.RecordMaterialConsumption,
                    _boundSession.StationId,
                    SelectedQueueItem.OperationExecutionId,
                    GetMaterialConsumptionActionToken()),
                payload);

            var response = await _stationClient.RecordMaterialConsumptionAsync(command, cancellationToken);
            if (!response.IsSuccess || response.Value is null)
            {
                ApplyUserMessage(_problemDisplayPolicy.Map(response.Failure ?? CreateUnknownClientFailure()));
                return;
            }

            var genealogyCaption = response.Value.GenealogyLinkCreated ? "생성" : "유지";
            var successMessage = new StationUserMessage(
                "자재 투입 접수 완료",
                $"{response.Value.MaterialLotId} lot의 잔량이 {response.Value.RemainingQuantity.Value:0.###} {response.Value.RemainingQuantity.Unit}로 갱신됐습니다. genealogy link는 {genealogyCaption} 상태입니다.",
                "info");
            await ApplyAcceptedCommandResultAsync(successMessage, SelectedQueueItem.OperationExecutionId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 선택된 작업에 대한 완료 명령을 전송합니다.
    /// </summary>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>명령 처리 작업입니다.</returns>
    private async Task CompleteOperationAsync(CancellationToken cancellationToken)
    {
        if (_boundSession is null)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "스테이션 미바인딩",
                    "먼저 스테이션을 바인딩한 뒤 완료 명령을 보내세요.",
                    "warning"));
            return;
        }

        if (SelectedQueueItem is null)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "작업 미선택",
                    "완료할 작업을 작업 큐에서 먼저 선택하세요.",
                    "warning"));
            return;
        }

        if (!SelectedQueueItem.CanCompleteOperation)
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "완료 불가 상태",
                    "선택된 작업은 현재 상태에서 완료 명령을 받을 수 없습니다.",
                    "warning"));
            return;
        }

        if (!TryParseCompletionQuantities(out var goodQuantity, out var scrapQuantity))
        {
            ApplyUserMessage(
                new StationUserMessage(
                    "완료 수량 확인 필요",
                    "양품 수량은 0보다 커야 하고, 불량 수량은 비워 두거나 0 이상으로 입력해야 합니다. 수량 단위도 함께 확인하세요.",
                    "warning"));
            return;
        }

        IsBusy = true;

        try
        {
            var command = new CompleteOperationCommandContract(
                _commandContextFactory.CreateForOperation(
                    OperatorExecutionCommandTypes.CompleteOperation,
                    _boundSession.StationId,
                    SelectedQueueItem.OperationExecutionId),
                new CompleteOperationPayloadContract(
                    SelectedQueueItem.OperationExecutionId,
                    goodQuantity,
                    scrapQuantity,
                    CompletionModeValues.Manual));

            var response = await _stationClient.CompleteOperationAsync(command, cancellationToken);
            if (!response.IsSuccess || response.Value is null)
            {
                ApplyUserMessage(_problemDisplayPolicy.Map(response.Failure ?? CreateUnknownClientFailure()));
                return;
            }

            var successMessage = new StationUserMessage(
                "작업 완료 접수 완료",
                $"{response.Value.OperationExecutionId}이(가) {response.Value.CompletedAt:yyyy-MM-dd HH:mm:ss}에 {response.Value.Status} 상태로 완료됐습니다. 생산실적 상태는 {response.Value.ProductionActualsStatus}입니다.",
                "info");
            await ApplyAcceptedCommandResultAsync(successMessage, SelectedQueueItem.OperationExecutionId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 응답 작업 큐 스냅샷을 현재 UI 컬렉션에 반영합니다.
    /// </summary>
    /// <param name="response">적용할 작업 큐 응답입니다.</param>
    /// <param name="preferredOperationExecutionId">가능하면 다시 선택할 공정 실행 식별자입니다.</param>
    private void ApplyQueueSnapshot(
        GetStationWorkQueueResponseContract response,
        string? preferredOperationExecutionId = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        var selectedOperationExecutionId = preferredOperationExecutionId ?? SelectedQueueItem?.OperationExecutionId;

        QueueItems.Clear();
        foreach (var item in response.Items)
        {
            QueueItems.Add(new WorkQueueItemViewModel(item));
        }

        SelectedQueueItem = string.IsNullOrWhiteSpace(selectedOperationExecutionId)
            ? null
            : QueueItems.FirstOrDefault(
                item => string.Equals(
                    item.OperationExecutionId,
                    selectedOperationExecutionId,
                    StringComparison.Ordinal));
        _snapshotTakenAt = response.SnapshotTakenAt;
        _snapshotStationId = response.StationId;
        RaiseDerivedStateChanged();
    }

    /// <summary>
    /// 현재 표시 중인 작업 큐 스냅샷을 초기화합니다.
    /// </summary>
    private void ClearQueueSnapshot()
    {
        QueueItems.Clear();
        SelectedQueueItem = null;
        _snapshotTakenAt = null;
        _snapshotStationId = null;
        RaiseDerivedStateChanged();
    }

    /// <summary>
    /// 현재 바인딩된 스테이션의 작업 큐 스냅샷을 다시 읽어 적용합니다.
    /// </summary>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <param name="preferredOperationExecutionId">성공 시 다시 선택할 공정 실행 식별자입니다.</param>
    /// <returns>실패 시 작업자 메시지, 성공 시 <see langword="null"/>입니다.</returns>
    private async Task<StationUserMessage?> TryRefreshQueueSnapshotAsync(
        CancellationToken cancellationToken,
        string? preferredOperationExecutionId = null)
    {
        if (_boundSession is null)
        {
            return new StationUserMessage(
                "스테이션 미바인딩",
                "먼저 스테이션을 바인딩한 뒤 작업 대기열을 조회하세요.",
                "warning");
        }

        var response = await _stationClient.GetStationWorkQueueAsync(
            new GetStationWorkQueueRequestContract(_boundSession.StationId),
            cancellationToken);

        if (response.IsSuccess && response.Value is not null)
        {
            ApplyQueueSnapshot(response.Value, preferredOperationExecutionId);
            return null;
        }

        return _problemDisplayPolicy.Map(response.Failure ?? CreateUnknownClientFailure());
    }

    /// <summary>
    /// 수락된 명령 이후 작업 큐를 안전하게 갱신하고 결과 메시지를 반영합니다.
    /// </summary>
    /// <param name="successMessage">명령 자체의 성공 메시지입니다.</param>
    /// <param name="preferredOperationExecutionId">새 스냅샷에서 다시 선택할 공정 실행 식별자입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>후속 갱신 작업입니다.</returns>
    private async Task ApplyAcceptedCommandResultAsync(
        StationUserMessage successMessage,
        string preferredOperationExecutionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(successMessage);

        ClearQueueSnapshot();
        var refreshFailure = await TryRefreshQueueSnapshotAsync(cancellationToken, preferredOperationExecutionId);
        if (refreshFailure is null)
        {
            ApplyUserMessage(successMessage);
            return;
        }

        ApplyUserMessage(
            new StationUserMessage(
                successMessage.Title,
                $"{successMessage.Detail} 하지만 후속 작업 큐 새로고침에 실패했습니다. {refreshFailure.Detail}",
                refreshFailure.Severity));
    }

    /// <summary>
    /// 작업자 메시지를 현재 화면 상태에 반영합니다.
    /// </summary>
    /// <param name="message">반영할 작업자 메시지입니다.</param>
    private void ApplyUserMessage(StationUserMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        MessageTitle = message.Title;
        MessageDetail = message.Detail;
        MessageSeverity = message.Severity;
    }

    /// <summary>
    /// 선택된 작업에 맞는 자재 투입 입력 기본값을 채웁니다.
    /// </summary>
    /// <param name="selectedItem">새로 선택된 작업 항목입니다.</param>
    private void ApplySelectedQueueItemDefaults(WorkQueueItemViewModel? selectedItem)
    {
        ResetMaterialConsumptionActionToken();
        CompletionQuantityUnit = selectedItem?.OperationQuantityUnit ?? _defaultCompletionQuantityUnit;
        SetMaterialConsumptionField(ref _materialConsumptionWipUnitId, string.Empty, nameof(MaterialConsumptionWipUnitId));
        SetMaterialConsumptionField(ref _materialConsumptionMaterialLotId, string.Empty, nameof(MaterialConsumptionMaterialLotId));
        SetMaterialConsumptionField(ref _materialConsumptionQuantityText, string.Empty, nameof(MaterialConsumptionQuantityText));
        SetMaterialConsumptionField(
            ref _materialConsumptionMaterialCode,
            selectedItem?.PreferredMaterialCode ?? string.Empty,
            nameof(MaterialConsumptionMaterialCode));
        SetMaterialConsumptionField(
            ref _materialConsumptionQuantityUnit,
            selectedItem?.PreferredMaterialQuantityUnit ?? string.Empty,
            nameof(MaterialConsumptionQuantityUnit));
    }

    /// <summary>
    /// 자재 투입 입력 필드를 직접 갱신하고 필요한 변경 알림을 발생시킵니다.
    /// </summary>
    /// <param name="storage">갱신할 backing field입니다.</param>
    /// <param name="value">설정할 값입니다.</param>
    /// <param name="propertyName">알림을 발생시킬 속성 이름입니다.</param>
    private void SetMaterialConsumptionField(ref string storage, string value, string propertyName)
    {
        if (string.Equals(storage, value, StringComparison.Ordinal))
        {
            return;
        }

        storage = value;
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// 자재 투입 명령에 사용할 action token을 반환합니다.
    /// </summary>
    /// <returns>현재 입력 조합에 대응하는 action token입니다.</returns>
    private string GetMaterialConsumptionActionToken()
    {
        _materialConsumptionActionToken ??= Guid.NewGuid().ToString("N");
        return _materialConsumptionActionToken;
    }

    /// <summary>
    /// 자재 투입 입력이 바뀌면 기존 action token을 폐기합니다.
    /// </summary>
    private void ResetMaterialConsumptionActionToken()
    {
        _materialConsumptionActionToken = null;
    }

    /// <summary>
    /// async command 바깥으로 전파된 예외를 작업자 메시지로 변환합니다.
    /// </summary>
    /// <param name="exception">처리 중 예외입니다.</param>
    private void HandleUnexpectedCommandFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        ApplyUserMessage(
            new StationUserMessage(
                "클라이언트 오류",
                $"스테이션 클라이언트에서 예상하지 못한 오류가 발생했습니다: {exception.Message}",
                "error"));
    }

    /// <summary>
    /// 추가 실패 정보가 없을 때 사용할 기본 클라이언트 실패를 생성합니다.
    /// </summary>
    /// <returns>기본 클라이언트 실패 정보입니다.</returns>
    private static OperatorExecutionStationClientFailure CreateUnknownClientFailure()
    {
        return new OperatorExecutionStationClientFailure(
            null,
            null,
            "client.unknown_failure",
            "예기치 못한 통신 오류가 발생했습니다.");
    }

    /// <summary>
    /// 메시지 severity 변화에 맞춰 파생 시각 속성을 다시 알립니다.
    /// </summary>
    private void RaiseMessageAppearanceChanged()
    {
        OnPropertyChanged(nameof(MessagePanelBackground));
        OnPropertyChanged(nameof(MessagePanelBorderBrush));
        OnPropertyChanged(nameof(MessageTitleBrush));
        OnPropertyChanged(nameof(MessageDetailBrush));
    }

    /// <summary>
    /// 파생 상태와 명령 활성 여부를 함께 갱신합니다.
    /// </summary>
    private void RaiseDerivedStateChanged()
    {
        OnPropertyChanged(nameof(SessionCaption));
        OnPropertyChanged(nameof(QueueSnapshotCaption));
        OnPropertyChanged(nameof(QueueCountCaption));
        OnPropertyChanged(nameof(BusyCaption));
        OnPropertyChanged(nameof(SelectedOperationCaption));
        OnPropertyChanged(nameof(SelectedOperationDetail));
        OnPropertyChanged(nameof(SelectedRequiredMaterialsCaption));
        OnPropertyChanged(nameof(CommandPanelHint));
        OnPropertyChanged(nameof(MaterialConsumptionHint));
        BindStationCommand.RaiseCanExecuteChanged();
        RefreshQueueCommand.RaiseCanExecuteChanged();
        StartOperationCommand.RaiseCanExecuteChanged();
        RecordMaterialConsumptionCommand.RaiseCanExecuteChanged();
        CompleteOperationCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 자재 투입 입력을 transport 계약으로 변환할 수 있는지 검증합니다.
    /// </summary>
    /// <param name="payload">성공 시 생성된 자재 투입 payload입니다.</param>
    /// <returns>입력이 유효하면 <see langword="true"/>를 반환합니다.</returns>
    private bool TryCreateMaterialConsumptionPayload(out RecordMaterialConsumptionPayloadContract payload)
    {
        payload = default!;

        if (SelectedQueueItem is null)
        {
            return false;
        }

        var wipUnitId = MaterialConsumptionWipUnitId?.Trim();
        var materialLotId = MaterialConsumptionMaterialLotId?.Trim();
        var materialCode = MaterialConsumptionMaterialCode?.Trim();
        var quantityUnit = MaterialConsumptionQuantityUnit?.Trim();

        if (string.IsNullOrWhiteSpace(wipUnitId)
            || string.IsNullOrWhiteSpace(materialLotId)
            || string.IsNullOrWhiteSpace(materialCode)
            || string.IsNullOrWhiteSpace(quantityUnit)
            || !TryParsePositiveDecimal(MaterialConsumptionQuantityText, out var quantityValue))
        {
            return false;
        }

        payload = new RecordMaterialConsumptionPayloadContract(
            SelectedQueueItem.OperationExecutionId,
            wipUnitId,
            materialLotId,
            materialCode,
            new MeasuredQuantityContract(quantityValue, quantityUnit));
        return true;
    }

    /// <summary>
    /// 완료 수량 입력을 transport 계약으로 변환할 수 있는지 검증합니다.
    /// </summary>
    /// <param name="goodQuantity">성공 시 생성된 양품 수량입니다.</param>
    /// <param name="scrapQuantity">성공 시 생성된 불량 수량입니다.</param>
    /// <returns>완료 입력이 유효하면 <see langword="true"/>를 반환합니다.</returns>
    private bool TryParseCompletionQuantities(
        out MeasuredQuantityContract goodQuantity,
        out MeasuredQuantityContract? scrapQuantity)
    {
        goodQuantity = default!;
        scrapQuantity = null;

        var unit = CompletionQuantityUnit?.Trim();
        if (string.IsNullOrWhiteSpace(unit))
        {
            return false;
        }

        if (!TryParsePositiveDecimal(CompletionGoodQuantityText, out var goodValue))
        {
            return false;
        }

        goodQuantity = new MeasuredQuantityContract(goodValue, unit);

        if (string.IsNullOrWhiteSpace(CompletionScrapQuantityText))
        {
            return true;
        }

        if (!TryParseNonNegativeDecimal(CompletionScrapQuantityText, out var scrapValue))
        {
            return false;
        }

        scrapQuantity = new MeasuredQuantityContract(scrapValue, unit);
        return true;
    }

    /// <summary>
    /// 양수 수량 문자열을 파싱합니다.
    /// </summary>
    /// <param name="text">파싱할 입력 문자열입니다.</param>
    /// <param name="value">성공 시 파싱된 수량입니다.</param>
    /// <returns>파싱에 성공하면 <see langword="true"/>를 반환합니다.</returns>
    private static bool TryParsePositiveDecimal(string? text, out decimal value)
    {
        return TryParseDecimal(text, out value) && value > 0m;
    }

    /// <summary>
    /// 0 이상 수량 문자열을 파싱합니다.
    /// </summary>
    /// <param name="text">파싱할 입력 문자열입니다.</param>
    /// <param name="value">성공 시 파싱된 수량입니다.</param>
    /// <returns>파싱에 성공하면 <see langword="true"/>를 반환합니다.</returns>
    private static bool TryParseNonNegativeDecimal(string? text, out decimal value)
    {
        return TryParseDecimal(text, out value) && value >= 0m;
    }

    /// <summary>
    /// 현재 문화권과 invariant 문화권을 순서대로 사용해 수량 문자열을 파싱합니다.
    /// </summary>
    /// <param name="text">파싱할 입력 문자열입니다.</param>
    /// <param name="value">성공 시 파싱된 수량입니다.</param>
    /// <returns>파싱에 성공하면 <see langword="true"/>를 반환합니다.</returns>
    private static bool TryParseDecimal(string? text, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
