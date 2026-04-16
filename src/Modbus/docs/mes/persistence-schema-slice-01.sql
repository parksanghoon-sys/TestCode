-- MES Persistence Schema Draft: Slice 01 Operator Execution
-- Target: SQL Server style draft for the first operator execution pilot slice.

create table dbo.production_order
(
    production_order_id nvarchar(64) not null,
    item_code nvarchar(64) not null,
    route_revision nvarchar(64) not null,
    status nvarchar(32) not null,
    released_at datetimeoffset not null,
    closed_at datetimeoffset null,
    constraint pk_production_order primary key (production_order_id)
);
go

create table dbo.operation_execution
(
    operation_execution_id nvarchar(64) not null,
    production_order_id nvarchar(64) not null,
    operation_sequence int not null,
    quantity_unit nvarchar(16) not null,
    status nvarchar(32) not null,
    status_before_hold nvarchar(32) null,
    station_id nvarchar(64) null,
    hold_reason nvarchar(256) null,
    hold_source_type nvarchar(64) null,
    hold_source_id nvarchar(64) null,
    started_at datetimeoffset null,
    completed_at datetimeoffset null,
    good_quantity decimal(18, 6) not null constraint df_operation_execution_good_quantity default (0),
    scrap_quantity decimal(18, 6) not null constraint df_operation_execution_scrap_quantity default (0),
    constraint pk_operation_execution primary key (operation_execution_id),
    constraint fk_operation_execution_production_order
        foreign key (production_order_id) references dbo.production_order (production_order_id)
);
go

create index ix_operation_execution_order_status
    on dbo.operation_execution (production_order_id, status);
go

create index ix_operation_execution_station_status
    on dbo.operation_execution (station_id, status);
go

create index ix_operation_execution_hold_source
    on dbo.operation_execution (hold_source_type, hold_source_id, status);
go

create table dbo.operation_material_requirement
(
    operation_material_requirement_id nvarchar(64) not null,
    operation_execution_id nvarchar(64) not null,
    sequence_no int not null,
    material_code nvarchar(64) not null,
    required_quantity_value decimal(18, 6) not null,
    required_quantity_unit nvarchar(16) not null,
    source_revision_ref nvarchar(64) null,
    created_at datetimeoffset not null,
    constraint pk_operation_material_requirement primary key (operation_material_requirement_id),
    constraint fk_operation_material_requirement_operation_execution
        foreign key (operation_execution_id) references dbo.operation_execution (operation_execution_id),
    constraint uq_operation_material_requirement_natural
        unique (operation_execution_id, sequence_no)
);
go

create index ix_operation_material_requirement_execution_material
    on dbo.operation_material_requirement (operation_execution_id, material_code);
go

create table dbo.wip_unit
(
    wip_unit_id nvarchar(64) not null,
    product_code nvarchar(64) not null,
    status nvarchar(32) not null,
    current_operation_execution_id nvarchar(64) null,
    hold_reason nvarchar(256) null,
    constraint pk_wip_unit primary key (wip_unit_id),
    constraint fk_wip_unit_operation_execution
        foreign key (current_operation_execution_id) references dbo.operation_execution (operation_execution_id)
);
go

create index ix_wip_unit_operation_status
    on dbo.wip_unit (current_operation_execution_id, status);
go

create table dbo.material_lot
(
    material_lot_id nvarchar(64) not null,
    material_code nvarchar(64) not null,
    status nvarchar(32) not null,
    available_quantity_value decimal(18, 6) not null,
    available_quantity_unit nvarchar(16) not null,
    consumed_quantity_value decimal(18, 6) not null constraint df_material_lot_consumed_quantity default (0),
    returned_quantity_value decimal(18, 6) not null constraint df_material_lot_returned_quantity default (0),
    block_reason nvarchar(256) null,
    constraint pk_material_lot primary key (material_lot_id)
);
go

create index ix_material_lot_code_status
    on dbo.material_lot (material_code, status);
go

create table dbo.material_consumption
(
    material_consumption_id nvarchar(64) not null,
    material_lot_id nvarchar(64) not null,
    wip_unit_id nvarchar(64) not null,
    operation_execution_id nvarchar(64) not null,
    quantity_value decimal(18, 6) not null,
    quantity_unit nvarchar(16) not null,
    occurred_at datetimeoffset not null,
    constraint pk_material_consumption primary key (material_consumption_id),
    constraint fk_material_consumption_material_lot
        foreign key (material_lot_id) references dbo.material_lot (material_lot_id),
    constraint fk_material_consumption_wip_unit
        foreign key (wip_unit_id) references dbo.wip_unit (wip_unit_id),
    constraint fk_material_consumption_operation_execution
        foreign key (operation_execution_id) references dbo.operation_execution (operation_execution_id)
);
go

create index ix_material_consumption_traceability
    on dbo.material_consumption (material_lot_id, wip_unit_id, operation_execution_id, occurred_at);
go

create table dbo.genealogy_link
(
    genealogy_link_id nvarchar(64) not null,
    material_lot_id nvarchar(64) not null,
    child_wip_unit_id nvarchar(64) not null,
    linked_at datetimeoffset not null,
    constraint pk_genealogy_link primary key (genealogy_link_id),
    constraint fk_genealogy_link_material_lot
        foreign key (material_lot_id) references dbo.material_lot (material_lot_id),
    constraint fk_genealogy_link_wip_unit
        foreign key (child_wip_unit_id) references dbo.wip_unit (wip_unit_id),
    constraint uq_genealogy_link_natural unique (material_lot_id, child_wip_unit_id, linked_at)
);
go

create index ix_genealogy_link_child_wip
    on dbo.genealogy_link (child_wip_unit_id, linked_at);
go

create table dbo.quality_record
(
    quality_record_id nvarchar(64) not null,
    wip_unit_id nvarchar(64) not null,
    inspection_code nvarchar(64) not null,
    status nvarchar(32) not null,
    decision_status nvarchar(32) null,
    hold_reason nvarchar(256) null,
    decision_note nvarchar(512) null,
    decision_recorded_at datetimeoffset null,
    constraint pk_quality_record primary key (quality_record_id),
    constraint fk_quality_record_wip_unit
        foreign key (wip_unit_id) references dbo.wip_unit (wip_unit_id)
);
go

create index ix_quality_record_wip_status
    on dbo.quality_record (wip_unit_id, status);
go

create table dbo.override_request
(
    override_request_id nvarchar(64) not null,
    operation_execution_id nvarchar(64) not null,
    requested_by nvarchar(64) not null,
    reason nvarchar(512) not null,
    requested_at datetimeoffset not null,
    status nvarchar(32) not null,
    reviewed_by nvarchar(64) null,
    review_note nvarchar(512) null,
    reviewed_at datetimeoffset null,
    constraint pk_override_request primary key (override_request_id),
    constraint fk_override_request_operation_execution
        foreign key (operation_execution_id) references dbo.operation_execution (operation_execution_id)
);
go

create index ix_override_request_operation_status
    on dbo.override_request (operation_execution_id, status);
go

create table dbo.command_receipt
(
    command_id nvarchar(64) not null,
    command_type nvarchar(64) not null,
    actor_id nvarchar(64) not null,
    channel nvarchar(32) not null,
    station_id nvarchar(64) null,
    correlation_id nvarchar(64) not null,
    idempotency_key nvarchar(128) not null,
    request_fingerprint nvarchar(128) not null,
    aggregate_type nvarchar(64) not null,
    aggregate_id nvarchar(64) not null,
    accepted_at datetimeoffset not null,
    result_code nvarchar(64) not null,
    response_json nvarchar(max) null,
    constraint pk_command_receipt primary key (command_id),
    constraint uq_command_receipt_channel_command_idempotency unique (channel, command_type, idempotency_key)
);
go

create index ix_command_receipt_aggregate
    on dbo.command_receipt (aggregate_type, aggregate_id, accepted_at);
go

create table dbo.domain_outbox
(
    outbox_event_id nvarchar(64) not null,
    aggregate_type nvarchar(64) not null,
    aggregate_id nvarchar(64) not null,
    event_type nvarchar(64) not null,
    occurred_at datetimeoffset not null,
    payload_json nvarchar(max) not null,
    published_at datetimeoffset null,
    publish_attempt_count int not null constraint df_domain_outbox_publish_attempt_count default (0),
    constraint pk_domain_outbox primary key (outbox_event_id)
);
go

create index ix_domain_outbox_publish
    on dbo.domain_outbox (published_at, event_type, occurred_at);
go

create table dbo.production_actuals_batch
(
    actuals_batch_id nvarchar(64) not null,
    production_order_id nvarchar(64) not null,
    operation_execution_id nvarchar(64) not null,
    good_quantity decimal(18, 6) not null,
    scrap_quantity decimal(18, 6) not null,
    quantity_unit nvarchar(16) not null,
    status nvarchar(32) not null,
    prepared_at datetimeoffset not null,
    posted_at datetimeoffset null,
    constraint pk_production_actuals_batch primary key (actuals_batch_id),
    constraint fk_production_actuals_batch_production_order
        foreign key (production_order_id) references dbo.production_order (production_order_id),
    constraint fk_production_actuals_batch_operation_execution
        foreign key (operation_execution_id) references dbo.operation_execution (operation_execution_id)
);
go

create index ix_production_actuals_batch_execution_status
    on dbo.production_actuals_batch (operation_execution_id, status, prepared_at);
go
