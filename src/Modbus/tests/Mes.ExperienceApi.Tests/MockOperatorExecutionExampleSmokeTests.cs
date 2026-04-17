using System.Net.Http.Json;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.ExperienceApi.OperatorExecution;
using Mes.MockStation.Example;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mes.ExperienceApi.Tests;

/// <summary>
/// example 폴더의 mock station 시나리오가 실제 thin-host HTTP 경로에서도 유지되는지 검증합니다.
/// </summary>
public sealed class MockOperatorExecutionExampleSmokeTests
{
    /// <summary>
    /// mock station example이 queue 조회, 시작, 자재 투입, 완료까지 현재 HTTP 경로로 실행되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task Mock_station_example_should_exercise_current_http_station_flow()
    {
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            "Modbus",
            "Mes.ExperienceApi.Tests",
            Guid.NewGuid().ToString("N"),
            "mock-example");
        var databaseFilePath = Path.Combine(outputRoot, "operator-execution-example.db");
        var manifestFilePath = Path.Combine(outputRoot, "mock-station-scenario.json");
        var seedResult = MockOperatorExecutionScenarioSeeder.SeedSqlite(
            new MockOperatorExecutionSeedRequest
            {
                DatabaseFilePath = databaseFilePath,
                ManifestFilePath = manifestFilePath,
                SeededAt = new DateTimeOffset(2026, 4, 17, 13, 0, 0, TimeSpan.Zero)
            });

        await using var application = await StartApplicationAsync(seedResult.Manifest.DatabaseFilePath);

        var initialQueue = await application.Client.GetFromJsonAsync<GetStationWorkQueueResponseContract>(
            $"/api/bff/operator-execution/stations/{seedResult.Manifest.StationId}/work-queue");

        Assert.NotNull(initialQueue);
        Assert.Equal(2, initialQueue.Items.Count);
        Assert.Contains(
            initialQueue.Items,
            item => item.OperationExecutionId == seedResult.Manifest.QueuedOperation.OperationExecutionId
                && item.Status == "Queued");
        Assert.Contains(
            initialQueue.Items,
            item => item.OperationExecutionId == seedResult.Manifest.RunningOperation.OperationExecutionId
                && item.Status == "Running");

        using var startResponse = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.StartOperation.Route,
            CreateStartOperationCommand(seedResult.Manifest));
        var started = await startResponse.Content.ReadFromJsonAsync<StartOperationResponseContract>();

        Assert.True(startResponse.IsSuccessStatusCode);
        Assert.NotNull(started);
        Assert.True(started.Accepted);
        Assert.Equal("Running", started.Status);

        using var materialResponse = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.RecordMaterialConsumption.Route,
            CreateMaterialConsumptionCommand(seedResult.Manifest));
        var consumed = await materialResponse.Content.ReadFromJsonAsync<RecordMaterialConsumptionResponseContract>();

        Assert.True(materialResponse.IsSuccessStatusCode);
        Assert.NotNull(consumed);
        Assert.True(consumed.Accepted);
        Assert.Equal("LOT-EXAMPLE-01", consumed.MaterialLotId);
        Assert.Equal(10m, consumed.RemainingQuantity.Value);
        Assert.Equal("EA", consumed.RemainingQuantity.Unit);

        using var completeResponse = await application.Client.PostAsJsonAsync(
            OperatorExecutionEndpointSignatures.CompleteOperation.Route,
            CreateCompleteOperationCommand(seedResult.Manifest));
        var completed = await completeResponse.Content.ReadFromJsonAsync<CompleteOperationResponseContract>();

        Assert.True(completeResponse.IsSuccessStatusCode);
        Assert.NotNull(completed);
        Assert.True(completed.Accepted);
        Assert.Equal("Done", completed.Status);
        Assert.Equal(ProductionActualsStatusValues.PendingProjection, completed.ProductionActualsStatus);

        var refreshedQueue = await application.Client.GetFromJsonAsync<GetStationWorkQueueResponseContract>(
            $"/api/bff/operator-execution/stations/{seedResult.Manifest.StationId}/work-queue");

        Assert.NotNull(refreshedQueue);
        Assert.Contains(
            refreshedQueue.Items,
            item => item.OperationExecutionId == seedResult.Manifest.QueuedOperation.OperationExecutionId
                && item.Status == "Running");
        Assert.Contains(
            refreshedQueue.Items,
            item => item.OperationExecutionId == seedResult.Manifest.RunningOperation.OperationExecutionId
                && item.Status == "Done");
    }

    /// <summary>
    /// random 포트로 mock Experience API를 시작하고 연결용 HTTP client를 돌려줍니다.
    /// </summary>
    /// <param name="sqliteDatabasePath">시드된 SQLite 데이터베이스 파일 경로입니다.</param>
    /// <returns>실행 중인 테스트용 API 호스트와 HTTP client입니다.</returns>
    private static async Task<StartedExampleApplication> StartApplicationAsync(string sqliteDatabasePath)
    {
        var application = CreateApplication(sqliteDatabasePath);
        application.Urls.Add("http://127.0.0.1:0");
        await application.StartAsync();

        var server = application.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("The example test application did not expose an HTTP address.");

        return new StartedExampleApplication(
            application,
            new HttpClient
            {
                BaseAddress = new Uri(address, UriKind.Absolute)
            });
    }

    /// <summary>
    /// 지정된 SQLite 경로를 사용하는 테스트용 Experience API host를 구성합니다.
    /// </summary>
    /// <param name="sqliteDatabasePath">시드된 SQLite 데이터베이스 파일 경로입니다.</param>
    /// <returns>구성된 테스트용 web application입니다.</returns>
    private static WebApplication CreateApplication(string sqliteDatabasePath)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.FullName,
            EnvironmentName = Environments.Development
        });

        builder.Configuration["Mes:OperatorExecutionSqliteDatabasePath"] = sqliteDatabasePath;
        builder.Services.AddOperatorExecutionProblemDetails();
        builder.Services.AddOperatorExecutionDurableServices(builder.Configuration, builder.Environment);

        var application = builder.Build();
        application.UseOperatorExecutionProblemDetails();
        application.MapOperatorExecutionEndpoints();
        return application;
    }

    /// <summary>
    /// queued example operation을 위한 `start-operation` 명령을 생성합니다.
    /// </summary>
    /// <param name="manifest">mock station 시나리오 manifest입니다.</param>
    /// <returns>thin-host smoke에 사용할 시작 명령입니다.</returns>
    private static StartOperationCommandContract CreateStartOperationCommand(MockOperatorExecutionScenarioManifest manifest)
    {
        return new StartOperationCommandContract(
            CreateContext("CMD-EXAMPLE-START-01", "CORR-EXAMPLE-START-01", "KEY-EXAMPLE-START-01", manifest),
            new StartOperationPayloadContract(
                manifest.QueuedOperation.ProductionOrderId,
                manifest.QueuedOperation.OperationExecutionId,
                manifest.QueuedOperation.OperationSequence,
                null));
    }

    /// <summary>
    /// running example operation을 위한 `record-material-consumption` 명령을 생성합니다.
    /// </summary>
    /// <param name="manifest">mock station 시나리오 manifest입니다.</param>
    /// <returns>thin-host smoke에 사용할 자재 투입 명령입니다.</returns>
    private static RecordMaterialConsumptionCommandContract CreateMaterialConsumptionCommand(
        MockOperatorExecutionScenarioManifest manifest)
    {
        return new RecordMaterialConsumptionCommandContract(
            CreateContext("CMD-EXAMPLE-MATERIAL-01", "CORR-EXAMPLE-MATERIAL-01", "KEY-EXAMPLE-MATERIAL-01", manifest),
            new RecordMaterialConsumptionPayloadContract(
                manifest.RunningOperation.OperationExecutionId,
                manifest.RunningOperation.WipUnitId ?? throw new InvalidOperationException("Running example WIP is missing."),
                manifest.RunningOperation.MaterialLotId ?? throw new InvalidOperationException("Running example lot is missing."),
                manifest.RunningOperation.MaterialCode ?? throw new InvalidOperationException("Running example material code is missing."),
                new MeasuredQuantityContract(2m, manifest.RunningOperation.MaterialQuantityUnit ?? "EA")));
    }

    /// <summary>
    /// running example operation을 위한 `complete-operation` 명령을 생성합니다.
    /// </summary>
    /// <param name="manifest">mock station 시나리오 manifest입니다.</param>
    /// <returns>thin-host smoke에 사용할 완료 명령입니다.</returns>
    private static CompleteOperationCommandContract CreateCompleteOperationCommand(
        MockOperatorExecutionScenarioManifest manifest)
    {
        return new CompleteOperationCommandContract(
            CreateContext("CMD-EXAMPLE-COMPLETE-01", "CORR-EXAMPLE-COMPLETE-01", "KEY-EXAMPLE-COMPLETE-01", manifest),
            new CompleteOperationPayloadContract(
                manifest.RunningOperation.OperationExecutionId,
                new MeasuredQuantityContract(5m, manifest.RunningOperation.QuantityUnit),
                new MeasuredQuantityContract(1m, manifest.RunningOperation.QuantityUnit),
                CompletionModeValues.Manual));
    }

    /// <summary>
    /// example smoke에 사용할 공통 command context를 생성합니다.
    /// </summary>
    /// <param name="commandId">명령 식별자입니다.</param>
    /// <param name="correlationId">correlation 식별자입니다.</param>
    /// <param name="idempotencyKey">idempotency 식별자입니다.</param>
    /// <param name="manifest">mock station 시나리오 manifest입니다.</param>
    /// <returns>thin-host smoke 명령 context입니다.</returns>
    private static CommandContextContract CreateContext(
        string commandId,
        string correlationId,
        string idempotencyKey,
        MockOperatorExecutionScenarioManifest manifest)
    {
        return new CommandContextContract(
            new CommandIdentityContract(commandId, correlationId, idempotencyKey),
            new CommandOriginContract(manifest.DefaultActorId, BffChannelValues.Wpf, manifest.StationId),
            new DateTimeOffset(2026, 4, 17, 13, 5, 0, TimeSpan.Zero),
            null);
    }

    /// <summary>
    /// example smoke가 끝나면 host와 client를 함께 정리합니다.
    /// </summary>
    /// <param name="Application">실행 중인 테스트용 web application입니다.</param>
    /// <param name="Client">해당 host와 연결된 HTTP client입니다.</param>
    private sealed record StartedExampleApplication(
        WebApplication Application,
        HttpClient Client) : IAsyncDisposable
    {
        /// <summary>
        /// host와 HTTP client를 비동기로 정리합니다.
        /// </summary>
        /// <returns>비동기 정리 작업입니다.</returns>
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Application.StopAsync();
            await Application.DisposeAsync();
        }
    }
}
