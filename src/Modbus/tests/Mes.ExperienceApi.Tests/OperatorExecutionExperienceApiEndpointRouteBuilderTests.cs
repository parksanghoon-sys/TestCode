using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.ExperienceApi.OperatorExecution;
using Mes.Infrastructure.OperatorExecution;
using Mes.Infrastructure.OperatorExecution.FileStore;
using Mes.Infrastructure.OperatorExecution.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mes.ExperienceApi.Tests;

/// <summary>
/// operator-execution Experience API host의 DI와 라우트 구성을 검증합니다.
/// </summary>
public sealed class OperatorExecutionExperienceApiEndpointRouteBuilderTests
{
    /// <summary>
    /// 별도 provider 설정이 없으면 endpoint adapter와 SQLite 저장소가 기본으로 해석되는지 검증합니다.
    /// </summary>
    [Fact]
    public void AddOperatorExecutionDurableServices_should_resolve_endpoint_adapter_and_sqlite_store_by_default()
    {
        var app = CreateApplication();

        Assert.NotNull(app.Services.GetService<OperatorExecutionBffEndpointAdapter>());
        Assert.NotNull(app.Services.GetService<SqliteOperatorExecutionStore>());
        Assert.Null(app.Services.GetService<FileOperatorExecutionStore>());
    }

    /// <summary>
    /// FileStore provider를 명시하면 비교용 경로로 파일 기반 저장소가 해석되는지 검증합니다.
    /// </summary>
    [Fact]
    public void AddOperatorExecutionDurableServices_should_resolve_file_store_for_explicit_comparison_path()
    {
        var app = CreateApplication((builder, _, _) =>
        {
            builder.Configuration["Mes:OperatorExecutionDurableProvider"] = "FileStore";
        });

        Assert.NotNull(app.Services.GetService<OperatorExecutionBffEndpointAdapter>());
        Assert.NotNull(app.Services.GetService<FileOperatorExecutionStore>());
        Assert.Null(app.Services.GetService<SqliteOperatorExecutionStore>());
    }

    /// <summary>
    /// 레거시 file-store 경로 설정만 있어도 provider가 명시되지 않으면 SQLite 기본 런타임이 유지되는지 검증합니다.
    /// </summary>
    [Fact]
    public void AddOperatorExecutionDurableServices_should_keep_sqlite_default_when_only_legacy_file_store_path_is_present()
    {
        var app = CreateApplication((builder, sqliteDatabasePath, _) =>
        {
            builder.Configuration["Mes:OperatorExecutionSqliteDatabasePath"] = sqliteDatabasePath;
            builder.Configuration["Mes:OperatorExecutionStorePath"] = Path.Combine(
                Path.GetTempPath(),
                "Modbus",
                "Mes.ExperienceApi.Tests",
                Guid.NewGuid().ToString("N"),
                "legacy-operator-execution-store.json");
        });

        Assert.NotNull(app.Services.GetService<OperatorExecutionBffEndpointAdapter>());
        Assert.NotNull(app.Services.GetService<SqliteOperatorExecutionStore>());
        Assert.Null(app.Services.GetService<FileOperatorExecutionStore>());
    }

    /// <summary>
    /// PostgreSQL provider는 아직 reserved 상태이므로 명확한 예외를 반환하는지 검증합니다.
    /// </summary>
    [Fact]
    public void AddOperatorExecutionDurableServices_should_throw_for_reserved_postgres_provider()
    {
        var builder = CreateBuilder();
        builder.Configuration["Mes:OperatorExecutionDurableProvider"] = "Postgres";

        var exception = Assert.Throws<NotSupportedException>(
            () => builder.Services.AddOperatorExecutionDurableServices(builder.Configuration, builder.Environment));

        Assert.Contains("Postgres", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 문서화된 endpoint signature가 모두 minimal API 라우트로 노출되는지 검증합니다.
    /// </summary>
    [Fact]
    public void MapOperatorExecutionEndpoints_should_expose_all_documented_signatures()
    {
        var app = CreateApplication();
        var routeEndpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        var expectedSignatures = OperatorExecutionEndpointSignatures.GetAll();

        Assert.Equal(expectedSignatures.Count, routeEndpoints.Count);

        foreach (var signature in expectedSignatures)
        {
            var routeEndpoint = Assert.Single(
                routeEndpoints,
                endpoint => string.Equals(endpoint.RoutePattern.RawText, signature.Route, StringComparison.Ordinal));

            Assert.Equal(signature.OperationName, routeEndpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
            Assert.Contains(signature.HttpMethod, routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []);
            Assert.NotNull(routeEndpoint.Metadata.GetMetadata<ITagsMetadata>());
        }
    }

    /// <summary>
    /// 테스트용 minimal API application을 만들고 operator-execution 라우트를 등록합니다.
    /// </summary>
    /// <param name="configure">추가 설정 주입 동작입니다.</param>
    /// <returns>operator-execution endpoint가 등록된 web application입니다.</returns>
    private static WebApplication CreateApplication(Action<WebApplicationBuilder, string, string>? configure = null)
    {
        var builder = CreateBuilder();
        var sqliteDatabasePath = Path.Combine(
            Path.GetTempPath(),
            "Modbus",
            "Mes.ExperienceApi.Tests",
            Guid.NewGuid().ToString("N"),
            "operator-execution.db");
        var fileStorePath = Path.Combine(
            Path.GetTempPath(),
            "Modbus",
            "Mes.ExperienceApi.Tests",
            Guid.NewGuid().ToString("N"),
            "operator-execution-store.json");

        builder.Configuration["Mes:OperatorExecutionSqliteDatabasePath"] = sqliteDatabasePath;
        builder.Configuration["Mes:OperatorExecutionFileStorePath"] = fileStorePath;
        configure?.Invoke(builder, sqliteDatabasePath, fileStorePath);
        builder.Services.AddOperatorExecutionProblemDetails();
        builder.Services.AddOperatorExecutionDurableServices(builder.Configuration, builder.Environment);

        var app = builder.Build();
        app.UseOperatorExecutionProblemDetails();
        app.MapOperatorExecutionEndpoints();
        return app;
    }

    /// <summary>
    /// 테스트용 web application builder를 생성합니다.
    /// </summary>
    /// <returns>테스트용 web application builder입니다.</returns>
    private static WebApplicationBuilder CreateBuilder()
    {
        return WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.FullName,
            EnvironmentName = Environments.Development
        });
    }
}
