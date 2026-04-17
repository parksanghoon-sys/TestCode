using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Infrastructure.OperatorExecution;
using Mes.Infrastructure.OperatorExecution.FileStore;
using Mes.Infrastructure.OperatorExecution.InMemory;
using Mes.Infrastructure.OperatorExecution.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mes.ExperienceApi.OperatorExecution;

/// <summary>
/// operator-execution host에 필요한 DI 구성을 등록하는 확장 메서드 모음입니다.
/// </summary>
public static class OperatorExecutionServiceCollectionExtensions
{
    private const string DurableProviderConfigurationKey = "Mes:OperatorExecutionDurableProvider";
    private const string SqliteDatabasePathConfigurationKey = "Mes:OperatorExecutionSqliteDatabasePath";
    private const string FileStorePathConfigurationKey = "Mes:OperatorExecutionFileStorePath";
    private const string LegacyFileStorePathConfigurationKey = "Mes:OperatorExecutionStorePath";
    private const string DurableConnectionStringConfigurationKey = "Mes:OperatorExecutionConnectionString";
    private const string SqliteProviderName = "Sqlite";
    private const string FileStoreProviderName = "FileStore";
    private const string PostgresProviderName = "Postgres";

    /// <summary>
    /// 현재 설정에 따라 operator-execution durable 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    /// <param name="configuration">호스트 구성입니다.</param>
    /// <param name="environment">호스트 환경 정보입니다.</param>
    /// <returns>추가 등록이 반영된 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddOperatorExecutionDurableServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        return ResolveDurableProvider(configuration) switch
        {
            SqliteProviderName => services.AddOperatorExecutionSqliteDurableServices(configuration, environment),
            FileStoreProviderName => services.AddOperatorExecutionFileStoreDurableServices(configuration, environment),
            PostgresProviderName => throw CreateUnsupportedProviderException(PostgresProviderName),
            var providerName => throw CreateUnknownProviderException(providerName)
        };
    }

    /// <summary>
    /// SQLite 기반 operator-execution durable 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    /// <param name="configuration">호스트 구성입니다.</param>
    /// <param name="environment">호스트 환경 정보입니다.</param>
    /// <returns>추가 등록이 반영된 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddOperatorExecutionSqliteDurableServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        AddSharedServices(services);
        AddDurableBridgeServices(services);
        services.AddSingleton(new SqliteOperatorExecutionStoreOptions
        {
            DatabaseFilePath = ResolveSqliteDatabasePath(configuration, environment)
        });
        services.AddSingleton<SqliteOperatorExecutionStore>();
        services.AddSingleton<IOperatorExecutionCommandPort, SqliteOperatorExecutionCommandAdapter>();
        services.AddSingleton<IStationWorkQueueSourcePort, SqliteStationWorkQueueSourceAdapter>();

        return services;
    }

    /// <summary>
    /// 파일 기반 operator-execution durable 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    /// <param name="configuration">호스트 구성입니다.</param>
    /// <param name="environment">호스트 환경 정보입니다.</param>
    /// <returns>추가 등록이 반영된 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddOperatorExecutionFileStoreDurableServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        AddSharedServices(services);
        AddDurableBridgeServices(services);
        services.AddSingleton(new FileOperatorExecutionStoreOptions
        {
            StoreFilePath = ResolveFileStorePath(configuration, environment)
        });
        services.AddSingleton<FileOperatorExecutionStore>();
        services.AddSingleton<IOperatorExecutionCommandPort, FileOperatorExecutionCommandAdapter>();
        services.AddSingleton<IStationWorkQueueSourcePort, FileStationWorkQueueSourceAdapter>();

        return services;
    }

    /// <summary>
    /// 현재 slice를 위한 in-memory reference 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    /// <returns>추가 등록이 반영된 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddOperatorExecutionReferenceServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        AddSharedServices(services);
        AddDurableBridgeServices(services);
        services.AddSingleton<InMemoryOperatorExecutionStore>();
        services.AddSingleton<IOperatorExecutionCommandPort, InMemoryOperatorExecutionCommandAdapter>();
        services.AddSingleton<IStationWorkQueueSourcePort, InMemoryStationWorkQueueSourceAdapter>();

        return services;
    }

    /// <summary>
    /// durable과 reference 구성이 공통으로 사용하는 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    private static void AddSharedServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CanonicalCommandFingerprintBuilder>();
        services.AddSingleton<CommandReceiptIdempotencyPolicy>();
        services.AddSingleton<StoredCommandResponseSerializer>();
        services.AddSingleton<QualityHoldGateCoordinator>();
        services.AddSingleton<ProductionActualsPreparationService>();
        services.AddSingleton<StationWorkQueueReadService>();
        services.AddSingleton<GetStationWorkQueueQueryHandler>();
        services.AddSingleton<OperatorExecutionCommandHandler>();
    }

    /// <summary>
    /// durable provider와 무관한 host bridge 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 등록 대상 컬렉션입니다.</param>
    private static void AddDurableBridgeServices(IServiceCollection services)
    {
        services.AddSingleton<OperatorExecutionApplicationService>();
        services.AddSingleton<OperatorExecutionBffEndpointAdapter>();
    }

    /// <summary>
    /// 현재 호스트 설정에서 durable provider 이름을 결정합니다.
    /// </summary>
    /// <param name="configuration">호스트 구성입니다.</param>
    /// <returns>정규화된 durable provider 이름입니다.</returns>
    private static string ResolveDurableProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return NormalizeProviderName(configuration[DurableProviderConfigurationKey]);
    }

    /// <summary>
    /// SQLite 데이터베이스 파일 경로를 결정합니다.
    /// </summary>
    /// <param name="configuration">호스트 구성입니다.</param>
    /// <param name="environment">호스트 환경 정보입니다.</param>
    /// <returns>SQLite 데이터베이스 파일 절대 경로입니다.</returns>
    private static string ResolveSqliteDatabasePath(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration[SqliteDatabasePathConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        return Path.Combine(environment.ContentRootPath, "App_Data", "operator-execution.db");
    }

    /// <summary>
    /// 파일 기반 durable 저장소 경로를 결정합니다.
    /// </summary>
    /// <param name="configuration">호스트 구성입니다.</param>
    /// <param name="environment">호스트 환경 정보입니다.</param>
    /// <returns>파일 기반 저장소 JSON 파일 절대 경로입니다.</returns>
    private static string ResolveFileStorePath(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration[FileStorePathConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var legacyConfiguredPath = configuration[LegacyFileStorePathConfigurationKey];
        if (!string.IsNullOrWhiteSpace(legacyConfiguredPath))
        {
            return Path.GetFullPath(legacyConfiguredPath);
        }

        return Path.Combine(environment.ContentRootPath, "App_Data", "operator-execution-store.json");
    }

    /// <summary>
    /// provider 이름을 현재 지원 형식으로 정규화합니다.
    /// </summary>
    /// <param name="providerName">구성에서 읽은 provider 이름입니다.</param>
    /// <returns>정규화된 provider 이름입니다.</returns>
    private static string NormalizeProviderName(string? providerName)
    {
        return providerName?.Trim().ToUpperInvariant() switch
        {
            null or "" => SqliteProviderName,
            "SQLITE" => SqliteProviderName,
            "FILE" or "FILESTORE" => FileStoreProviderName,
            "POSTGRES" or "POSTGRESQL" => PostgresProviderName,
            _ => providerName.Trim()
        };
    }

    /// <summary>
    /// 아직 구현되지 않은 durable provider 요청에 대한 예외를 생성합니다.
    /// </summary>
    /// <param name="providerName">요청된 provider 이름입니다.</param>
    /// <returns>설명 가능한 예외입니다.</returns>
    private static NotSupportedException CreateUnsupportedProviderException(string providerName)
    {
        return new NotSupportedException(
            $"Durable provider '{providerName}' is reserved for future use. " +
            $"Use '{SqliteProviderName}' for the active runtime. " +
            $"Keep '{FileStoreProviderName}' only as a comparison path while the future relational provider is still pending. " +
            $"The '{DurableConnectionStringConfigurationKey}' setting is kept for a future relational provider such as PostgreSQL.");
    }

    /// <summary>
    /// 알 수 없는 durable provider 요청에 대한 예외를 생성합니다.
    /// </summary>
    /// <param name="providerName">요청된 provider 이름입니다.</param>
    /// <returns>설명 가능한 예외입니다.</returns>
    private static NotSupportedException CreateUnknownProviderException(string providerName)
    {
        return new NotSupportedException(
            $"Unsupported durable provider '{providerName}'. " +
            $"Use '{SqliteProviderName}' for the active runtime, " +
            $"or '{FileStoreProviderName}' only for comparison until a PostgreSQL adapter exists behind '{PostgresProviderName}'.");
    }
}
