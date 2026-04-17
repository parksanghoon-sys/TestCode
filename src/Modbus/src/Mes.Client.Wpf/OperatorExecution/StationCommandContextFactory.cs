using Mes.Application.Contracts.Common;
using Mes.Client.Wpf.Configuration;
using Microsoft.Extensions.Options;

namespace Mes.Client.Wpf.OperatorExecution;

/// <summary>
/// WPF 스테이션 명령이 공유하는 command context 생성 정책을 한 곳에 모읍니다.
/// </summary>
public sealed class StationCommandContextFactory
{
    private readonly TimeProvider _timeProvider;
    private readonly string _actorId;

    /// <summary>
    /// 구성과 시간 공급자를 받아 station command context factory를 초기화합니다.
    /// </summary>
    /// <param name="options">WPF station client 구성입니다.</param>
    /// <param name="timeProvider">클라이언트 타임스탬프 공급자입니다.</param>
    public StationCommandContextFactory(
        IOptions<OperatorExecutionStationClientOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
        _actorId = options.Value.ResolveDefaultActorId();
    }

    /// <summary>
    /// 특정 공정 실행 명령에 사용할 공통 command context를 생성합니다.
    /// </summary>
    /// <param name="commandType">생성할 명령 유형입니다.</param>
    /// <param name="stationId">명령을 보내는 스테이션 식별자입니다.</param>
    /// <param name="operationExecutionId">대상 공정 실행 식별자입니다.</param>
    /// <param name="actionToken">multi-shot 명령에서 재시도 범위를 구분할 선택 토큰입니다.</param>
    /// <returns>공유 BFF 계약에 맞는 command context입니다.</returns>
    public CommandContextContract CreateForOperation(
        string commandType,
        string stationId,
        string operationExecutionId,
        string? actionToken = null)
    {
        var normalizedCommandType = NormalizeRequired(commandType, nameof(commandType));
        var normalizedStationId = NormalizeRequired(stationId, nameof(stationId));
        var normalizedOperationExecutionId = NormalizeRequired(operationExecutionId, nameof(operationExecutionId));
        var normalizedActionToken = NormalizeOptional(actionToken);

        return new CommandContextContract(
            new CommandIdentityContract(
                Guid.NewGuid().ToString("N"),
                BuildCorrelationId(normalizedStationId, normalizedOperationExecutionId),
                BuildIdempotencyKey(
                    normalizedCommandType,
                    normalizedStationId,
                    normalizedOperationExecutionId,
                    normalizedActionToken)),
            new CommandOriginContract(_actorId, BffChannelValues.Wpf, normalizedStationId),
            _timeProvider.GetLocalNow(),
            null);
    }

    /// <summary>
    /// 같은 공정 실행 conversation을 묶는 correlation id를 생성합니다.
    /// </summary>
    /// <param name="stationId">스테이션 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <returns>재현 가능한 correlation id입니다.</returns>
    private static string BuildCorrelationId(string stationId, string operationExecutionId)
    {
        return $"wpf:{stationId}:{operationExecutionId}";
    }

    /// <summary>
    /// 같은 명령 재시도를 안전하게 재생할 수 있도록 idempotency key를 생성합니다.
    /// </summary>
    /// <param name="commandType">명령 유형입니다.</param>
    /// <param name="stationId">스테이션 식별자입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <param name="actionToken">multi-shot 명령에서 사용할 선택 action token입니다.</param>
    /// <returns>명령 유형별 idempotency key입니다.</returns>
    private static string BuildIdempotencyKey(
        string commandType,
        string stationId,
        string operationExecutionId,
        string? actionToken)
    {
        var baseKey = $"wpf:{commandType}:{stationId}:{operationExecutionId}";
        return actionToken is null
            ? baseKey
            : $"{baseKey}:{actionToken}";
    }

    /// <summary>
    /// 필수 문자열을 검증하고 trim 처리합니다.
    /// </summary>
    /// <param name="value">검증할 문자열입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 매개변수 이름입니다.</param>
    /// <returns>trim 처리된 필수 문자열입니다.</returns>
    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Required string value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    /// <summary>
    /// 선택 문자열을 trim 처리된 값으로 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <returns>trim 처리된 문자열 또는 <see langword="null"/>입니다.</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
