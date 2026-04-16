using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Infrastructure.OperatorExecution;
using Mes.Infrastructure.OperatorExecution.InMemory;

namespace Mes.ExperienceApi.OperatorExecution;

/// <summary>
/// 운영자 실행 reference host에 필요한 서비스를 등록하는 확장 메서드를 제공합니다.
/// </summary>
public static class OperatorExecutionServiceCollectionExtensions
{
    /// <summary>
    /// 현재 slice를 위한 in-memory reference 서비스 구성을 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    /// <returns>추가 등록을 이어갈 수 있는 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddOperatorExecutionReferenceServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CanonicalCommandFingerprintBuilder>();
        services.AddSingleton<CommandReceiptIdempotencyPolicy>();
        services.AddSingleton<StoredCommandResponseSerializer>();
        services.AddSingleton<QualityHoldGateCoordinator>();
        services.AddSingleton<ProductionActualsPreparationService>();
        services.AddSingleton<StationWorkQueueReadService>();
        services.AddSingleton<GetStationWorkQueueQueryHandler>();
        services.AddSingleton<OperatorExecutionCommandHandler>();
        services.AddSingleton<InMemoryOperatorExecutionStore>();
        services.AddSingleton<IOperatorExecutionCommandPort, InMemoryOperatorExecutionCommandAdapter>();
        services.AddSingleton<IStationWorkQueueSourcePort, InMemoryStationWorkQueueSourceAdapter>();
        services.AddSingleton<OperatorExecutionApplicationService>();
        services.AddSingleton<OperatorExecutionBffEndpointAdapter>();

        return services;
    }
}
