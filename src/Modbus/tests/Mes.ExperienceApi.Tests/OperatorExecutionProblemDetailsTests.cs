using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.ExperienceApi.OperatorExecution;
using Mes.Domain.Aggregates;
using Mes.Domain.ValueObjects;
using Mes.Infrastructure.OperatorExecution.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mes.ExperienceApi.Tests;

/// <summary>
/// operator-execution Experience API가 deterministic failure를 stable problem details로 노출하는지 검증합니다.
/// </summary>
public sealed class OperatorExecutionProblemDetailsTests
{
    /// <summary>
    /// authoritative aggregate가 없으면 `404 Not Found` problem details가 반환되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_should_return_problem_details_for_missing_aggregate_state()
    {
        await using var application = await StartApplicationAsync();

        using var response = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.StartOperation.Route,
            new StartOperationCommandContract(
                CreateContext("CMD-404-01", "CORR-404-01", "KEY-404-01", "operator-404", "ST-404"),
                new StartOperationPayloadContract("PO-404-01", "OP-404-01", 10, "EA")));

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("operator_execution.not_found", problem.GetProperty("errorCode").GetString());
        Assert.Equal("OperationExecution", problem.GetProperty("aggregateType").GetString());
        Assert.Equal("OP-404-01", problem.GetProperty("aggregateId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// idempotency fingerprint가 충돌하면 `409 Conflict` problem details가 반환되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperationAsync_should_return_problem_details_for_idempotency_conflict()
    {
        await using var application = await StartApplicationAsync();
        var store = application.Services.GetRequiredService<SqliteOperatorExecutionStore>();
        var order = CreateProductionOrder("PO-409-01");
        var operation = CreateRunningOperation(order.Id.ToString(), "OP-409-01", "ST-409", 10, new DateTimeOffset(2026, 4, 17, 11, 0, 0, TimeSpan.Zero));
        order.AttachOperation(operation.Id);

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        var acceptedCommand = new CompleteOperationCommandContract(
            CreateContext("CMD-409-01", "CORR-409-01", "KEY-409-01", "operator-409", "ST-409"),
            new CompleteOperationPayloadContract(
                operation.Id.ToString(),
                new MeasuredQuantityContract(5m, "EA"),
                new MeasuredQuantityContract(1m, "EA"),
                CompletionModeValues.Manual));
        var conflictCommand = new CompleteOperationCommandContract(
            CreateContext("CMD-409-02", "CORR-409-01", "KEY-409-01", "operator-409", "ST-409"),
            new CompleteOperationPayloadContract(
                operation.Id.ToString(),
                new MeasuredQuantityContract(6m, "EA"),
                new MeasuredQuantityContract(1m, "EA"),
                CompletionModeValues.Manual));

        using var acceptedResponse = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.CompleteOperation.Route,
            acceptedCommand);
        using var conflictResponse = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.CompleteOperation.Route,
            conflictCommand);

        var problem = await ReadProblemAsync(conflictResponse);

        Assert.Equal(HttpStatusCode.OK, acceptedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal("operator_execution.conflict", problem.GetProperty("errorCode").GetString());
        Assert.Equal("OperationExecution", problem.GetProperty("aggregateType").GetString());
        Assert.Equal("OP-409-01", problem.GetProperty("aggregateId").GetString());
        Assert.Equal("CMD-409-01", problem.GetProperty("commandId").GetString());
        Assert.Equal("KEY-409-01", problem.GetProperty("idempotencyKey").GetString());
    }

    /// <summary>
    /// semantic validation 실패가 `422 Unprocessable Entity` problem details로 반환되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_should_return_problem_details_for_semantic_validation_failure()
    {
        await using var application = await StartApplicationAsync();
        var store = application.Services.GetRequiredService<SqliteOperatorExecutionStore>();
        var order = CreateProductionOrder("PO-422-01");
        var operation = CreateQueuedOperation(order.Id, "OP-422-01", 10);
        order.AttachOperation(operation.Id);

        store.Seed(new SqliteOperatorExecutionSeed
        {
            ProductionOrders = [order],
            OperationExecutions = [operation]
        });

        using var response = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.StartOperation.Route,
            new StartOperationCommandContract(
                CreateContext("CMD-422-01", "CORR-422-01", "KEY-422-01", "operator-422", "ST-422"),
                new StartOperationPayloadContract(order.Id.ToString(), operation.Id.ToString(), 20, "EA")));

        var problem = await ReadProblemAsync(response);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal("operator_execution.validation_failed", problem.GetProperty("errorCode").GetString());
        Assert.Equal("OperationExecution", problem.GetProperty("aggregateType").GetString());
        Assert.Equal("OP-422-01", problem.GetProperty("aggregateId").GetString());
        Assert.Equal("CMD-422-01", problem.GetProperty("commandId").GetString());
        Assert.Equal("KEY-422-01", problem.GetProperty("idempotencyKey").GetString());
    }

    /// <summary>
    /// malformed JSON이 `400 Bad Request` problem details로 반환되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_should_return_problem_details_for_transport_bad_request()
    {
        await using var application = await StartApplicationAsync();
        using var request = new StringContent("{\"context\":", Encoding.UTF8, "application/json");
        using var response = await application.Client.PostAsync(
            OperatorExecutionEndpointSignatures.StartOperation.Route,
            request);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("transport.invalid_request", problem.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// 예기치 않은 host 내부 예외가 `500 Internal Server Error` problem details로 반환되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_should_return_problem_details_for_unexpected_failure()
    {
        await using var application = await StartApplicationAsync(
            configureAfterRegistration: builder =>
            {
                builder.Services.AddSingleton<TimeProvider>(new ThrowingTimeProvider());
            });

        using var response = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.StartOperation.Route,
            new StartOperationCommandContract(
                CreateContext("CMD-500-01", "CORR-500-01", "KEY-500-01", "operator-500", "ST-500"),
                new StartOperationPayloadContract("PO-500-01", "OP-500-01", 10, "EA")));

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("system.unexpected_error", problem.GetProperty("errorCode").GetString());
        Assert.False(problem.TryGetProperty("aggregateType", out _));
    }

    /// <summary>
    /// 테스트용 Experience API 애플리케이션을 실제 HTTP 리스너와 함께 시작합니다.
    /// </summary>
    /// <param name="configureBeforeRegistration">기본 파일 경로 설정 직후 실행할 구성 콜백입니다.</param>
    /// <param name="configureAfterRegistration">표준 서비스 등록 이후 실행할 구성 콜백입니다.</param>
    /// <returns>실행 중인 테스트용 애플리케이션 핸들입니다.</returns>
    private static async Task<StartedOperatorExecutionApplication> StartApplicationAsync(
        Action<WebApplicationBuilder, string, string>? configureBeforeRegistration = null,
        Action<WebApplicationBuilder>? configureAfterRegistration = null)
    {
        var application = CreateApplication(configureBeforeRegistration, configureAfterRegistration);
        application.Urls.Add("http://127.0.0.1:0");
        await application.StartAsync();

        var server = application.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The test application did not expose an HTTP address.");

        return new StartedOperatorExecutionApplication(
            application,
            new HttpClient
            {
                BaseAddress = new Uri(address, UriKind.Absolute)
            });
    }

    /// <summary>
    /// 테스트용 Experience API 애플리케이션을 생성하고 표준 host 구성을 적용합니다.
    /// </summary>
    /// <param name="configureBeforeRegistration">기본 파일 경로 설정 직후 실행할 구성 콜백입니다.</param>
    /// <param name="configureAfterRegistration">표준 서비스 등록 이후 실행할 구성 콜백입니다.</param>
    /// <returns>구성된 웹 애플리케이션입니다.</returns>
    private static WebApplication CreateApplication(
        Action<WebApplicationBuilder, string, string>? configureBeforeRegistration = null,
        Action<WebApplicationBuilder>? configureAfterRegistration = null)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.FullName,
            EnvironmentName = Environments.Development
        });
        var sqliteDatabasePath = Path.Combine(
            Path.GetTempPath(),
            "Modbus",
            "Mes.ExperienceApi.Tests",
            Guid.NewGuid().ToString("N"),
            "problem-details-operator-execution.db");
        var fileStorePath = Path.Combine(
            Path.GetTempPath(),
            "Modbus",
            "Mes.ExperienceApi.Tests",
            Guid.NewGuid().ToString("N"),
            "problem-details-operator-execution-store.json");

        builder.Configuration["Mes:OperatorExecutionSqliteDatabasePath"] = sqliteDatabasePath;
        builder.Configuration["Mes:OperatorExecutionFileStorePath"] = fileStorePath;
        configureBeforeRegistration?.Invoke(builder, sqliteDatabasePath, fileStorePath);
        builder.Services.AddOperatorExecutionProblemDetails();
        builder.Services.AddOperatorExecutionDurableServices(builder.Configuration, builder.Environment);
        configureAfterRegistration?.Invoke(builder);

        var application = builder.Build();
        application.UseOperatorExecutionProblemDetails();
        application.MapOperatorExecutionEndpoints();
        return application;
    }

    /// <summary>
    /// HTTP 응답 본문에서 problem details JSON을 읽습니다.
    /// </summary>
    /// <param name="response">읽을 HTTP 응답입니다.</param>
    /// <returns>problem details JSON 루트 요소입니다.</returns>
    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    /// <summary>
    /// 테스트용 command context를 생성합니다.
    /// </summary>
    /// <param name="commandId">command 식별자입니다.</param>
    /// <param name="correlationId">correlation 식별자입니다.</param>
    /// <param name="idempotencyKey">idempotency key입니다.</param>
    /// <param name="actorId">actor 식별자입니다.</param>
    /// <param name="stationId">station 식별자입니다.</param>
    /// <returns>공통 command context 계약입니다.</returns>
    private static CommandContextContract CreateContext(
        string commandId,
        string correlationId,
        string idempotencyKey,
        string actorId,
        string stationId)
    {
        return new CommandContextContract(
            new CommandIdentityContract(commandId, correlationId, idempotencyKey),
            new CommandOriginContract(actorId, BffChannelValues.Wpf, stationId),
            new DateTimeOffset(2026, 4, 17, 10, 55, 0, TimeSpan.Zero),
            null);
    }

    /// <summary>
    /// 테스트용 production order를 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">production order 식별자입니다.</param>
    /// <returns>released 상태의 production order입니다.</returns>
    private static ProductionOrder CreateProductionOrder(string productionOrderId)
    {
        return ProductionOrder.Release(
            new ProductionOrderId(productionOrderId),
            "ITEM-" + productionOrderId,
            "ROUTE-A",
            new DateTimeOffset(2026, 4, 17, 10, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// 테스트용 queued operation execution을 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 production order 식별자입니다.</param>
    /// <param name="operationExecutionId">operation execution 식별자입니다.</param>
    /// <param name="operationSequence">operation sequence입니다.</param>
    /// <returns>queued 상태의 operation execution입니다.</returns>
    private static OperationExecution CreateQueuedOperation(
        ProductionOrderId productionOrderId,
        string operationExecutionId,
        int operationSequence)
    {
        var operation = OperationExecution.Create(
            new OperationExecutionId(operationExecutionId),
            productionOrderId,
            operationSequence);

        operation.QueueForExecution();
        return operation;
    }

    /// <summary>
    /// 테스트용 running operation execution을 생성합니다.
    /// </summary>
    /// <param name="productionOrderId">상위 production order 식별자입니다.</param>
    /// <param name="operationExecutionId">operation execution 식별자입니다.</param>
    /// <param name="stationId">station 식별자입니다.</param>
    /// <param name="operationSequence">operation sequence입니다.</param>
    /// <param name="startedAt">시작 시각입니다.</param>
    /// <returns>running 상태의 operation execution입니다.</returns>
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
            operationSequence);

        operation.QueueForExecution();
        operation.Start(new StationId(stationId), startedAt);
        operation.ClearDomainEvents();
        return operation;
    }

    /// <summary>
    /// 실행 중인 테스트용 Experience API 애플리케이션과 HTTP client를 묶습니다.
    /// </summary>
    /// <param name="Application">실행 중인 웹 애플리케이션입니다.</param>
    /// <param name="Client">애플리케이션에 연결된 HTTP client입니다.</param>
    private sealed record StartedOperatorExecutionApplication(
        WebApplication Application,
        HttpClient Client) : IAsyncDisposable
    {
        /// <summary>
        /// 애플리케이션의 서비스 공급자를 반환합니다.
        /// </summary>
        public IServiceProvider Services => Application.Services;

        /// <summary>
        /// 테스트가 끝나면 HTTP client와 웹 애플리케이션을 정리합니다.
        /// </summary>
        /// <returns>비동기 정리 작업입니다.</returns>
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Application.StopAsync();
            await Application.DisposeAsync();
        }
    }

    /// <summary>
    /// 예기치 않은 내부 오류를 만들기 위한 테스트용 time provider입니다.
    /// </summary>
    private sealed class ThrowingTimeProvider : TimeProvider
    {
        /// <summary>
        /// 항상 예외를 발생시켜 host fallback 경로를 검증합니다.
        /// </summary>
        /// <returns>반환되지 않습니다.</returns>
        public override DateTimeOffset GetUtcNow()
        {
            throw new InvalidOperationException("Simulated host failure.");
        }
    }
}
