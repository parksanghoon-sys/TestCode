using System.Text.Json;
using Mes.Client.Wpf.OperatorExecution;

namespace Mes.Client.Wpf.Tests;

/// <summary>
/// 스테이션 오류 표시 정책을 검증합니다.
/// </summary>
public sealed class OperatorExecutionProblemDisplayPolicyTests
{
    /// <summary>
    /// conflict 오류가 경고 메시지로 변환되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Map_WhenConflictProblemProvided_ReturnsWarningMessage()
    {
        var policy = new OperatorExecutionProblemDisplayPolicy();
        var failure = new OperatorExecutionStationClientFailure(
            409,
            new BffProblemDetails
            {
                Detail = "이미 완료된 작업입니다.",
                Extensions = new Dictionary<string, JsonElement>
                {
                    ["errorCode"] = JsonSerializer.SerializeToElement("operator_execution.conflict")
                }
            },
            null,
            null);

        var message = policy.Map(failure);

        Assert.Equal("상태 충돌", message.Title);
        Assert.Equal("warning", message.Severity);
        Assert.Equal("이미 완료된 작업입니다.", message.Detail);
    }

    /// <summary>
    /// 연결 실패가 오류 메시지로 변환되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Map_WhenConnectivityFailureProvided_ReturnsConnectionFailureMessage()
    {
        var policy = new OperatorExecutionProblemDisplayPolicy();
        var failure = new OperatorExecutionStationClientFailure(
            null,
            null,
            "client.connectivity_failure",
            "BFF 연결에 실패했습니다: connection refused");

        var message = policy.Map(failure);

        Assert.Equal("연결 실패", message.Title);
        Assert.Equal("error", message.Severity);
        Assert.Equal("BFF 연결에 실패했습니다: connection refused", message.Detail);
    }

    /// <summary>
    /// 성공 응답 해석 실패가 전용 오류 메시지로 변환되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Map_WhenInvalidResponseFailureProvided_ReturnsParsingFailureMessage()
    {
        var policy = new OperatorExecutionProblemDisplayPolicy();
        var failure = new OperatorExecutionStationClientFailure(
            200,
            null,
            "client.invalid_response",
            "BFF 성공 응답 본문을 해석하지 못했습니다.");

        var message = policy.Map(failure);

        Assert.Equal("응답 해석 실패", message.Title);
        Assert.Equal("error", message.Severity);
        Assert.Equal("BFF 성공 응답 본문을 해석하지 못했습니다.", message.Detail);
    }
}
