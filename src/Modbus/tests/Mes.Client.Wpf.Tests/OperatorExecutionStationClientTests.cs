using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Client.Wpf.OperatorExecution;

namespace Mes.Client.Wpf.Tests;

/// <summary>
/// 스테이션 BFF HTTP 클라이언트의 응답 정규화와 POST 명령 경로를 검증합니다.
/// </summary>
public sealed class OperatorExecutionStationClientTests
{
    /// <summary>
    /// 성공 응답을 올바른 route로 역직렬화하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task GetStationWorkQueueAsync_WhenSuccessfulResponse_ReturnsSnapshotAndUsesEscapedRoute()
    {
        Uri? requestedUri = null;
        var snapshotTakenAt = new DateTimeOffset(2026, 4, 17, 9, 0, 0, TimeSpan.Zero);
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            requestedUri = request.RequestUri;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""
                        {"stationId":"ST 1001","snapshotTakenAt":"{{snapshotTakenAt:O}}","items":[{"productionOrderId":"PO-1001","operationExecutionId":"OP-1001","operationSequence":10,"stationId":"ST 1001","status":"Queued","operationQuantityUnit":"EA","requiredMaterials":[],"qualityGateState":"open"}]}
                        """,
                        Encoding.UTF8,
                        "application/json")
                });
        });

        var client = CreateClient(handler);

        var result = await client.GetStationWorkQueueAsync(new GetStationWorkQueueRequestContract("ST 1001"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("ST 1001", result.Value!.StationId);
        Assert.Single(result.Value.Items);
        Assert.Equal("EA", result.Value.Items[0].OperationQuantityUnit);
        Assert.Equal(
            "/api/bff/operator-execution/stations/ST%201001/work-queue",
            requestedUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped));
    }

    /// <summary>
    /// 시작 명령이 올바른 POST route와 command context로 전송되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task StartOperationAsync_WhenSuccessfulResponse_PostsCommandToStartRoute()
    {
        Uri? requestedUri = null;
        JsonDocument? requestDocument = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestedUri = request.RequestUri;
            requestDocument = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"accepted":true,"commandId":"cmd-001","serverReceivedAt":"2026-04-17T09:00:01+00:00","operationExecutionId":"OP-1001","status":"Running","startedAt":"2026-04-17T09:00:02+00:00"}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = CreateClient(handler);
        var command = new StartOperationCommandContract(
            CreateCommandContext("cmd-001", "corr-001", "idem-001"),
            new StartOperationPayloadContract("PO-1001", "OP-1001", 10, null));

        var result = await client.StartOperationAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Equal(
            "/api/bff/operator-execution/commands/start-operation",
            requestedUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped));
        Assert.NotNull(requestDocument);
        Assert.Equal("cmd-001", requestDocument!.RootElement.GetProperty("context").GetProperty("identity").GetProperty("commandId").GetString());
        Assert.Equal("operator.demo", requestDocument.RootElement.GetProperty("context").GetProperty("origin").GetProperty("actorId").GetString());
        Assert.Equal("OP-1001", requestDocument.RootElement.GetProperty("payload").GetProperty("operationExecutionId").GetString());
    }

    /// <summary>
    /// 자재 스캔 검증 명령이 올바른 POST route와 payload로 전송되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task RecordMaterialScanAsync_WhenSuccessfulResponse_PostsCommandToScanRoute()
    {
        Uri? requestedUri = null;
        JsonDocument? requestDocument = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestedUri = request.RequestUri;
            requestDocument = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"accepted":true,"commandId":"cmd-002-scan","serverReceivedAt":"2026-04-17T09:02:01+00:00","operationExecutionId":"OP-1001","wipUnitId":"WIP-1001","materialLotId":"LOT-1001","materialCode":"MAT-RED","availableQuantity":{"value":20.0,"unit":"KG"}}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = CreateClient(handler);
        var command = new RecordMaterialScanCommandContract(
            CreateCommandContext("cmd-002-scan", "corr-002-scan", "idem-002-scan"),
            new RecordMaterialScanPayloadContract("OP-1001", "WIP-1001", "LOT-1001", "MAT-RED"));

        var result = await client.RecordMaterialScanAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Equal(
            "/api/bff/operator-execution/commands/material-scan",
            requestedUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped));
        Assert.NotNull(requestDocument);
        Assert.Equal("LOT-1001", requestDocument!.RootElement.GetProperty("payload").GetProperty("materialLotId").GetString());
        Assert.Equal("MAT-RED", requestDocument.RootElement.GetProperty("payload").GetProperty("materialCode").GetString());
    }

    /// <summary>
    /// 자재 투입 명령이 올바른 POST route와 payload로 전송되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task RecordMaterialConsumptionAsync_WhenSuccessfulResponse_PostsCommandToMaterialRoute()
    {
        Uri? requestedUri = null;
        JsonDocument? requestDocument = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestedUri = request.RequestUri;
            requestDocument = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"accepted":true,"commandId":"cmd-003","serverReceivedAt":"2026-04-17T09:05:01+00:00","materialLotId":"LOT-1001","remainingQuantity":{"value":17.5,"unit":"KG"},"genealogyLinkCreated":true}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = CreateClient(handler);
        var command = new RecordMaterialConsumptionCommandContract(
            CreateCommandContext("cmd-003", "corr-003", "idem-003"),
            new RecordMaterialConsumptionPayloadContract(
                "OP-1001",
                "WIP-1001",
                "LOT-1001",
                "MAT-RED",
                new MeasuredQuantityContract(2.5m, "KG")));

        var result = await client.RecordMaterialConsumptionAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(HttpMethod.Post, handler.LastRequestMethod);
        Assert.Equal(
            "/api/bff/operator-execution/commands/material-consumption",
            requestedUri?.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped));
        Assert.NotNull(requestDocument);
        Assert.Equal("LOT-1001", requestDocument!.RootElement.GetProperty("payload").GetProperty("materialLotId").GetString());
        Assert.Equal("MAT-RED", requestDocument.RootElement.GetProperty("payload").GetProperty("materialCode").GetString());
        Assert.Equal(2.5m, requestDocument.RootElement.GetProperty("payload").GetProperty("quantity").GetProperty("value").GetDecimal());
        Assert.Equal("KG", requestDocument.RootElement.GetProperty("payload").GetProperty("quantity").GetProperty("unit").GetString());
    }

    /// <summary>
    /// problem details 실패 응답을 그대로 보존하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task CompleteOperationAsync_WhenProblemDetailsReturned_PreservesServerFailureShape()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent(
                        """
                        {"type":"urn:mes:problem:operator_execution.conflict","title":"Conflict","status":409,"detail":"이미 완료된 작업입니다.","errorCode":"operator_execution.conflict"}
                        """,
                        Encoding.UTF8,
                        "application/json")
                }));

        var client = CreateClient(handler);
        var command = new CompleteOperationCommandContract(
            CreateCommandContext("cmd-002", "corr-002", "idem-002"),
            new CompleteOperationPayloadContract(
                "OP-1001",
                new MeasuredQuantityContract(12.5m, "EA"),
                null,
                CompletionModeValues.Manual));

        var result = await client.CompleteOperationAsync(command);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal(409, result.Failure!.StatusCode);
        Assert.Equal("operator_execution.conflict", result.Failure.ProblemDetails?.GetErrorCode());
        Assert.Null(result.Failure.ClientErrorCode);
        Assert.Null(result.Failure.ClientMessage);
    }

    /// <summary>
    /// 성공 응답 본문이 비어 있으면 empty-response 실패로 정규화되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task GetStationWorkQueueAsync_WhenSuccessfulBodyIsEmpty_ReturnsEmptyResponseFailure()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
                }));

        var client = CreateClient(handler);

        var result = await client.GetStationWorkQueueAsync(new GetStationWorkQueueRequestContract("ST-1001"));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal("client.empty_response", result.Failure!.ClientErrorCode);
        Assert.Contains("비어 있는 성공 응답 본문", result.Failure.ClientMessage);
    }

    /// <summary>
    /// 성공 응답 본문이 잘못된 JSON이면 invalid-response 실패로 정규화되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task GetStationWorkQueueAsync_WhenSuccessfulBodyIsInvalidJson_ReturnsInvalidResponseFailure()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"stationId\":", Encoding.UTF8, "application/json")
                }));

        var client = CreateClient(handler);

        var result = await client.GetStationWorkQueueAsync(new GetStationWorkQueueRequestContract("ST-1001"));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal(200, result.Failure!.StatusCode);
        Assert.Equal("client.invalid_response", result.Failure.ClientErrorCode);
        Assert.Contains("해석하지 못했습니다", result.Failure.ClientMessage);
    }

    /// <summary>
    /// 테스트용 HTTP 클라이언트를 생성합니다.
    /// </summary>
    /// <param name="handler">응답을 흉내 낼 메시지 핸들러입니다.</param>
    /// <returns>테스트용 station client입니다.</returns>
    private static OperatorExecutionStationClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:51398/")
        };

        return new OperatorExecutionStationClient(httpClient);
    }

    /// <summary>
    /// 테스트용 command context를 생성합니다.
    /// </summary>
    /// <param name="commandId">명령 식별자입니다.</param>
    /// <param name="correlationId">상관관계 식별자입니다.</param>
    /// <param name="idempotencyKey">idempotency 키입니다.</param>
    /// <returns>테스트용 command context입니다.</returns>
    private static CommandContextContract CreateCommandContext(
        string commandId,
        string correlationId,
        string idempotencyKey)
    {
        return new CommandContextContract(
            new CommandIdentityContract(commandId, correlationId, idempotencyKey),
            new CommandOriginContract("operator.demo", "wpf", "ST-1001"),
            new DateTimeOffset(2026, 4, 17, 9, 0, 0, TimeSpan.Zero),
            null);
    }

    /// <summary>
    /// 요청별 응답을 제어하는 테스트용 메시지 핸들러입니다.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        /// <summary>
        /// 메시지 핸들러를 초기화합니다.
        /// </summary>
        /// <param name="sendAsync">요청을 처리할 비동기 함수입니다.</param>
        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync ?? throw new ArgumentNullException(nameof(sendAsync));
        }

        /// <summary>
        /// 마지막 요청의 HTTP 메서드를 가져옵니다.
        /// </summary>
        public HttpMethod? LastRequestMethod { get; private set; }

        /// <summary>
        /// 테스트용 HTTP 응답을 반환합니다.
        /// </summary>
        /// <param name="request">수신한 요청입니다.</param>
        /// <param name="cancellationToken">취소 토큰입니다.</param>
        /// <returns>테스트용 HTTP 응답입니다.</returns>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestMethod = request.Method;
            return _sendAsync(request, cancellationToken);
        }
    }
}
