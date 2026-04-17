using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Infrastructure.OperatorExecution;

namespace Mes.ExperienceApi.OperatorExecution;

/// <summary>
/// 운영자 실행 BFF endpoint를 minimal API 라우트로 매핑하는 확장 메서드를 제공합니다.
/// </summary>
public static class OperatorExecutionEndpointRouteBuilderExtensions
{
    /// <summary>
    /// 운영자 실행 slice가 제공하는 모든 reference endpoint를 라우트에 등록합니다.
    /// </summary>
    /// <param name="endpoints">라우트를 추가할 endpoint builder입니다.</param>
    /// <returns>동일한 endpoint builder입니다.</returns>
    public static IEndpointRouteBuilder MapOperatorExecutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        MapCommand<StartOperationCommandContract, StartOperationResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.StartOperation,
            static (adapter, command, cancellationToken) => adapter.StartOperationAsync(command, cancellationToken));

        MapCommand<RecordMaterialConsumptionCommandContract, RecordMaterialConsumptionResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.RecordMaterialConsumption,
            static (adapter, command, cancellationToken) => adapter.RecordMaterialConsumptionAsync(command, cancellationToken));

        MapCommand<RecordMaterialScanCommandContract, RecordMaterialScanResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.RecordMaterialScan,
            static (adapter, command, cancellationToken) => adapter.RecordMaterialScanAsync(command, cancellationToken));

        MapCommand<PlaceHoldCommandContract, PlaceHoldResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.PlaceHold,
            static (adapter, command, cancellationToken) => adapter.PlaceHoldAsync(command, cancellationToken));

        MapCommand<ReleaseHoldCommandContract, ReleaseHoldResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.ReleaseHold,
            static (adapter, command, cancellationToken) => adapter.ReleaseHoldAsync(command, cancellationToken));

        MapCommand<RecordQualityResultCommandContract, RecordQualityResultResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.RecordQualityResult,
            static (adapter, command, cancellationToken) => adapter.RecordQualityResultAsync(command, cancellationToken));

        MapCommand<CompleteOperationCommandContract, CompleteOperationResponseContract>(
            endpoints,
            OperatorExecutionEndpointSignatures.CompleteOperation,
            static (adapter, command, cancellationToken) => adapter.CompleteOperationAsync(command, cancellationToken));

        endpoints.MapGet(
                OperatorExecutionEndpointSignatures.GetStationWorkQueue.Route,
                static (string stationId, OperatorExecutionBffEndpointAdapter adapter, CancellationToken cancellationToken) =>
                    adapter.GetStationWorkQueueAsync(new GetStationWorkQueueRequestContract(stationId), cancellationToken))
            .WithName(OperatorExecutionEndpointSignatures.GetStationWorkQueue.OperationName)
            .WithTags("OperatorExecution");

        return endpoints;
    }

    /// <summary>
    /// command endpoint 하나를 공통 metadata와 함께 등록합니다.
    /// </summary>
    /// <typeparam name="TCommand">요청 계약 형식입니다.</typeparam>
    /// <typeparam name="TResponse">응답 계약 형식입니다.</typeparam>
    /// <param name="endpoints">등록 대상 endpoint builder입니다.</param>
    /// <param name="signature">계약에서 정의한 endpoint 시그니처입니다.</param>
    /// <param name="handler">endpoint adapter를 호출하는 handler입니다.</param>
    private static void MapCommand<TCommand, TResponse>(
        IEndpointRouteBuilder endpoints,
        BffEndpointSignature signature,
        Func<OperatorExecutionBffEndpointAdapter, TCommand, CancellationToken, Task<TResponse>> handler)
        where TCommand : class
    {
        endpoints.MapPost(
                signature.Route,
                (TCommand command, OperatorExecutionBffEndpointAdapter adapter, CancellationToken cancellationToken) =>
                    handler(adapter, command, cancellationToken))
            .WithName(signature.OperationName)
            .WithTags("OperatorExecution");
    }
}
