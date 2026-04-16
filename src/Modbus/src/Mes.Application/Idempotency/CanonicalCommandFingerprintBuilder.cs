using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;

namespace Mes.Application.Idempotency;

/// <summary>
/// operator-execution 명령의 canonical business field fingerprint를 생성합니다.
/// </summary>
public sealed class CanonicalCommandFingerprintBuilder
{
    /// <summary>
    /// `start-operation` 명령의 fingerprint를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 계약입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    public string Build(StartOperationCommandContract command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return BuildFingerprint(
            command,
            writer =>
            {
                writer.Add("production_order_id", command.Payload.ProductionOrderId);
                writer.Add("operation_execution_id", command.Payload.OperationExecutionId);
                writer.Add("operation_sequence", command.Payload.OperationSequence.ToString(CultureInfo.InvariantCulture));
                writer.Add("quantity_unit", NormalizeUnit(command.Payload.QuantityUnit, "EA"));
            });
    }

    /// <summary>
    /// `record-material-consumption` 명령의 fingerprint를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 계약입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    public string Build(RecordMaterialConsumptionCommandContract command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return BuildFingerprint(
            command,
            writer =>
            {
                writer.Add("operation_execution_id", command.Payload.OperationExecutionId);
                writer.Add("wip_unit_id", command.Payload.WipUnitId);
                writer.Add("material_lot_id", command.Payload.MaterialLotId);
                writer.Add("material_code", command.Payload.MaterialCode);
                writer.Add("quantity_value", FormatDecimal(command.Payload.Quantity.Value));
                writer.Add("quantity_unit", NormalizeUnit(command.Payload.Quantity.Unit));
            });
    }

    /// <summary>
    /// `place-hold` 명령의 fingerprint를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 계약입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    public string Build(PlaceHoldCommandContract command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return BuildFingerprint(
            command,
            writer =>
            {
                writer.Add("subject_type", command.Payload.SubjectType);
                writer.Add("subject_id", command.Payload.SubjectId);
                writer.Add("reason", command.Payload.Reason);
            });
    }

    /// <summary>
    /// `release-hold` 명령의 fingerprint를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 계약입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    public string Build(ReleaseHoldCommandContract command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return BuildFingerprint(
            command,
            writer =>
            {
                writer.Add("subject_type", command.Payload.SubjectType);
                writer.Add("subject_id", command.Payload.SubjectId);
                writer.Add("note", command.Payload.Note);
            });
    }

    /// <summary>
    /// `record-quality-result` 명령의 fingerprint를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 계약입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    public string Build(RecordQualityResultCommandContract command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return BuildFingerprint(
            command,
            writer =>
            {
                writer.Add("quality_record_id", command.Payload.QualityRecordId);
                writer.Add("wip_unit_id", command.Payload.WipUnitId);
                writer.Add("inspection_code", command.Payload.InspectionCode);
                writer.Add("decision", command.Payload.Decision);
                writer.Add("note", command.Payload.Note);
            });
    }

    /// <summary>
    /// `complete-operation` 명령의 fingerprint를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 계약입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    public string Build(CompleteOperationCommandContract command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return BuildFingerprint(
            command,
            writer =>
            {
                writer.Add("operation_execution_id", command.Payload.OperationExecutionId);
                writer.Add("good_quantity_value", FormatDecimal(command.Payload.GoodQuantity.Value));
                writer.Add("good_quantity_unit", NormalizeUnit(command.Payload.GoodQuantity.Unit));
                writer.Add("scrap_quantity_value", FormatDecimal(command.Payload.ScrapQuantity?.Value ?? 0m));
                writer.Add("scrap_quantity_unit", NormalizeUnit(command.Payload.ScrapQuantity?.Unit, command.Payload.GoodQuantity.Unit));
                writer.Add("completion_mode", NormalizeOptional(command.Payload.CompletionMode) ?? CompletionModeValues.Manual);
            });
    }

    /// <summary>
    /// 현재 command type에 맞는 receipt scope를 생성합니다.
    /// </summary>
    /// <param name="command">대상 명령 envelope입니다.</param>
    /// <returns>정규화된 receipt scope입니다.</returns>
    public CommandReceiptScope CreateScope<TPayload>(BffCommandEnvelope<TPayload> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return new CommandReceiptScope(
            NormalizeRequired(command.Channel).ToLowerInvariant(),
            NormalizeRequired(command.CommandType),
            NormalizeRequired(command.IdempotencyKey));
    }

    /// <summary>
    /// 공통 envelope 필드와 payload business field를 합쳐 fingerprint를 계산합니다.
    /// </summary>
    /// <param name="command">대상 명령 envelope입니다.</param>
    /// <param name="writePayloadFields">payload business field 작성 함수입니다.</param>
    /// <returns>SHA-256 hex fingerprint입니다.</returns>
    private static string BuildFingerprint<TPayload>(
        BffCommandEnvelope<TPayload> command,
        Action<CanonicalFieldWriter> writePayloadFields)
    {
        var writer = new CanonicalFieldWriter();
        writer.Add("command_type", command.CommandType);
        writer.Add("actor_id", command.ActorId);
        writer.Add("station_id", NormalizeOptional(command.StationId));
        writePayloadFields(writer);
        return writer.ToFingerprint();
    }

    /// <summary>
    /// decimal 값을 culture-invariant 문자열로 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 수량 값입니다.</param>
    /// <returns>정규화된 문자열입니다.</returns>
    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.############################", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 수량 단위를 대문자 canonical 값으로 정규화합니다.
    /// </summary>
    /// <param name="unit">정규화할 단위입니다.</param>
    /// <param name="fallback">입력값이 없을 때 사용할 기본 단위입니다.</param>
    /// <returns>정규화된 단위입니다.</returns>
    private static string NormalizeUnit(string? unit, string? fallback = null)
    {
        return NormalizeRequired(string.IsNullOrWhiteSpace(unit) ? fallback : unit).ToUpperInvariant();
    }

    /// <summary>
    /// 필수 문자열 값을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <returns>trim된 문자열입니다.</returns>
    private static string NormalizeRequired(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Required fingerprint field cannot be empty.", nameof(value));
        }

        return value.Trim();
    }

    /// <summary>
    /// 선택 문자열 값을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <returns>trim된 문자열 또는 <see langword="null"/>입니다.</returns>
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    /// <summary>
    /// canonical fingerprint 입력 필드를 누적하고 hash로 변환합니다.
    /// </summary>
    private sealed class CanonicalFieldWriter
    {
        private readonly List<string> _fields = [];

        /// <summary>
        /// canonical 필드를 하나 추가합니다.
        /// </summary>
        /// <param name="name">필드 이름입니다.</param>
        /// <param name="value">필드 값입니다.</param>
        public void Add(string name, string? value)
        {
            _fields.Add($"{NormalizeRequired(name).ToLowerInvariant()}={NormalizeOptional(value) ?? "<null>"}");
        }

        /// <summary>
        /// 누적된 canonical 필드를 SHA-256 hex fingerprint로 변환합니다.
        /// </summary>
        /// <returns>정규화된 fingerprint 문자열입니다.</returns>
        public string ToFingerprint()
        {
            var canonical = string.Join("|", _fields);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes);
        }
    }
}
