using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Client.Wpf.Configuration;
using Mes.Client.Wpf.OperatorExecution;
using Microsoft.Extensions.Options;

namespace Mes.Client.Wpf.Tests;

/// <summary>
/// WPF 스테이션 command context 생성 정책을 검증합니다.
/// </summary>
public sealed class StationCommandContextFactoryTests
{
    /// <summary>
    /// 동일한 공정에 같은 명령을 보내면 correlation과 idempotency 정책이 안정적으로 유지되는지 검증합니다.
    /// </summary>
    [Fact]
    public void CreateForOperation_WhenSameOperationRequested_KeepsStableCorrelationAndIdempotencyPolicy()
    {
        var factory = CreateFactory();

        var first = factory.CreateForOperation(OperatorExecutionCommandTypes.StartOperation, "ST-1001", "OP-1001");
        var second = factory.CreateForOperation(OperatorExecutionCommandTypes.StartOperation, "ST-1001", "OP-1001");

        Assert.NotEqual(first.Identity.CommandId, second.Identity.CommandId);
        Assert.Equal("operator.demo", first.Origin.ActorId);
        Assert.Equal(BffChannelValues.Wpf, first.Origin.Channel);
        Assert.Equal("ST-1001", first.Origin.StationId);
        Assert.Equal("wpf:ST-1001:OP-1001", first.Identity.CorrelationId);
        Assert.Equal(first.Identity.CorrelationId, second.Identity.CorrelationId);
        Assert.Equal("wpf:start-operation:ST-1001:OP-1001", first.Identity.IdempotencyKey);
        Assert.Equal(first.Identity.IdempotencyKey, second.Identity.IdempotencyKey);
        Assert.Equal(new DateTimeOffset(2026, 4, 17, 9, 0, 0, TimeSpan.Zero), first.ClientTimestamp);
    }

    /// <summary>
    /// 멀티샷 명령에서 action token이 idempotency 범위만 넓히고 correlation은 유지하는지 검증합니다.
    /// </summary>
    [Fact]
    public void CreateForOperation_WhenActionTokenProvided_AppendsOnlyIdempotencyScope()
    {
        var factory = CreateFactory();

        var first = factory.CreateForOperation(
            OperatorExecutionCommandTypes.RecordMaterialConsumption,
            "ST-1001",
            "OP-1001",
            "scan-001");
        var second = factory.CreateForOperation(
            OperatorExecutionCommandTypes.RecordMaterialConsumption,
            "ST-1001",
            "OP-1001",
            "scan-002");

        Assert.Equal("wpf:ST-1001:OP-1001", first.Identity.CorrelationId);
        Assert.Equal(first.Identity.CorrelationId, second.Identity.CorrelationId);
        Assert.Equal(
            "wpf:record-material-consumption:ST-1001:OP-1001:scan-001",
            first.Identity.IdempotencyKey);
        Assert.Equal(
            "wpf:record-material-consumption:ST-1001:OP-1001:scan-002",
            second.Identity.IdempotencyKey);
    }

    /// <summary>
    /// 테스트용 factory를 생성합니다.
    /// </summary>
    /// <returns>고정 시간 기반 command context factory입니다.</returns>
    private static StationCommandContextFactory CreateFactory()
    {
        var options = Options.Create(
            new OperatorExecutionStationClientOptions
            {
                DefaultActorId = "operator.demo"
            });
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 17, 9, 0, 0, TimeSpan.Zero));
        return new StationCommandContextFactory(options, timeProvider);
    }

    /// <summary>
    /// 고정된 시간을 반환하는 테스트용 시간 공급자입니다.
    /// </summary>
    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        /// <summary>
        /// 시간 공급자를 초기화합니다.
        /// </summary>
        /// <param name="utcNow">반환할 UTC 시각입니다.</param>
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow.ToUniversalTime();
        }

        /// <summary>
        /// 고정된 UTC 시각을 반환합니다.
        /// </summary>
        /// <returns>고정된 UTC 시각입니다.</returns>
        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        /// <summary>
        /// 로컬 시간대를 반환합니다.
        /// </summary>
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
