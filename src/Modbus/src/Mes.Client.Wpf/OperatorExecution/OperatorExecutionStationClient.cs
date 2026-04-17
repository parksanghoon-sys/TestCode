using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;

namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// Experience API의 operator-execution BFF를 호출하는 HTTP 클라이언트입니다.
/// </summary>
public sealed class OperatorExecutionStationClient : IOperatorExecutionStationClient
{
    private const string ConnectivityFailureCode = "client.connectivity_failure";
    private const string EmptyResponseCode = "client.empty_response";
    private const string InvalidResponseCode = "client.invalid_response";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    /// <summary>
    /// HTTP 클라이언트를 받아 스테이션 BFF 클라이언트를 초기화합니다.
    /// </summary>
    /// <param name="httpClient">실제 요청을 전송할 HTTP 클라이언트입니다.</param>
    public OperatorExecutionStationClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 지정한 스테이션의 현재 작업 큐를 조회합니다.
    /// </summary>
    /// <param name="request">조회 요청입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 작업 큐 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    public Task<OperatorExecutionStationClientResult<GetStationWorkQueueResponseContract>> GetStationWorkQueueAsync(
        GetStationWorkQueueRequestContract request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var route = OperatorExecutionEndpointSignatures.GetStationWorkQueue.Route.Replace(
            "{stationId}",
            Uri.EscapeDataString(request.StationId),
            StringComparison.Ordinal);

        return SendGetAsync<GetStationWorkQueueResponseContract>(route, cancellationToken);
    }

    /// <summary>
    /// 지정한 공정 실행에 대한 시작 명령을 전송합니다.
    /// </summary>
    /// <param name="command">시작 명령 계약입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 시작 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    public Task<OperatorExecutionStationClientResult<StartOperationResponseContract>> StartOperationAsync(
        StartOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return SendPostAsync<StartOperationCommandContract, StartOperationResponseContract>(
            OperatorExecutionEndpointSignatures.StartOperation.Route,
            command,
            cancellationToken);
    }

    /// <summary>
    /// 지정한 공정 실행에 대한 자재 투입 명령을 전송합니다.
    /// </summary>
    /// <param name="command">자재 투입 명령 계약입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 자재 투입 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    public Task<OperatorExecutionStationClientResult<RecordMaterialConsumptionResponseContract>> RecordMaterialConsumptionAsync(
        RecordMaterialConsumptionCommandContract command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return SendPostAsync<RecordMaterialConsumptionCommandContract, RecordMaterialConsumptionResponseContract>(
            OperatorExecutionEndpointSignatures.RecordMaterialConsumption.Route,
            command,
            cancellationToken);
    }

    /// <summary>
    /// 지정한 공정 실행에 대한 완료 명령을 전송합니다.
    /// </summary>
    /// <param name="command">완료 명령 계약입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 시 완료 응답, 실패 시 오류 정보를 포함한 결과입니다.</returns>
    public Task<OperatorExecutionStationClientResult<CompleteOperationResponseContract>> CompleteOperationAsync(
        CompleteOperationCommandContract command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return SendPostAsync<CompleteOperationCommandContract, CompleteOperationResponseContract>(
            OperatorExecutionEndpointSignatures.CompleteOperation.Route,
            command,
            cancellationToken);
    }

    /// <summary>
    /// GET 요청을 보내고 성공 또는 실패 결과를 공통 형식으로 변환합니다.
    /// </summary>
    /// <typeparam name="TResponse">응답 본문 형식입니다.</typeparam>
    /// <param name="route">호출할 상대 경로입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 또는 실패 결과입니다.</returns>
    private async Task<OperatorExecutionStationClientResult<TResponse>> SendGetAsync<TResponse>(
        string route,
        CancellationToken cancellationToken)
    {
        return await SendAsync<TResponse>(
            token => _httpClient.GetAsync(route, token),
            cancellationToken);
    }

    /// <summary>
    /// POST 요청을 보내고 성공 또는 실패 결과를 공통 형식으로 변환합니다.
    /// </summary>
    /// <typeparam name="TRequest">전송할 요청 형식입니다.</typeparam>
    /// <typeparam name="TResponse">응답 본문 형식입니다.</typeparam>
    /// <param name="route">호출할 상대 경로입니다.</param>
    /// <param name="request">전송할 요청 본문입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 또는 실패 결과입니다.</returns>
    private async Task<OperatorExecutionStationClientResult<TResponse>> SendPostAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken)
    {
        return await SendAsync<TResponse>(
            token => _httpClient.PostAsJsonAsync(route, request, SerializerOptions, token),
            cancellationToken);
    }

    /// <summary>
    /// HTTP 호출을 실행하고 성공 또는 실패 결과를 공통 형식으로 변환합니다.
    /// </summary>
    /// <typeparam name="TResponse">응답 본문 형식입니다.</typeparam>
    /// <param name="sendAsync">실제 HTTP 요청을 수행하는 비동기 함수입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>성공 또는 실패 결과입니다.</returns>
    private async Task<OperatorExecutionStationClientResult<TResponse>> SendAsync<TResponse>(
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await sendAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var responseBody = await ReadSuccessBodyAsync<TResponse>(response, cancellationToken);

                    if (responseBody is null)
                    {
                        return OperatorExecutionStationClientResult<TResponse>.Fail(
                            new OperatorExecutionStationClientFailure(
                                (int)response.StatusCode,
                                null,
                                EmptyResponseCode,
                                "BFF가 비어 있는 성공 응답 본문을 반환했습니다."));
                    }

                    return OperatorExecutionStationClientResult<TResponse>.Success(responseBody);
                }
                catch (NotSupportedException exception)
                {
                    return OperatorExecutionStationClientResult<TResponse>.Fail(
                        new OperatorExecutionStationClientFailure(
                            (int)response.StatusCode,
                            null,
                            InvalidResponseCode,
                            $"BFF 성공 응답 형식을 해석하지 못했습니다. {exception.Message}"));
                }
                catch (JsonException exception)
                {
                    return OperatorExecutionStationClientResult<TResponse>.Fail(
                        new OperatorExecutionStationClientFailure(
                            (int)response.StatusCode,
                            null,
                            InvalidResponseCode,
                            $"BFF 성공 응답 본문을 해석하지 못했습니다. {exception.Message}"));
                }
            }

            var problemDetails = await ReadProblemDetailsAsync(response, cancellationToken);
            return OperatorExecutionStationClientResult<TResponse>.Fail(
                new OperatorExecutionStationClientFailure(
                    (int)response.StatusCode,
                    problemDetails,
                    null,
                    null));
        }
        catch (HttpRequestException exception)
        {
            return OperatorExecutionStationClientResult<TResponse>.Fail(
                new OperatorExecutionStationClientFailure(
                    null,
                    null,
                    ConnectivityFailureCode,
                    $"BFF 연결에 실패했습니다: {exception.Message}"));
        }
    }

    /// <summary>
    /// 성공 응답 본문을 문자열로 읽어 지정한 형식으로 역직렬화합니다.
    /// </summary>
    /// <typeparam name="TResponse">성공 응답 형식입니다.</typeparam>
    /// <param name="response">성공한 HTTP 응답입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>역직렬화된 응답 또는 빈 본문이면 <see langword="null"/>입니다.</returns>
    private static async Task<TResponse?> ReadSuccessBodyAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(rawBody, SerializerOptions);
    }

    /// <summary>
    /// 실패 응답 본문에서 problem details를 읽어들입니다.
    /// </summary>
    /// <param name="response">실패한 HTTP 응답입니다.</param>
    /// <param name="cancellationToken">요청 취소 토큰입니다.</param>
    /// <returns>problem details를 읽으면 해당 값을, 아니면 <see langword="null"/>을 반환합니다.</returns>
    private static async Task<BffProblemDetails?> ReadProblemDetailsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<BffProblemDetails>(
                SerializerOptions,
                cancellationToken);
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
