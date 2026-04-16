using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.Application.Idempotency;

namespace Mes.Application.Tests;

/// <summary>
/// command receipt idempotency 정책과 fingerprint 규칙을 검증합니다.
/// </summary>
public sealed class CommandReceiptIdempotencyPolicyTests
{
    /// <summary>
    /// 같은 scope 와 같은 fingerprint 면 저장된 응답을 재생하는지 검증합니다.
    /// </summary>
    [Fact]
    public void Evaluate_should_replay_when_scope_and_fingerprint_match()
    {
        var policy = new CommandReceiptIdempotencyPolicy();
        var scope = new CommandReceiptScope("wpf", OperatorExecutionCommandTypes.RecordQualityResult, "KEY-6001");
        var existingReceipt = policy.CreateAcceptedReceipt(
            new RegisterAcceptedCommandReceiptRequest(
                "CMD-6001",
                scope,
                "operator-01",
                "ST-31",
                "CORR-6001",
                "FP-6001",
                "QualityRecord",
                "QR-6001",
                new DateTimeOffset(2026, 4, 16, 14, 0, 0, TimeSpan.Zero),
                "{\"accepted\":true}"));

        var result = policy.Evaluate(new CommandReceiptEvaluationRequest(scope, "FP-6001", existingReceipt));

        Assert.Equal(CommandReceiptDecisionKind.ReplayStored, result.Decision);
        Assert.Same(existingReceipt, result.StoredReceipt);
        Assert.Equal("{\"accepted\":true}", result.StoredReceipt!.ResponseJson);
    }

    /// <summary>
    /// 같은 scope 에서 다른 fingerprint 가 오면 conflict 로 판정되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Evaluate_should_return_conflict_when_fingerprint_differs()
    {
        var policy = new CommandReceiptIdempotencyPolicy();
        var scope = new CommandReceiptScope("wpf", OperatorExecutionCommandTypes.CompleteOperation, "KEY-6002");
        var existingReceipt = policy.CreateAcceptedReceipt(
            new RegisterAcceptedCommandReceiptRequest(
                "CMD-6002",
                scope,
                "operator-02",
                "ST-32",
                "CORR-6002",
                "FP-OLD",
                "OperationExecution",
                "OP-6002",
                new DateTimeOffset(2026, 4, 16, 14, 5, 0, TimeSpan.Zero),
                "{\"accepted\":true}"));

        var result = policy.Evaluate(new CommandReceiptEvaluationRequest(scope, "FP-NEW", existingReceipt));

        Assert.Equal(CommandReceiptDecisionKind.Conflict, result.Decision);
        Assert.Same(existingReceipt, result.StoredReceipt);
        Assert.Equal("FP-NEW", result.IncomingFingerprint);
    }

    /// <summary>
    /// transport 전용 metadata 가 달라도 canonical business field 가 같으면 같은 fingerprint 가 생성되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Fingerprint_should_ignore_transport_only_metadata()
    {
        var builder = new CanonicalCommandFingerprintBuilder();
        var firstCommand = new RecordQualityResultCommandContract(
            new CommandContextContract(
                new CommandIdentityContract("CMD-6101", "CORR-6101", "KEY-6101"),
                new CommandOriginContract("operator-11", BffChannelValues.Wpf, "ST-41"),
                new DateTimeOffset(2026, 4, 16, 14, 10, 0, TimeSpan.Zero),
                new RevisionReferencesContract("ITEM-A", "ROUTE-A", "BOM-A", "SPEC-A")),
            new RecordQualityResultPayloadContract("QR-6101", "WIP-6101", "INSP-41", QualityDecisionValues.Failed, "dimension mismatch"));
        var secondCommand = new RecordQualityResultCommandContract(
            new CommandContextContract(
                new CommandIdentityContract("CMD-6102", "CORR-6102", "KEY-6102"),
                new CommandOriginContract("operator-11", BffChannelValues.Wpf, "ST-41"),
                new DateTimeOffset(2026, 4, 16, 14, 12, 0, TimeSpan.Zero),
                null),
            new RecordQualityResultPayloadContract("QR-6101", "WIP-6101", "INSP-41", QualityDecisionValues.Failed, "dimension mismatch"));

        var firstFingerprint = builder.Build(firstCommand);
        var secondFingerprint = builder.Build(secondCommand);

        Assert.Equal(firstFingerprint, secondFingerprint);
    }

    /// <summary>
    /// business 의미가 같은 기본값 표현은 같은 fingerprint 로 정규화되는지 검증합니다.
    /// </summary>
    [Fact]
    public void Fingerprint_should_normalize_business_defaults()
    {
        var builder = new CanonicalCommandFingerprintBuilder();
        var firstCommand = new StartOperationCommandContract(
            new CommandContextContract(
                new CommandIdentityContract("CMD-6201", "CORR-6201", "KEY-6201"),
                new CommandOriginContract("operator-12", BffChannelValues.Wpf, "ST-42"),
                new DateTimeOffset(2026, 4, 16, 14, 20, 0, TimeSpan.Zero),
                null),
            new StartOperationPayloadContract("PO-6201", "OP-6201", 10, null));
        var secondCommand = new StartOperationCommandContract(
            new CommandContextContract(
                new CommandIdentityContract("CMD-6202", "CORR-6202", "KEY-6202"),
                new CommandOriginContract("operator-12", BffChannelValues.Wpf, "ST-42"),
                new DateTimeOffset(2026, 4, 16, 14, 21, 0, TimeSpan.Zero),
                new RevisionReferencesContract("ITEM-62", "ROUTE-62", null, null)),
            new StartOperationPayloadContract("PO-6201", "OP-6201", 10, "ea"));

        var firstFingerprint = builder.Build(firstCommand);
        var secondFingerprint = builder.Build(secondCommand);

        Assert.Equal(firstFingerprint, secondFingerprint);
        Assert.Equal(
            new CommandReceiptScope("wpf", OperatorExecutionCommandTypes.StartOperation, "KEY-6201"),
            builder.CreateScope(firstCommand));
    }
}
