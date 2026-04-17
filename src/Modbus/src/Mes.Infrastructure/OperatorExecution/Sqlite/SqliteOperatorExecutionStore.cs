using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mes.Application.Idempotency;
using Mes.Application.OperatorExecution;
using Mes.Application.OperatorExecution.WorkQueue;
using Mes.Domain.Abstractions;
using Mes.Domain.Aggregates;
using Mes.Domain.Entities;
using Mes.Domain.Statuses;
using Mes.Domain.ValueObjects;
using Microsoft.Data.Sqlite;

namespace Mes.Infrastructure.OperatorExecution.Sqlite;

/// <summary>
/// operator-execution 상태를 SQLite 관계형 저장소에 durable 하게 보관합니다.
/// </summary>
public sealed class SqliteOperatorExecutionStore
{
    private const string OutboxSequenceKey = "outbox-sequence";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly string[] SeedDeleteOrder =
    [
        "delete from genealogy_link;",
        "delete from domain_outbox;",
        "delete from production_actuals_batch;",
        "delete from command_receipt;",
        "delete from operation_material_requirement;",
        "delete from quality_record;",
        "delete from material_lot;",
        "delete from wip_unit;",
        "delete from operation_execution;",
        "delete from production_order;"
    ];

    private readonly string _connectionString;
    private readonly object _gate = new();

    /// <summary>
    /// SQLite 저장소를 초기화하고 필요한 스키마를 보장합니다.
    /// </summary>
    /// <param name="options">데이터베이스 파일 경로 옵션입니다.</param>
    public SqliteOperatorExecutionStore(SqliteOperatorExecutionStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DatabaseFilePath))
        {
            throw new ArgumentException("Database file path is required.", nameof(options));
        }

        var databaseFilePath = Path.GetFullPath(options.DatabaseFilePath);
        EnsureParentDirectoryExists(databaseFilePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        InitializeDatabase();
    }

    /// <summary>
    /// inspection 용 receipt 목록을 반환합니다.
    /// </summary>
    public IReadOnlyList<CommandReceiptRecord> Receipts
    {
        get
        {
            lock (_gate)
            {
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    select
                        command_id,
                        command_type,
                        actor_id,
                        channel,
                        station_id,
                        correlation_id,
                        idempotency_key,
                        request_fingerprint,
                        aggregate_type,
                        aggregate_id,
                        accepted_at,
                        result_code,
                        response_json
                    from command_receipt
                    order by accepted_at, command_id;
                    """;

                using var reader = command.ExecuteReader();
                var receipts = new List<CommandReceiptRecord>();
                while (reader.Read())
                {
                    receipts.Add(ReadReceipt(reader));
                }

                return receipts;
            }
        }
    }

    /// <summary>
    /// inspection 용 production actuals batch 목록을 반환합니다.
    /// </summary>
    public IReadOnlyList<PreparedProductionActualsBatch> ProductionActualsBatches
    {
        get
        {
            lock (_gate)
            {
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    select
                        actuals_batch_id,
                        production_order_id,
                        operation_execution_id,
                        good_quantity_value,
                        scrap_quantity_value,
                        quantity_unit,
                        status,
                        prepared_at
                    from production_actuals_batch
                    order by prepared_at, actuals_batch_id;
                    """;

                using var reader = command.ExecuteReader();
                var batches = new List<PreparedProductionActualsBatch>();
                while (reader.Read())
                {
                    batches.Add(ReadPreparedBatch(reader));
                }

                return batches;
            }
        }
    }

    /// <summary>
    /// inspection 용 outbox 항목 목록을 반환합니다.
    /// </summary>
    public IReadOnlyList<SqliteOperatorExecutionOutboxEntry> OutboxEntries
    {
        get
        {
            lock (_gate)
            {
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    select
                        outbox_event_id,
                        aggregate_type,
                        aggregate_id,
                        event_type,
                        occurred_at,
                        payload_json,
                        persisted_at
                    from domain_outbox
                    order by outbox_event_id;
                    """;

                using var reader = command.ExecuteReader();
                var entries = new List<SqliteOperatorExecutionOutboxEntry>();
                while (reader.Read())
                {
                    entries.Add(new SqliteOperatorExecutionOutboxEntry(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        ReadDateTimeOffset(reader, 4),
                        reader.GetString(5),
                        ReadDateTimeOffset(reader, 6)));
                }

                return entries;
            }
        }
    }

    /// <summary>
    /// 테스트나 부트스트랩용 초기 데이터를 SQLite 저장소에 적재합니다.
    /// </summary>
    /// <param name="seed">적재할 초기 데이터입니다.</param>
    public void Seed(SqliteOperatorExecutionSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);

        lock (_gate)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            foreach (var deleteSql in SeedDeleteOrder)
            {
                ExecuteNonQuery(transaction, deleteSql);
            }

            foreach (var order in seed.ProductionOrders)
            {
                UpsertProductionOrder(transaction, order);
            }

            foreach (var operation in seed.OperationExecutions)
            {
                UpsertOperationExecution(transaction, operation);
            }

            foreach (var wipUnit in seed.WipUnits)
            {
                UpsertWipUnit(transaction, wipUnit);
            }

            foreach (var materialLot in seed.MaterialLots)
            {
                UpsertMaterialLot(transaction, materialLot);
            }

            foreach (var qualityRecord in seed.QualityRecords)
            {
                UpsertQualityRecord(transaction, qualityRecord);
            }

            foreach (var requirement in seed.MaterialRequirements)
            {
                UpsertMaterialRequirement(transaction, requirement);
            }

            foreach (var receipt in seed.Receipts)
            {
                UpsertReceipt(transaction, receipt);
            }

            foreach (var batch in seed.ProductionActualsBatches)
            {
                UpsertPreparedBatch(transaction, batch);
            }

            SetOutboxSequence(transaction, 0L);
            transaction.Commit();
        }
    }

    /// <summary>
    /// 자연 유일 scope 기준으로 기존 receipt를 조회합니다.
    /// </summary>
    /// <param name="scope">조회할 receipt scope입니다.</param>
    /// <returns>기존 receipt가 있으면 반환합니다.</returns>
    public CommandReceiptRecord? FindReceipt(CommandReceiptScope scope)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                select
                    command_id,
                    command_type,
                    actor_id,
                    channel,
                    station_id,
                    correlation_id,
                    idempotency_key,
                    request_fingerprint,
                    aggregate_type,
                    aggregate_id,
                    accepted_at,
                    result_code,
                    response_json
                from command_receipt
                where channel = $channel
                  and command_type = $commandType
                  and idempotency_key = $idempotencyKey;
                """;
            command.Parameters.AddWithValue("$channel", scope.Channel);
            command.Parameters.AddWithValue("$commandType", scope.CommandType);
            command.Parameters.AddWithValue("$idempotencyKey", scope.IdempotencyKey);

            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadReceipt(reader) : null;
        }
    }

    /// <summary>
    /// 생산 오더 aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="productionOrderId">생산 오더 식별자입니다.</param>
    /// <returns>복원된 생산 오더 aggregate입니다.</returns>
    public ProductionOrder GetProductionOrder(string productionOrderId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                select
                    production_order_id,
                    item_code,
                    route_revision,
                    status,
                    released_at
                from production_order
                where production_order_id = $productionOrderId;
                """;
            command.Parameters.AddWithValue("$productionOrderId", productionOrderId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw CreateNotFoundException(nameof(productionOrderId), productionOrderId);
            }

            var orderId = reader.GetString(0);
            var operationIds = GetOperationIdsForOrder(connection, orderId);
            return ProductionOrder.Restore(new ProductionOrderRestoreState(
                new ProductionOrderId(orderId),
                reader.GetString(1),
                reader.GetString(2),
                ReadEnum<ProductionOrderStatus>(reader, 3),
                ReadDateTimeOffset(reader, 4),
                operationIds));
        }
    }

    /// <summary>
    /// 공정 실행 aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <returns>복원된 공정 실행 aggregate입니다.</returns>
    public OperationExecution GetOperationExecution(string operationExecutionId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            return GetOperationExecution(connection, operationExecutionId);
        }
    }

    /// <summary>
    /// 생산오더 완료 진행률 판단에 필요한 sibling-operation 요약을 계산합니다.
    /// </summary>
    /// <param name="productionOrderId">요약을 계산할 생산오더 식별자입니다.</param>
    /// <param name="currentOperationExecutionId">현재 완료 처리 중인 공정 실행 식별자입니다.</param>
    /// <returns>완료 진행률 판단에 필요한 최소 요약입니다.</returns>
    public OrderCompletionProgressSnapshot GetOrderCompletionProgress(string productionOrderId, string currentOperationExecutionId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                select
                    count(*) as total_operation_count,
                    sum(
                        case
                            when operation_execution_id <> $currentOperationExecutionId
                             and status <> $completedStatus
                            then 1
                            else 0
                        end) as remaining_open_operation_count
                from operation_execution
                where production_order_id = $productionOrderId;
                """;
            command.Parameters.AddWithValue("$productionOrderId", productionOrderId);
            command.Parameters.AddWithValue("$currentOperationExecutionId", NormalizeRequired(currentOperationExecutionId, nameof(currentOperationExecutionId)));
            command.Parameters.AddWithValue("$completedStatus", OperationExecutionStatus.Done.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw CreateNotFoundException(nameof(productionOrderId), productionOrderId);
            }

            var totalOperationCount = checked((int)reader.GetInt64(0));
            var remainingOpenOperationCount = reader.IsDBNull(1) ? 0 : checked((int)reader.GetInt64(1));

            return new OrderCompletionProgressSnapshot(
                NormalizeRequired(productionOrderId, nameof(productionOrderId)),
                totalOperationCount,
                remainingOpenOperationCount,
                totalOperationCount - remainingOpenOperationCount);
        }
    }

    /// <summary>
    /// WIP 엔티티를 복원해 조회합니다.
    /// </summary>
    /// <param name="wipUnitId">WIP 식별자입니다.</param>
    /// <returns>복원된 WIP 엔티티입니다.</returns>
    public WipUnit GetWipUnit(string wipUnitId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            return GetWipUnit(connection, wipUnitId);
        }
    }

    /// <summary>
    /// 자재 lot aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="materialLotId">자재 lot 식별자입니다.</param>
    /// <returns>복원된 자재 lot aggregate입니다.</returns>
    public MaterialLot GetMaterialLot(string materialLotId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            return GetMaterialLot(connection, materialLotId);
        }
    }

    /// <summary>
    /// 지정한 공정 실행에 연결된 요구 자재 snapshot을 조회합니다.
    /// </summary>
    /// <param name="operationExecutionId">대상 공정 실행 식별자입니다.</param>
    /// <returns>공정 실행 기준 요구 자재 snapshot 목록입니다.</returns>
    public IReadOnlyCollection<OperationMaterialRequirementSnapshot> GetMaterialRequirements(string operationExecutionId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            return LoadMaterialRequirements(connection, operationExecutionId);
        }
    }

    /// <summary>
    /// 품질 기록 aggregate를 복원해 조회합니다.
    /// </summary>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <returns>복원된 품질 기록 aggregate입니다.</returns>
    public QualityRecord GetQualityRecord(string qualityRecordId)
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            return GetQualityRecord(connection, qualityRecordId);
        }
    }

    /// <summary>
    /// 현재 저장소 상태에서 station work queue source를 구성합니다.
    /// </summary>
    /// <returns>MES-side work queue source입니다.</returns>
    public StationWorkQueueSource BuildWorkQueueSource()
    {
        lock (_gate)
        {
            using var connection = OpenConnection();
            return new StationWorkQueueSource(
                LoadOperationExecutions(connection),
                LoadWipUnits(connection),
                LoadQualityRecords(connection),
                LoadMaterialRequirements(connection));
        }
    }

    /// <summary>
    /// aggregate 상태와 receipt, actuals batch, outbox를 하나의 SQLite 트랜잭션으로 커밋합니다.
    /// </summary>
    /// <param name="request">커밋할 변경 묶음입니다.</param>
    internal void Commit(SqliteOperatorExecutionCommitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            foreach (var order in request.ProductionOrders)
            {
                UpsertProductionOrder(transaction, order);
            }

            foreach (var operation in request.OperationExecutions)
            {
                UpsertOperationExecution(transaction, operation);
            }

            foreach (var wipUnit in request.WipUnits)
            {
                UpsertWipUnit(transaction, wipUnit);
            }

            foreach (var materialLot in request.MaterialLots)
            {
                UpsertMaterialLot(transaction, materialLot);
            }

            foreach (var qualityRecord in request.QualityRecords)
            {
                UpsertQualityRecord(transaction, qualityRecord);
            }

            if (request.Receipt is not null)
            {
                UpsertReceipt(transaction, request.Receipt);
            }

            if (request.PreparedBatch is not null)
            {
                UpsertPreparedBatch(transaction, request.PreparedBatch);
            }

            if (request.OutboxEntries.Count > 0)
            {
                var nextSequence = GetOutboxSequence(transaction);

                foreach (var outboxDraft in request.OutboxEntries)
                {
                    nextSequence++;
                    InsertOutboxEntry(transaction, nextSequence, outboxDraft, request.CommittedAt);
                }

                SetOutboxSequence(transaction, nextSequence);
            }

            transaction.Commit();
        }
    }

    /// <summary>
    /// SQLite 전용 JSON 직렬화 옵션을 생성합니다.
    /// </summary>
    /// <returns>저장소 JSON 옵션입니다.</returns>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    /// <summary>
    /// 데이터베이스 파일의 상위 디렉터리를 보장합니다.
    /// </summary>
    /// <param name="databaseFilePath">데이터베이스 파일 경로입니다.</param>
    private static void EnsureParentDirectoryExists(string databaseFilePath)
    {
        var directoryPath = Path.GetDirectoryName(databaseFilePath)
            ?? throw new InvalidOperationException("The database file path must include a parent directory.");
        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// SQLite 연결을 열고 foreign key 검사를 활성화합니다.
    /// </summary>
    /// <returns>열린 SQLite 연결입니다.</returns>
    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "pragma foreign_keys = on;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    /// <summary>
    /// 저장소가 사용하는 첫 관계형 스키마를 생성합니다.
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            create table if not exists production_order
            (
                production_order_id text primary key,
                item_code text not null,
                route_revision text not null,
                status text not null,
                released_at text not null
            );

            create table if not exists operation_execution
            (
                operation_execution_id text primary key,
                production_order_id text not null references production_order (production_order_id),
                operation_sequence integer not null,
                quantity_unit text not null,
                status text not null,
                status_before_hold text null,
                station_id text null,
                hold_reason text null,
                hold_source_type text null,
                hold_source_id text null,
                started_at text null,
                completed_at text null,
                good_quantity_value text not null,
                scrap_quantity_value text not null
            );

            create index if not exists ix_operation_execution_order_status
                on operation_execution (production_order_id, status);

            create index if not exists ix_operation_execution_station_status
                on operation_execution (station_id, status);

            create index if not exists ix_operation_execution_hold_source
                on operation_execution (hold_source_type, hold_source_id, status);

            create table if not exists wip_unit
            (
                wip_unit_id text primary key,
                product_code text not null,
                status text not null,
                current_operation_execution_id text null references operation_execution (operation_execution_id),
                hold_reason text null,
                status_before_hold text null
            );

            create index if not exists ix_wip_unit_operation_status
                on wip_unit (current_operation_execution_id, status);

            create table if not exists material_lot
            (
                material_lot_id text primary key,
                material_code text not null,
                status text not null,
                available_quantity_value text not null,
                available_quantity_unit text not null,
                consumed_quantity_value text not null,
                returned_quantity_value text not null,
                quantity_unit text not null,
                block_reason text null
            );

            create index if not exists ix_material_lot_code_status
                on material_lot (material_code, status);

            create table if not exists genealogy_link
            (
                genealogy_link_id text primary key,
                material_lot_id text not null references material_lot (material_lot_id),
                child_wip_unit_id text not null,
                linked_at text not null,
                unique (material_lot_id, child_wip_unit_id, linked_at)
            );

            create index if not exists ix_genealogy_link_child_wip
                on genealogy_link (child_wip_unit_id, linked_at);

            create table if not exists quality_record
            (
                quality_record_id text primary key,
                wip_unit_id text not null references wip_unit (wip_unit_id),
                inspection_code text not null,
                status text not null,
                decision_status text null,
                hold_reason text null,
                decision_note text null
            );

            create index if not exists ix_quality_record_wip_status
                on quality_record (wip_unit_id, status);

            create table if not exists operation_material_requirement
            (
                operation_material_requirement_id text primary key,
                operation_execution_id text not null references operation_execution (operation_execution_id),
                sequence_no integer not null,
                material_code text not null,
                required_quantity_value text not null,
                required_quantity_unit text not null,
                source_revision_ref text null,
                created_at text not null,
                unique (operation_execution_id, sequence_no)
            );

            create index if not exists ix_operation_material_requirement_execution_material
                on operation_material_requirement (operation_execution_id, material_code);

            create table if not exists command_receipt
            (
                command_id text primary key,
                command_type text not null,
                actor_id text not null,
                channel text not null,
                station_id text null,
                correlation_id text not null,
                idempotency_key text not null,
                request_fingerprint text not null,
                aggregate_type text not null,
                aggregate_id text not null,
                accepted_at text not null,
                result_code text not null,
                response_json text null,
                unique (channel, command_type, idempotency_key)
            );

            create index if not exists ix_command_receipt_aggregate
                on command_receipt (aggregate_type, aggregate_id, accepted_at);

            create table if not exists domain_outbox
            (
                outbox_event_id text primary key,
                aggregate_type text not null,
                aggregate_id text not null,
                event_type text not null,
                occurred_at text not null,
                payload_json text not null,
                persisted_at text not null,
                published_at text null,
                publish_attempt_count integer not null default 0
            );

            create index if not exists ix_domain_outbox_publish
                on domain_outbox (published_at, event_type, occurred_at);

            create table if not exists production_actuals_batch
            (
                actuals_batch_id text primary key,
                production_order_id text not null references production_order (production_order_id),
                operation_execution_id text not null references operation_execution (operation_execution_id),
                good_quantity_value text not null,
                scrap_quantity_value text not null,
                quantity_unit text not null,
                status text not null,
                prepared_at text not null
            );

            create index if not exists ix_production_actuals_batch_execution_status
                on production_actuals_batch (operation_execution_id, status, prepared_at);

            create table if not exists store_metadata
            (
                metadata_key text primary key,
                metadata_value text not null
            );

            insert or ignore into store_metadata (metadata_key, metadata_value)
            values ('outbox-sequence', '0');
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 생산 오더에 연결된 공정 실행 식별자 목록을 순서대로 읽습니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="productionOrderId">생산 오더 식별자입니다.</param>
    /// <returns>공정 실행 식별자 목록입니다.</returns>
    private static IReadOnlyCollection<OperationExecutionId> GetOperationIdsForOrder(
        SqliteConnection connection,
        string productionOrderId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select operation_execution_id
            from operation_execution
            where production_order_id = $productionOrderId
            order by operation_sequence, operation_execution_id;
            """;
        command.Parameters.AddWithValue("$productionOrderId", productionOrderId);

        using var reader = command.ExecuteReader();
        var operationIds = new List<OperationExecutionId>();
        while (reader.Read())
        {
            operationIds.Add(new OperationExecutionId(reader.GetString(0)));
        }

        return operationIds;
    }

    /// <summary>
    /// 연결에서 단일 공정 실행 aggregate를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="operationExecutionId">공정 실행 식별자입니다.</param>
    /// <returns>복원된 공정 실행 aggregate입니다.</returns>
    private static OperationExecution GetOperationExecution(SqliteConnection connection, string operationExecutionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                operation_execution_id,
                production_order_id,
                operation_sequence,
                quantity_unit,
                status,
                status_before_hold,
                station_id,
                hold_reason,
                hold_source_type,
                hold_source_id,
                started_at,
                completed_at,
                good_quantity_value,
                scrap_quantity_value
            from operation_execution
            where operation_execution_id = $operationExecutionId;
            """;
        command.Parameters.AddWithValue("$operationExecutionId", operationExecutionId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw CreateNotFoundException(nameof(operationExecutionId), operationExecutionId);
        }

        return ReadOperationExecution(reader);
    }

    /// <summary>
    /// 연결에서 단일 WIP 엔티티를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="wipUnitId">WIP 식별자입니다.</param>
    /// <returns>복원된 WIP 엔티티입니다.</returns>
    private static WipUnit GetWipUnit(SqliteConnection connection, string wipUnitId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                wip_unit_id,
                product_code,
                status,
                current_operation_execution_id,
                hold_reason,
                status_before_hold
            from wip_unit
            where wip_unit_id = $wipUnitId;
            """;
        command.Parameters.AddWithValue("$wipUnitId", wipUnitId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw CreateNotFoundException(nameof(wipUnitId), wipUnitId);
        }

        return ReadWipUnit(reader);
    }

    /// <summary>
    /// 연결에서 단일 자재 lot aggregate를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="materialLotId">자재 lot 식별자입니다.</param>
    /// <returns>복원된 자재 lot aggregate입니다.</returns>
    private static MaterialLot GetMaterialLot(SqliteConnection connection, string materialLotId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                material_lot_id,
                material_code,
                status,
                available_quantity_value,
                available_quantity_unit,
                consumed_quantity_value,
                returned_quantity_value,
                block_reason
            from material_lot
            where material_lot_id = $materialLotId;
            """;
        command.Parameters.AddWithValue("$materialLotId", materialLotId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw CreateNotFoundException(nameof(materialLotId), materialLotId);
        }

        var lotId = reader.GetString(0);
        var unit = reader.GetString(4);
        return MaterialLot.Restore(new MaterialLotRestoreState(
            new MaterialLotId(lotId),
            reader.GetString(1),
            ReadEnum<MaterialLotStatus>(reader, 2),
            ReadMeasuredQuantity(reader, 3, 4),
            ReadMeasuredQuantity(reader, 5, unit),
            ReadMeasuredQuantity(reader, 6, unit),
            ReadOptionalString(reader, 7),
            GetGenealogyLinks(connection, lotId)));
    }

    /// <summary>
    /// 연결에서 단일 품질 기록 aggregate를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="qualityRecordId">품질 기록 식별자입니다.</param>
    /// <returns>복원된 품질 기록 aggregate입니다.</returns>
    private static QualityRecord GetQualityRecord(SqliteConnection connection, string qualityRecordId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                quality_record_id,
                wip_unit_id,
                inspection_code,
                status,
                decision_status,
                hold_reason,
                decision_note
            from quality_record
            where quality_record_id = $qualityRecordId;
            """;
        command.Parameters.AddWithValue("$qualityRecordId", qualityRecordId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw CreateNotFoundException(nameof(qualityRecordId), qualityRecordId);
        }

        return ReadQualityRecord(reader);
    }

    /// <summary>
    /// 모든 공정 실행 aggregate를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <returns>공정 실행 목록입니다.</returns>
    private static IReadOnlyCollection<OperationExecution> LoadOperationExecutions(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                operation_execution_id,
                production_order_id,
                operation_sequence,
                quantity_unit,
                status,
                status_before_hold,
                station_id,
                hold_reason,
                hold_source_type,
                hold_source_id,
                started_at,
                completed_at,
                good_quantity_value,
                scrap_quantity_value
            from operation_execution
            order by operation_sequence, operation_execution_id;
            """;

        using var reader = command.ExecuteReader();
        var operations = new List<OperationExecution>();
        while (reader.Read())
        {
            operations.Add(ReadOperationExecution(reader));
        }

        return operations;
    }

    /// <summary>
    /// 모든 WIP 엔티티를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <returns>WIP 목록입니다.</returns>
    private static IReadOnlyCollection<WipUnit> LoadWipUnits(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                wip_unit_id,
                product_code,
                status,
                current_operation_execution_id,
                hold_reason,
                status_before_hold
            from wip_unit
            order by wip_unit_id;
            """;

        using var reader = command.ExecuteReader();
        var wipUnits = new List<WipUnit>();
        while (reader.Read())
        {
            wipUnits.Add(ReadWipUnit(reader));
        }

        return wipUnits;
    }

    /// <summary>
    /// 모든 품질 기록 aggregate를 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <returns>품질 기록 목록입니다.</returns>
    private static IReadOnlyCollection<QualityRecord> LoadQualityRecords(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                quality_record_id,
                wip_unit_id,
                inspection_code,
                status,
                decision_status,
                hold_reason,
                decision_note
            from quality_record
            order by quality_record_id;
            """;

        using var reader = command.ExecuteReader();
        var qualityRecords = new List<QualityRecord>();
        while (reader.Read())
        {
            qualityRecords.Add(ReadQualityRecord(reader));
        }

        return qualityRecords;
    }

    /// <summary>
    /// 모든 자재 요구 snapshot을 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <returns>자재 요구 snapshot 목록입니다.</returns>
    private static IReadOnlyCollection<OperationMaterialRequirementSnapshot> LoadMaterialRequirements(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                operation_material_requirement_id,
                operation_execution_id,
                sequence_no,
                material_code,
                required_quantity_value,
                required_quantity_unit,
                source_revision_ref,
                created_at
            from operation_material_requirement
            order by sequence_no, operation_material_requirement_id;
            """;

        using var reader = command.ExecuteReader();
        var requirements = new List<OperationMaterialRequirementSnapshot>();
        while (reader.Read())
        {
            requirements.Add(new OperationMaterialRequirementSnapshot(
                reader.GetString(0),
                new OperationExecutionId(reader.GetString(1)),
                reader.GetString(3),
                ReadMeasuredQuantity(reader, 4, 5),
                new OperationMaterialRequirementMetadata(
                    reader.GetInt32(2),
                    ReadOptionalString(reader, 6),
                    ReadDateTimeOffset(reader, 7))));
        }

        return requirements;
    }

    /// <summary>
    /// 지정한 공정 실행에 연결된 요구 자재 snapshot만 복원합니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="operationExecutionId">대상 공정 실행 식별자입니다.</param>
    /// <returns>공정 실행 기준 요구 자재 snapshot 목록입니다.</returns>
    private static IReadOnlyCollection<OperationMaterialRequirementSnapshot> LoadMaterialRequirements(
        SqliteConnection connection,
        string operationExecutionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select
                operation_material_requirement_id,
                operation_execution_id,
                sequence_no,
                material_code,
                required_quantity_value,
                required_quantity_unit,
                source_revision_ref,
                created_at
            from operation_material_requirement
            where operation_execution_id = $operationExecutionId
            order by sequence_no, operation_material_requirement_id;
            """;
        command.Parameters.AddWithValue("$operationExecutionId", operationExecutionId);

        using var reader = command.ExecuteReader();
        var requirements = new List<OperationMaterialRequirementSnapshot>();
        while (reader.Read())
        {
            requirements.Add(new OperationMaterialRequirementSnapshot(
                reader.GetString(0),
                new OperationExecutionId(reader.GetString(1)),
                reader.GetString(3),
                ReadMeasuredQuantity(reader, 4, 5),
                new OperationMaterialRequirementMetadata(
                    reader.GetInt32(2),
                    ReadOptionalString(reader, 6),
                    ReadDateTimeOffset(reader, 7))));
        }

        return requirements;
    }

    /// <summary>
    /// genealogy link를 lot 기준으로 읽습니다.
    /// </summary>
    /// <param name="connection">열린 SQLite 연결입니다.</param>
    /// <param name="materialLotId">부모 자재 lot 식별자입니다.</param>
    /// <returns>해당 lot의 genealogy link 목록입니다.</returns>
    private static IReadOnlyCollection<GenealogyLink> GetGenealogyLinks(SqliteConnection connection, string materialLotId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            select material_lot_id, child_wip_unit_id, linked_at
            from genealogy_link
            where material_lot_id = $materialLotId
            order by linked_at, genealogy_link_id;
            """;
        command.Parameters.AddWithValue("$materialLotId", materialLotId);

        using var reader = command.ExecuteReader();
        var links = new List<GenealogyLink>();
        while (reader.Read())
        {
            links.Add(new GenealogyLink(
                new MaterialLotId(reader.GetString(0)),
                new WipUnitId(reader.GetString(1)),
                ReadDateTimeOffset(reader, 2)));
        }

        return links;
    }

    /// <summary>
    /// operation_execution row를 aggregate로 변환합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <returns>복원된 공정 실행 aggregate입니다.</returns>
    private static OperationExecution ReadOperationExecution(SqliteDataReader reader)
    {
        return OperationExecution.Restore(new OperationExecutionRestoreState(
            new OperationExecutionId(reader.GetString(0)),
            new ProductionOrderId(reader.GetString(1)),
            reader.GetInt32(2),
            reader.GetString(3),
            ReadEnum<OperationExecutionStatus>(reader, 4),
            ReadOptionalIdentifier(reader, 6, static value => new StationId(value)),
            ReadOptionalString(reader, 7),
            ReadNullableEnum<OperationExecutionStatus>(reader, 5),
            ReadOptionalString(reader, 8),
            ReadOptionalString(reader, 9),
            ReadNullableDateTimeOffset(reader, 10),
            ReadNullableDateTimeOffset(reader, 11),
            ReadMeasuredQuantity(reader, 12, 3),
            ReadMeasuredQuantity(reader, 13, 3)));
    }

    /// <summary>
    /// wip_unit row를 엔티티로 변환합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <returns>복원된 WIP 엔티티입니다.</returns>
    private static WipUnit ReadWipUnit(SqliteDataReader reader)
    {
        return WipUnit.Restore(new WipUnitRestoreState(
            new WipUnitId(reader.GetString(0)),
            reader.GetString(1),
            ReadEnum<WipUnitStatus>(reader, 2),
            ReadOptionalIdentifier(reader, 3, static value => new OperationExecutionId(value)),
            ReadOptionalString(reader, 4),
            ReadNullableEnum<WipUnitStatus>(reader, 5)));
    }

    /// <summary>
    /// quality_record row를 aggregate로 변환합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <returns>복원된 품질 기록 aggregate입니다.</returns>
    private static QualityRecord ReadQualityRecord(SqliteDataReader reader)
    {
        return QualityRecord.Restore(new QualityRecordRestoreState(
            new QualityRecordId(reader.GetString(0)),
            new WipUnitId(reader.GetString(1)),
            reader.GetString(2),
            ReadEnum<QualityRecordStatus>(reader, 3),
            ReadNullableEnum<QualityDecisionStatus>(reader, 4),
            ReadOptionalString(reader, 5),
            ReadOptionalString(reader, 6)));
    }

    /// <summary>
    /// command receipt row를 record로 변환합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <returns>복원된 command receipt입니다.</returns>
    private static CommandReceiptRecord ReadReceipt(SqliteDataReader reader)
    {
        return new CommandReceiptRecord(
            reader.GetString(0),
            new CommandReceiptScope(
                reader.GetString(3),
                reader.GetString(1),
                reader.GetString(6)),
            reader.GetString(2),
            ReadOptionalString(reader, 4),
            reader.GetString(5),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            ReadDateTimeOffset(reader, 10),
            reader.GetString(11),
            ReadOptionalString(reader, 12));
    }

    /// <summary>
    /// production actuals batch row를 record로 변환합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <returns>복원된 batch record입니다.</returns>
    private static PreparedProductionActualsBatch ReadPreparedBatch(SqliteDataReader reader)
    {
        return new PreparedProductionActualsBatch(
            reader.GetString(0),
            new ProductionOrderId(reader.GetString(1)),
            new OperationExecutionId(reader.GetString(2)),
            ReadMeasuredQuantity(reader, 3, 5),
            ReadMeasuredQuantity(reader, 4, 5),
            reader.GetString(6),
            ReadDateTimeOffset(reader, 7));
    }

    /// <summary>
    /// 생산 오더를 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="order">저장할 생산 오더입니다.</param>
    private static void UpsertProductionOrder(SqliteTransaction transaction, ProductionOrder order)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into production_order (
                production_order_id,
                item_code,
                route_revision,
                status,
                released_at)
            values (
                $productionOrderId,
                $itemCode,
                $routeRevision,
                $status,
                $releasedAt)
            on conflict (production_order_id) do update set
                item_code = excluded.item_code,
                route_revision = excluded.route_revision,
                status = excluded.status,
                released_at = excluded.released_at;
            """;
        command.Parameters.AddWithValue("$productionOrderId", order.Id.ToString());
        command.Parameters.AddWithValue("$itemCode", order.ItemCode);
        command.Parameters.AddWithValue("$routeRevision", order.RouteRevision);
        command.Parameters.AddWithValue("$status", order.Status.ToString());
        command.Parameters.AddWithValue("$releasedAt", ToText(order.ReleasedAt));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 공정 실행을 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="operation">저장할 공정 실행입니다.</param>
    private static void UpsertOperationExecution(SqliteTransaction transaction, OperationExecution operation)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into operation_execution (
                operation_execution_id,
                production_order_id,
                operation_sequence,
                quantity_unit,
                status,
                status_before_hold,
                station_id,
                hold_reason,
                hold_source_type,
                hold_source_id,
                started_at,
                completed_at,
                good_quantity_value,
                scrap_quantity_value)
            values (
                $operationExecutionId,
                $productionOrderId,
                $operationSequence,
                $quantityUnit,
                $status,
                $statusBeforeHold,
                $stationId,
                $holdReason,
                $holdSourceType,
                $holdSourceId,
                $startedAt,
                $completedAt,
                $goodQuantityValue,
                $scrapQuantityValue)
            on conflict (operation_execution_id) do update set
                production_order_id = excluded.production_order_id,
                operation_sequence = excluded.operation_sequence,
                quantity_unit = excluded.quantity_unit,
                status = excluded.status,
                status_before_hold = excluded.status_before_hold,
                station_id = excluded.station_id,
                hold_reason = excluded.hold_reason,
                hold_source_type = excluded.hold_source_type,
                hold_source_id = excluded.hold_source_id,
                started_at = excluded.started_at,
                completed_at = excluded.completed_at,
                good_quantity_value = excluded.good_quantity_value,
                scrap_quantity_value = excluded.scrap_quantity_value;
            """;
        command.Parameters.AddWithValue("$operationExecutionId", operation.Id.ToString());
        command.Parameters.AddWithValue("$productionOrderId", operation.ProductionOrderId.ToString());
        command.Parameters.AddWithValue("$operationSequence", operation.OperationSequence);
        command.Parameters.AddWithValue("$quantityUnit", operation.QuantityUnit);
        command.Parameters.AddWithValue("$status", operation.Status.ToString());
        command.Parameters.AddWithValue("$statusBeforeHold", ToDbValue(operation.StatusBeforeHold?.ToString()));
        command.Parameters.AddWithValue("$stationId", ToDbValue(operation.StationId?.ToString()));
        command.Parameters.AddWithValue("$holdReason", ToDbValue(operation.HoldReason));
        command.Parameters.AddWithValue("$holdSourceType", ToDbValue(operation.HoldSourceType));
        command.Parameters.AddWithValue("$holdSourceId", ToDbValue(operation.HoldSourceId));
        command.Parameters.AddWithValue("$startedAt", ToDbValue(ToText(operation.StartedAt)));
        command.Parameters.AddWithValue("$completedAt", ToDbValue(ToText(operation.CompletedAt)));
        command.Parameters.AddWithValue("$goodQuantityValue", ToText(operation.GoodQuantity.Value));
        command.Parameters.AddWithValue("$scrapQuantityValue", ToText(operation.ScrapQuantity.Value));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// WIP 엔티티를 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="wipUnit">저장할 WIP 엔티티입니다.</param>
    private static void UpsertWipUnit(SqliteTransaction transaction, WipUnit wipUnit)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into wip_unit (
                wip_unit_id,
                product_code,
                status,
                current_operation_execution_id,
                hold_reason,
                status_before_hold)
            values (
                $wipUnitId,
                $productCode,
                $status,
                $currentOperationExecutionId,
                $holdReason,
                $statusBeforeHold)
            on conflict (wip_unit_id) do update set
                product_code = excluded.product_code,
                status = excluded.status,
                current_operation_execution_id = excluded.current_operation_execution_id,
                hold_reason = excluded.hold_reason,
                status_before_hold = excluded.status_before_hold;
            """;
        command.Parameters.AddWithValue("$wipUnitId", wipUnit.Id.ToString());
        command.Parameters.AddWithValue("$productCode", wipUnit.ProductCode);
        command.Parameters.AddWithValue("$status", wipUnit.Status.ToString());
        command.Parameters.AddWithValue("$currentOperationExecutionId", ToDbValue(wipUnit.CurrentOperationExecutionId?.ToString()));
        command.Parameters.AddWithValue("$holdReason", ToDbValue(wipUnit.HoldReason));
        command.Parameters.AddWithValue("$statusBeforeHold", ToDbValue(wipUnit.StatusBeforeHold?.ToString()));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 자재 lot과 genealogy link를 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="materialLot">저장할 자재 lot입니다.</param>
    private static void UpsertMaterialLot(SqliteTransaction transaction, MaterialLot materialLot)
    {
        using (var command = transaction.Connection!.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                insert into material_lot (
                    material_lot_id,
                    material_code,
                    status,
                    available_quantity_value,
                    available_quantity_unit,
                    consumed_quantity_value,
                    returned_quantity_value,
                    quantity_unit,
                    block_reason)
                values (
                    $materialLotId,
                    $materialCode,
                    $status,
                    $availableQuantityValue,
                    $availableQuantityUnit,
                    $consumedQuantityValue,
                    $returnedQuantityValue,
                    $quantityUnit,
                    $blockReason)
                on conflict (material_lot_id) do update set
                    material_code = excluded.material_code,
                    status = excluded.status,
                    available_quantity_value = excluded.available_quantity_value,
                    available_quantity_unit = excluded.available_quantity_unit,
                    consumed_quantity_value = excluded.consumed_quantity_value,
                    returned_quantity_value = excluded.returned_quantity_value,
                    quantity_unit = excluded.quantity_unit,
                    block_reason = excluded.block_reason;
                """;
            command.Parameters.AddWithValue("$materialLotId", materialLot.Id.ToString());
            command.Parameters.AddWithValue("$materialCode", materialLot.MaterialCode);
            command.Parameters.AddWithValue("$status", materialLot.Status.ToString());
            command.Parameters.AddWithValue("$availableQuantityValue", ToText(materialLot.AvailableQuantity.Value));
            command.Parameters.AddWithValue("$availableQuantityUnit", materialLot.AvailableQuantity.Unit);
            command.Parameters.AddWithValue("$consumedQuantityValue", ToText(materialLot.ConsumedQuantity.Value));
            command.Parameters.AddWithValue("$returnedQuantityValue", ToText(materialLot.ReturnedQuantity.Value));
            command.Parameters.AddWithValue("$quantityUnit", materialLot.AvailableQuantity.Unit);
            command.Parameters.AddWithValue("$blockReason", ToDbValue(materialLot.BlockReason));
            command.ExecuteNonQuery();
        }

        using (var deleteCommand = transaction.Connection!.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "delete from genealogy_link where material_lot_id = $materialLotId;";
            deleteCommand.Parameters.AddWithValue("$materialLotId", materialLot.Id.ToString());
            deleteCommand.ExecuteNonQuery();
        }

        foreach (var link in materialLot.GenealogyLinks)
        {
            using var insertCommand = transaction.Connection!.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText =
                """
                insert into genealogy_link (
                    genealogy_link_id,
                    material_lot_id,
                    child_wip_unit_id,
                    linked_at)
                values (
                    $genealogyLinkId,
                    $materialLotId,
                    $childWipUnitId,
                    $linkedAt);
                """;
            insertCommand.Parameters.AddWithValue(
                "$genealogyLinkId",
                CreateGenealogyLinkId(link.ParentMaterialLotId, link.ChildWipUnitId, link.LinkedAt));
            insertCommand.Parameters.AddWithValue("$materialLotId", link.ParentMaterialLotId.ToString());
            insertCommand.Parameters.AddWithValue("$childWipUnitId", link.ChildWipUnitId.ToString());
            insertCommand.Parameters.AddWithValue("$linkedAt", ToText(link.LinkedAt));
            insertCommand.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 품질 기록을 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="qualityRecord">저장할 품질 기록입니다.</param>
    private static void UpsertQualityRecord(SqliteTransaction transaction, QualityRecord qualityRecord)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into quality_record (
                quality_record_id,
                wip_unit_id,
                inspection_code,
                status,
                decision_status,
                hold_reason,
                decision_note)
            values (
                $qualityRecordId,
                $wipUnitId,
                $inspectionCode,
                $status,
                $decisionStatus,
                $holdReason,
                $decisionNote)
            on conflict (quality_record_id) do update set
                wip_unit_id = excluded.wip_unit_id,
                inspection_code = excluded.inspection_code,
                status = excluded.status,
                decision_status = excluded.decision_status,
                hold_reason = excluded.hold_reason,
                decision_note = excluded.decision_note;
            """;
        command.Parameters.AddWithValue("$qualityRecordId", qualityRecord.Id.ToString());
        command.Parameters.AddWithValue("$wipUnitId", qualityRecord.WipUnitId.ToString());
        command.Parameters.AddWithValue("$inspectionCode", qualityRecord.InspectionCode);
        command.Parameters.AddWithValue("$status", qualityRecord.Status.ToString());
        command.Parameters.AddWithValue("$decisionStatus", ToDbValue(qualityRecord.DecisionStatus?.ToString()));
        command.Parameters.AddWithValue("$holdReason", ToDbValue(qualityRecord.HoldReason));
        command.Parameters.AddWithValue("$decisionNote", ToDbValue(qualityRecord.DecisionNote));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 자재 요구 snapshot을 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="snapshot">저장할 자재 요구 snapshot입니다.</param>
    private static void UpsertMaterialRequirement(SqliteTransaction transaction, OperationMaterialRequirementSnapshot snapshot)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into operation_material_requirement (
                operation_material_requirement_id,
                operation_execution_id,
                sequence_no,
                material_code,
                required_quantity_value,
                required_quantity_unit,
                source_revision_ref,
                created_at)
            values (
                $operationMaterialRequirementId,
                $operationExecutionId,
                $sequenceNo,
                $materialCode,
                $requiredQuantityValue,
                $requiredQuantityUnit,
                $sourceRevisionRef,
                $createdAt)
            on conflict (operation_material_requirement_id) do update set
                operation_execution_id = excluded.operation_execution_id,
                sequence_no = excluded.sequence_no,
                material_code = excluded.material_code,
                required_quantity_value = excluded.required_quantity_value,
                required_quantity_unit = excluded.required_quantity_unit,
                source_revision_ref = excluded.source_revision_ref,
                created_at = excluded.created_at;
            """;
        command.Parameters.AddWithValue("$operationMaterialRequirementId", snapshot.OperationMaterialRequirementId);
        command.Parameters.AddWithValue("$operationExecutionId", snapshot.OperationExecutionId.ToString());
        command.Parameters.AddWithValue("$sequenceNo", snapshot.Metadata.SequenceNo);
        command.Parameters.AddWithValue("$materialCode", snapshot.MaterialCode);
        command.Parameters.AddWithValue("$requiredQuantityValue", ToText(snapshot.RequiredQuantity.Value));
        command.Parameters.AddWithValue("$requiredQuantityUnit", snapshot.RequiredQuantity.Unit);
        command.Parameters.AddWithValue("$sourceRevisionRef", ToDbValue(snapshot.Metadata.SourceRevisionRef));
        command.Parameters.AddWithValue("$createdAt", ToText(snapshot.Metadata.CreatedAt));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// command receipt를 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="receipt">저장할 receipt입니다.</param>
    private static void UpsertReceipt(SqliteTransaction transaction, CommandReceiptRecord receipt)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into command_receipt (
                command_id,
                command_type,
                actor_id,
                channel,
                station_id,
                correlation_id,
                idempotency_key,
                request_fingerprint,
                aggregate_type,
                aggregate_id,
                accepted_at,
                result_code,
                response_json)
            values (
                $commandId,
                $commandType,
                $actorId,
                $channel,
                $stationId,
                $correlationId,
                $idempotencyKey,
                $requestFingerprint,
                $aggregateType,
                $aggregateId,
                $acceptedAt,
                $resultCode,
                $responseJson)
            on conflict (command_id) do update set
                command_type = excluded.command_type,
                actor_id = excluded.actor_id,
                channel = excluded.channel,
                station_id = excluded.station_id,
                correlation_id = excluded.correlation_id,
                idempotency_key = excluded.idempotency_key,
                request_fingerprint = excluded.request_fingerprint,
                aggregate_type = excluded.aggregate_type,
                aggregate_id = excluded.aggregate_id,
                accepted_at = excluded.accepted_at,
                result_code = excluded.result_code,
                response_json = excluded.response_json;
            """;
        command.Parameters.AddWithValue("$commandId", receipt.CommandId);
        command.Parameters.AddWithValue("$commandType", receipt.Scope.CommandType);
        command.Parameters.AddWithValue("$actorId", receipt.ActorId);
        command.Parameters.AddWithValue("$channel", receipt.Scope.Channel);
        command.Parameters.AddWithValue("$stationId", ToDbValue(receipt.StationId));
        command.Parameters.AddWithValue("$correlationId", receipt.CorrelationId);
        command.Parameters.AddWithValue("$idempotencyKey", receipt.Scope.IdempotencyKey);
        command.Parameters.AddWithValue("$requestFingerprint", receipt.RequestFingerprint);
        command.Parameters.AddWithValue("$aggregateType", receipt.AggregateType);
        command.Parameters.AddWithValue("$aggregateId", receipt.AggregateId);
        command.Parameters.AddWithValue("$acceptedAt", ToText(receipt.AcceptedAt));
        command.Parameters.AddWithValue("$resultCode", receipt.ResultCode);
        command.Parameters.AddWithValue("$responseJson", ToDbValue(receipt.ResponseJson));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// production actuals batch를 upsert 합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="preparedBatch">저장할 batch입니다.</param>
    private static void UpsertPreparedBatch(SqliteTransaction transaction, PreparedProductionActualsBatch preparedBatch)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into production_actuals_batch (
                actuals_batch_id,
                production_order_id,
                operation_execution_id,
                good_quantity_value,
                scrap_quantity_value,
                quantity_unit,
                status,
                prepared_at)
            values (
                $actualsBatchId,
                $productionOrderId,
                $operationExecutionId,
                $goodQuantityValue,
                $scrapQuantityValue,
                $quantityUnit,
                $status,
                $preparedAt)
            on conflict (actuals_batch_id) do update set
                production_order_id = excluded.production_order_id,
                operation_execution_id = excluded.operation_execution_id,
                good_quantity_value = excluded.good_quantity_value,
                scrap_quantity_value = excluded.scrap_quantity_value,
                quantity_unit = excluded.quantity_unit,
                status = excluded.status,
                prepared_at = excluded.prepared_at;
            """;
        command.Parameters.AddWithValue("$actualsBatchId", preparedBatch.ActualsBatchId);
        command.Parameters.AddWithValue("$productionOrderId", preparedBatch.ProductionOrderId.ToString());
        command.Parameters.AddWithValue("$operationExecutionId", preparedBatch.OperationExecutionId.ToString());
        command.Parameters.AddWithValue("$goodQuantityValue", ToText(preparedBatch.GoodQuantity.Value));
        command.Parameters.AddWithValue("$scrapQuantityValue", ToText(preparedBatch.ScrapQuantity.Value));
        command.Parameters.AddWithValue("$quantityUnit", preparedBatch.GoodQuantity.Unit);
        command.Parameters.AddWithValue("$status", preparedBatch.Status);
        command.Parameters.AddWithValue("$preparedAt", ToText(preparedBatch.PreparedAt));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// outbox 초안을 실제 저장 row로 적재합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="outboxSequence">할당된 outbox 시퀀스입니다.</param>
    /// <param name="outboxDraft">저장할 outbox 초안입니다.</param>
    /// <param name="persistedAt">저장 시각입니다.</param>
    private static void InsertOutboxEntry(
        SqliteTransaction transaction,
        long outboxSequence,
        SqliteOperatorExecutionOutboxDraft outboxDraft,
        DateTimeOffset persistedAt)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into domain_outbox (
                outbox_event_id,
                aggregate_type,
                aggregate_id,
                event_type,
                occurred_at,
                payload_json,
                persisted_at,
                published_at,
                publish_attempt_count)
            values (
                $outboxEventId,
                $aggregateType,
                $aggregateId,
                $eventType,
                $occurredAt,
                $payloadJson,
                $persistedAt,
                null,
                0);
            """;
        command.Parameters.AddWithValue("$outboxEventId", $"OUT-{outboxSequence:000000}");
        command.Parameters.AddWithValue("$aggregateType", outboxDraft.AggregateType);
        command.Parameters.AddWithValue("$aggregateId", outboxDraft.AggregateId);
        command.Parameters.AddWithValue("$eventType", outboxDraft.DomainEvent.GetType().Name);
        command.Parameters.AddWithValue("$occurredAt", ToText(outboxDraft.DomainEvent.OccurredAt));
        command.Parameters.AddWithValue("$payloadJson", SerializeDomainEvent(outboxDraft.DomainEvent));
        command.Parameters.AddWithValue("$persistedAt", ToText(persistedAt));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 현재 outbox 시퀀스를 읽습니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <returns>현재 outbox 시퀀스입니다.</returns>
    private static long GetOutboxSequence(SqliteTransaction transaction)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            select metadata_value
            from store_metadata
            where metadata_key = $metadataKey;
            """;
        command.Parameters.AddWithValue("$metadataKey", OutboxSequenceKey);

        var result = command.ExecuteScalar()?.ToString();
        return string.IsNullOrWhiteSpace(result)
            ? 0L
            : long.Parse(result, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// outbox 시퀀스를 갱신합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="outboxSequence">저장할 outbox 시퀀스입니다.</param>
    private static void SetOutboxSequence(SqliteTransaction transaction, long outboxSequence)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into store_metadata (metadata_key, metadata_value)
            values ($metadataKey, $metadataValue)
            on conflict (metadata_key) do update set
                metadata_value = excluded.metadata_value;
            """;
        command.Parameters.AddWithValue("$metadataKey", OutboxSequenceKey);
        command.Parameters.AddWithValue("$metadataValue", outboxSequence.ToString(CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// genealogy link 자연키 기반 식별자를 생성합니다.
    /// </summary>
    /// <param name="materialLotId">부모 자재 lot 식별자입니다.</param>
    /// <param name="wipUnitId">자식 WIP 식별자입니다.</param>
    /// <param name="linkedAt">링크 생성 시각입니다.</param>
    /// <returns>결정적인 genealogy link 식별자입니다.</returns>
    private static string CreateGenealogyLinkId(MaterialLotId materialLotId, WipUnitId wipUnitId, DateTimeOffset linkedAt)
    {
        return $"GEN-{materialLotId}-{wipUnitId}-{linkedAt.UtcDateTime:yyyyMMddHHmmssfff}";
    }

    /// <summary>
    /// domain event를 outbox payload JSON으로 직렬화합니다.
    /// </summary>
    /// <param name="domainEvent">직렬화할 domain event입니다.</param>
    /// <returns>저장 가능한 JSON payload입니다.</returns>
    private static string SerializeDomainEvent(IDomainEvent domainEvent)
    {
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
    }

    /// <summary>
    /// 단일 SQL 문을 실행합니다.
    /// </summary>
    /// <param name="transaction">활성 트랜잭션입니다.</param>
    /// <param name="sql">실행할 SQL입니다.</param>
    private static void ExecuteNonQuery(SqliteTransaction transaction, string sql)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 필수 문자열 입력을 정규화합니다.
    /// </summary>
    /// <param name="value">정규화할 문자열입니다.</param>
    /// <param name="parameterName">예외 메시지에 사용할 파라미터 이름입니다.</param>
    /// <returns>trim 처리된 문자열입니다.</returns>
    private static string NormalizeRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Required string value cannot be empty.", parameterName);
        }

        return value.Trim();
    }

    /// <summary>
    /// SQLite 조회 miss를 operator-execution not-found 예외로 변환합니다.
    /// </summary>
    /// <param name="parameterName">조회에 사용한 식별자 이름입니다.</param>
    /// <param name="key">조회에 사용한 식별자 값입니다.</param>
    /// <returns>host가 problem details로 승격할 수 있는 not-found 예외입니다.</returns>
    private static OperatorExecutionNotFoundException CreateNotFoundException(string parameterName, string key)
    {
        var aggregateType = parameterName switch
        {
            "productionOrderId" => nameof(ProductionOrder),
            "operationExecutionId" or "currentOperationExecutionId" => nameof(OperationExecution),
            "wipUnitId" => nameof(WipUnit),
            "materialLotId" => nameof(MaterialLot),
            "qualityRecordId" => nameof(QualityRecord),
            _ => parameterName
        };

        return new OperatorExecutionNotFoundException(
            $"Could not find {parameterName} '{key}'.",
            new OperatorExecutionErrorContext(
                aggregateType,
                key.Trim(),
                null,
                null));
    }

    /// <summary>
    /// nullable string 값을 데이터베이스 파라미터 값으로 변환합니다.
    /// </summary>
    /// <param name="value">변환할 값입니다.</param>
    /// <returns>SQLite 파라미터에 넣을 값입니다.</returns>
    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DBNull.Value
            : value;
    }

    /// <summary>
    /// nullable timestamp를 텍스트로 변환합니다.
    /// </summary>
    /// <param name="value">변환할 시각입니다.</param>
    /// <returns>ISO-8601 텍스트 또는 <see langword="null"/>입니다.</returns>
    private static string? ToText(DateTimeOffset? value)
    {
        return value?.ToString("O", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// decimal 값을 invariant culture 텍스트로 변환합니다.
    /// </summary>
    /// <param name="value">변환할 decimal 값입니다.</param>
    /// <returns>저장용 decimal 텍스트입니다.</returns>
    private static string ToText(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// timestamp를 ISO-8601 텍스트로 변환합니다.
    /// </summary>
    /// <param name="value">변환할 시각입니다.</param>
    /// <returns>저장용 timestamp 텍스트입니다.</returns>
    private static string ToText(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// reader의 문자열 열에서 enum 값을 읽습니다.
    /// </summary>
    /// <typeparam name="TEnum">읽을 enum 형식입니다.</typeparam>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="ordinal">읽을 열 ordinal입니다.</param>
    /// <returns>파싱된 enum 값입니다.</returns>
    private static TEnum ReadEnum<TEnum>(SqliteDataReader reader, int ordinal)
        where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(reader.GetString(ordinal), ignoreCase: false);
    }

    /// <summary>
    /// reader의 nullable enum 열을 읽습니다.
    /// </summary>
    /// <typeparam name="TEnum">읽을 enum 형식입니다.</typeparam>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="ordinal">읽을 열 ordinal입니다.</param>
    /// <returns>파싱된 nullable enum 값입니다.</returns>
    private static TEnum? ReadNullableEnum<TEnum>(SqliteDataReader reader, int ordinal)
        where TEnum : struct, Enum
    {
        return reader.IsDBNull(ordinal)
            ? null
            : Enum.Parse<TEnum>(reader.GetString(ordinal), ignoreCase: false);
    }

    /// <summary>
    /// reader의 nullable 문자열 열을 읽습니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="ordinal">읽을 열 ordinal입니다.</param>
    /// <returns>문자열 값 또는 <see langword="null"/>입니다.</returns>
    private static string? ReadOptionalString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    /// <summary>
    /// reader의 nullable 문자열 열을 식별자 형식으로 변환합니다.
    /// </summary>
    /// <typeparam name="TIdentifier">만들 식별자 형식입니다.</typeparam>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="ordinal">읽을 열 ordinal입니다.</param>
    /// <param name="factory">문자열에서 식별자를 만드는 팩터리입니다.</param>
    /// <returns>식별자 값 또는 <see langword="null"/>입니다.</returns>
    private static TIdentifier? ReadOptionalIdentifier<TIdentifier>(
        SqliteDataReader reader,
        int ordinal,
        Func<string, TIdentifier> factory)
        where TIdentifier : struct
    {
        return reader.IsDBNull(ordinal)
            ? null
            : factory(reader.GetString(ordinal));
    }

    /// <summary>
    /// reader의 필수 timestamp 열을 읽습니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="ordinal">읽을 열 ordinal입니다.</param>
    /// <returns>파싱된 timestamp입니다.</returns>
    private static DateTimeOffset ReadDateTimeOffset(SqliteDataReader reader, int ordinal)
    {
        return DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <summary>
    /// reader의 nullable timestamp 열을 읽습니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="ordinal">읽을 열 ordinal입니다.</param>
    /// <returns>파싱된 timestamp 또는 <see langword="null"/>입니다.</returns>
    private static DateTimeOffset? ReadNullableDateTimeOffset(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <summary>
    /// value/unit 쌍으로 저장한 측정 수량을 복원합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="valueOrdinal">수량 값 열 ordinal입니다.</param>
    /// <param name="unitOrdinal">수량 단위 열 ordinal입니다.</param>
    /// <returns>복원된 측정 수량입니다.</returns>
    private static MeasuredQuantity ReadMeasuredQuantity(SqliteDataReader reader, int valueOrdinal, int unitOrdinal)
    {
        return new MeasuredQuantity(
            decimal.Parse(reader.GetString(valueOrdinal), CultureInfo.InvariantCulture),
            reader.GetString(unitOrdinal));
    }

    /// <summary>
    /// value 열과 별도 단위 값으로 측정 수량을 복원합니다.
    /// </summary>
    /// <param name="reader">현재 row를 가리키는 reader입니다.</param>
    /// <param name="valueOrdinal">수량 값 열 ordinal입니다.</param>
    /// <param name="unit">적용할 단위 값입니다.</param>
    /// <returns>복원된 측정 수량입니다.</returns>
    private static MeasuredQuantity ReadMeasuredQuantity(SqliteDataReader reader, int valueOrdinal, string unit)
    {
        return new MeasuredQuantity(
            decimal.Parse(reader.GetString(valueOrdinal), CultureInfo.InvariantCulture),
            unit);
    }
}
