using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.OperatorExecution;

namespace Mes.Infrastructure.OperatorExecution;

/// <summary>
/// operator-execution BFF endpoint를 contract 기준으로 노출하는 façade를 제공합니다.
/// </summary>
public sealed class OperatorExecutionBffEndpointAdapter
{
    private readonly OperatorExecutionApplicationService _applicationService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// BFF endpoint façade를 초기화합니다.
    /// </summary>
    /// <param name="applicationService">실제 application orchestration 서비스입니다.</param>
    /// <param name="timeProvider">서버 기준 시각 공급자입니다.</param>
    public OperatorExecutionBffEndpointAdapter(
        OperatorExecutionApplicationService applicationService,
        TimeProvider timeProvider)
    {
        _applicationService = applicationService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// façade가 제공하는 endpoint 시그니처 목록을 반환합니다.
    /// </summary>
    /// <returns>지원하는 endpoint 시그니처 목록입니다.</returns>
    public IReadOnlyList<BffEndpointSignature> GetSupportedEndpoints()
    {
        return OperatorExecutionEndpointSignatures.GetAll();
    }

    /// <summary>
    /// `start-operation` endpoint를 실행합니다.
    /// </summary>
    /// <param name="command">공정 시작 command 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>공정 시작 응답 계약입니다.</returns>
    public async Task<StartOperationResponseContract> StartOperationAsync(
        StartOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<StartOperationCommandContract>(
                command,
                GetCurrentTimestamp()),
            cancellationToken);

        return result.Response;
    }

    /// <summary>
    /// `record-material-consumption` endpoint를 실행합니다.
    /// </summary>
    /// <param name="command">자재 소모 command 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>자재 소모 응답 계약입니다.</returns>
    public async Task<RecordMaterialConsumptionResponseContract> RecordMaterialConsumptionAsync(
        RecordMaterialConsumptionCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<RecordMaterialConsumptionCommandContract>(
                command,
                GetCurrentTimestamp()),
            cancellationToken);

        return result.Response;
    }

    /// <summary>
    /// `place-hold` endpoint를 실행합니다.
    /// </summary>
    /// <param name="command">hold 설정 command 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>hold 설정 응답 계약입니다.</returns>
    public async Task<PlaceHoldResponseContract> PlaceHoldAsync(
        PlaceHoldCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<PlaceHoldCommandContract>(
                command,
                GetCurrentTimestamp()),
            cancellationToken);

        return result.Response;
    }

    /// <summary>
    /// `release-hold` endpoint를 실행합니다.
    /// </summary>
    /// <param name="command">hold 해제 command 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>hold 해제 응답 계약입니다.</returns>
    public async Task<ReleaseHoldResponseContract> ReleaseHoldAsync(
        ReleaseHoldCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<ReleaseHoldCommandContract>(
                command,
                GetCurrentTimestamp()),
            cancellationToken);

        return result.Response;
    }

    /// <summary>
    /// `record-quality-result` endpoint를 실행합니다.
    /// </summary>
    /// <param name="command">품질 결과 command 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>품질 결과 응답 계약입니다.</returns>
    public async Task<RecordQualityResultResponseContract> RecordQualityResultAsync(
        RecordQualityResultCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<RecordQualityResultCommandContract>(
                command,
                GetCurrentTimestamp()),
            cancellationToken);

        return result.Response;
    }

    /// <summary>
    /// `complete-operation` endpoint를 실행합니다.
    /// </summary>
    /// <param name="command">공정 완료 command 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>공정 완료 응답 계약입니다.</returns>
    public async Task<CompleteOperationResponseContract> CompleteOperationAsync(
        CompleteOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.HandleAsync(
            new ExecuteOperatorExecutionCommandRequest<CompleteOperationCommandContract>(
                command,
                GetCurrentTimestamp()),
            cancellationToken);

        return result.Response;
    }

    /// <summary>
    /// `GetStationWorkQueue` endpoint를 실행합니다.
    /// </summary>
    /// <param name="request">작업 큐 query 계약입니다.</param>
    /// <param name="cancellationToken">비동기 취소 토큰입니다.</param>
    /// <returns>작업 큐 응답 계약입니다.</returns>
    public Task<GetStationWorkQueueResponseContract> GetStationWorkQueueAsync(
        GetStationWorkQueueRequestContract request,
        CancellationToken cancellationToken = default)
    {
        return _applicationService.HandleAsync(
            new ExecuteStationWorkQueueQueryRequest(
                request,
                GetCurrentTimestamp()),
            cancellationToken);
    }

    /// <summary>
    /// 현재 서버 기준 시각을 반환합니다.
    /// </summary>
    /// <returns>UTC 기준 현재 시각입니다.</returns>
    private DateTimeOffset GetCurrentTimestamp()
    {
        return _timeProvider.GetUtcNow();
    }
}
