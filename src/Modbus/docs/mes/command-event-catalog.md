# MES Command and Event Catalog

## 1. 목적

이 문서는 Release 1 MES pilot에서 사용하는 command, domain event, workflow event,
integration event vocabulary를 현재 실행 가능 범위와 후속 계획 범위로 나누어 정리한다.

이 문서의 목적은 다음과 같다.

- WPF, Web, Edge, integration이 같은 machine-facing command 이름을 사용하도록 기준을 고정한다.
- 현재 코드로 실행 가능한 command와 아직 설계 단계에 머무는 command를 분리한다.
- idempotency, audit, outbox, integration contract가 어떤 command와 event를 기준으로 움직이는지 명확히 한다.
- `Mes.Domain`, `Mes.Application.Contracts`, `Mes.ExperienceApi`, WPF station shell이 같은 vocabulary를 사용하도록 맞춘다.

## 2. 범위와 기준

- 모든 business command의 authoritative entry point는 `Experience API / BFF`다.
- 이 문서는 Release 1 operator-execution slice를 중심으로 작성한다.
- 현재 실행 가능 범위는 `src/Mes.Application.Contracts/OperatorExecution/`와 `src/Mes.ExperienceApi/OperatorExecution/`에 실제 shared contract와 route가 존재하는 command만 뜻한다.
- 아직 shared contract가 없는 command는 catalog에 남기더라도 설계 또는 후속 범위로만 취급한다.
- machine-facing 상태명과 command명은 prose variant가 아니라 현재 코드 spelling을 그대로 사용한다.

### 2.1 현재 실행 가능한 BFF command

- `start-operation`
- `record-material-scan`
- `record-material-consumption`
- `place-hold`
- `release-hold`
- `record-quality-result`
- `complete-operation`

### 2.2 아직 계획 단계인 command

- `pause-operation`
- `resume-operation`
- `record-material-return`
- `record-scrap`
- `request-override`
- `approve-override`
- `reject-override`
- `acknowledge-discrepancy`
- `resolve-discrepancy`

## 3. Command Envelope

| Field | 설명 |
|---|---|
| `command_id` | 클라이언트가 생성하거나 서버가 수용 후 부여하는 고유 명령 식별자 |
| `command_type` | 예: `start-operation`, `record-material-consumption` |
| `actor_id` | 사용자 또는 system actor 식별자 |
| `channel` | `wpf`, `web`, `integration`, `edge` 중 하나 |
| `station_id` | 현장 station context가 필요한 경우 포함 |
| `correlation_id` | 하나의 작업 흐름을 묶는 상관관계 ID |
| `idempotency_key` | 중복 재시도 방지를 위한 키 |
| `revision_refs` | item, routing, BOM, spec revision 참조 |
| `client_timestamp` | 클라이언트 발생 시각 |
| `server_received_at` | 서버 수신 시각 |

Release 1 idempotency 기준:

- receipt scope는 `channel + command_type + idempotency_key`
- `request_fingerprint`는 canonical business field와 actor, station context에서 생성한다.
- `command_id`, `correlation_id`, `client_timestamp`, `revision_refs`는 transport metadata이며 replay equality를 바꾸지 않는다.

## 4. 현재 실행 가능한 business command catalog

| Command | Primary initiator | Preferred channel | Offline | Preconditions | Success response or workflow output |
|---|---|---|---|---|---|
| `start-operation` | Operator | WPF-first | Conditional | station bound, operation ready, revision valid | `operation-started` |
| `record-material-scan` | Operator | WPF-first | Conditional | station active, WIP and material lot present | `material-scanned`, `material-validation-passed` or `material-validation-failed` |
| `record-material-consumption` | Operator | WPF-first | Conditional | material validated, quantity rule satisfied | `material-consumption-recorded`, `genealogy-link-created` |
| `place-hold` | Operator, quality | WPF-first | Yes | hold subject exists, hold reason required | `hold-placed` |
| `release-hold` | Quality, supervisor | Web-first | No | hold active, release authority satisfied | `hold-released` |
| `record-quality-result` | Quality inspector or station quality step | Shared | Conditional | quality record exists, inspection is pending or in progress | `quality-result-recorded` |
| `complete-operation` | Operator or equipment-assisted station | WPF-first | Conditional | operation running, quality gate open, required evidence present | `operation-completed`, `production-actuals-ready` |

## 5. 후속 설계 범위 business command catalog

이 섹션의 command는 architecture와 roadmap에 남아 있지만 현재 shared contract와 Experience API route는 아직 없다.

| Command | Intended owner | Preferred channel | Why it remains deferred |
|---|---|---|---|
| `pause-operation` | Operator execution slice | WPF-first | domain에는 상태 전이가 있으나 shared contract와 endpoint가 아직 없다 |
| `resume-operation` | Operator execution slice | WPF-first | `pause-operation`과 같은 이유로 delivery 범위가 아직 닫히지 않았다 |
| `record-material-return` | Material reconciliation slice | WPF-first | line-side reconciliation rule이 pilot line 기준으로 아직 고정되지 않았다 |
| `record-scrap` | Execution plus actuals slice | WPF-first | current pilot shell은 completion 중심으로만 actuals를 만든다 |
| `request-override` | Exception management slice | WPF-first | operator-side request UX와 audit policy가 아직 정리되지 않았다 |
| `approve-override` | Exception management slice | Web-first | authority, approval flow, optional e-signature가 외부 검증을 기다린다 |
| `reject-override` | Exception management slice | Web-first | approval flow와 같은 이유 |
| `acknowledge-discrepancy` | WMS-lite reconciliation slice | Web-first | discrepancy lifecycle와 ownership이 pilot line 기준으로 아직 미확정이다 |
| `resolve-discrepancy` | WMS-lite reconciliation slice | Web-first | same as above |

## 6. System and integration commands

| Command | Initiator | Owner | Notes |
|---|---|---|---|
| `ingest-order-release` | ERP integration | MES core | 생산오더 release 수신 |
| `sync-master-data-revision` | Integration API | MES core | item, BOM, routing, spec revision 동기화 |
| `ingest-equipment-completion` | Edge | MES core | raw device event를 validated completion candidate로 전달 |
| `post-production-actuals` | MES core or system | Integration API | good, scrap, consumption, completion 전송 |
| `sync-line-side-material-movement` | MES core or system | Integration API | Release 1 WMS-lite event or batch sync |

## 7. Domain and workflow event catalog

### 7.1 현재 `Mes.Domain`가 직접 raise하는 event

아래 event는 `src/Mes.Domain/Events/DomainEvents.cs`에 실제 타입이 존재한다.
단, 이들 모두가 현재 공개 BFF command에서 직접 발생하는 것은 아니다.

| Event | Current direct source | Meaning |
|---|---|---|
| `order-released-ingested` | `ProductionOrder.Release(...)` | ERP release order가 MES executable state로 들어옴 |
| `operation-started` | `start-operation` | 작업 시작이 authoritative 하게 기록됨 |
| `operation-paused` | future `pause-operation` | 작업 일시정지가 authoritative 하게 기록됨 |
| `operation-resumed` | future `resume-operation` | 일시정지된 작업이 다시 진행됨 |
| `operation-completed` | `complete-operation` | 작업 완료가 authoritative 하게 확정됨 |
| `scrap-recorded` | future `record-scrap` | scrap이 authoritative 하게 기록됨 |
| `material-consumption-recorded` | `record-material-consumption` | 자재 소모가 authoritative 하게 기록됨 |
| `material-return-recorded` | future `record-material-return` | 자재 반납이 authoritative 하게 기록됨 |
| `genealogy-link-created` | `record-material-consumption` | parent-child genealogy link가 생성됨 |
| `hold-placed` | `place-hold`, quality hold propagation | operation 또는 quality subject가 hold 상태로 전환됨 |
| `hold-released` | `release-hold`, quality hold reconciliation | hold가 release됨 |
| `quality-result-recorded` | `record-quality-result` | inspection decision이 authoritative 하게 기록됨 |
| `override-requested` | future `request-override` | 예외 승인 요청이 생성됨 |
| `override-approved` | future `approve-override` | 예외 승인이 확정됨 |
| `override-rejected` | future `reject-override` | 예외 승인 요청이 반려됨 |

### 7.2 현재 application or integration workflow output으로만 남아 있는 event

아래 event는 현재 문서 vocabulary로는 중요하지만, 아직 `Mes.Domain` aggregate event로는 고정되지 않았다.

| Event | Current source | Why it is not a direct domain event yet |
|---|---|---|
| `material-scanned` | `record-material-scan` application handling | scan 자체는 validation workflow entry이고 aggregate mutation이 아닐 수 있다 |
| `material-validation-passed` | `record-material-scan` application handling | authoritative mutation보다 validation result에 가깝다 |
| `material-validation-failed` | `record-material-scan` application handling | same as above |
| `line-side-discrepancy-detected` | reconciliation logic | discrepancy lifecycle가 아직 slice 밖 설계 단계다 |
| `line-side-discrepancy-acknowledged` | future command | current slice 밖이다 |
| `line-side-discrepancy-resolved` | future command | current slice 밖이다 |
| `production-actuals-ready` | completion or consolidation workflow | prepared actuals batch는 application projection 결과다 |

### 7.3 현재 코드 alignment notes

- material usage의 canonical command name은 `record-material-consumption`이다.
- current executable state spelling은 `InProgress`, `PartiallyCompleted`, `Done`, `InProcess`, `InInspection`을 기준으로 유지한다.
- `ProductionOrderStatus.PartiallyCompleted`와 `Completed` 전이는 현재 `complete-operation` acceptance path 안에서 이미 실행된다.

## 8. Integration event catalog

| Event | Direction | Meaning |
|---|---|---|
| `production-actuals-posted-to-erp` | MES -> ERP | good, scrap, completion, consumption이 ERP에 반영됨 |
| `production-actuals-post-failed` | MES -> ERP result | ERP posting 실패 또는 재시도 필요 |
| `material-movement-sent-to-wms-lite` | MES -> WMS | line-side issue, return, discrepancy 관련 최소 정합 이벤트 전송 |
| `material-movement-sync-failed` | MES -> WMS result | WMS-lite 정합 실패 또는 보류 |
| `quality-notification-sent` | MES -> QMS/LIMS | hold, NCR, lab gate 관련 알림 전달 |

## 9. Channel-specific rules

- 같은 business command는 WPF와 Web에서 같은 command name, validation rule, audit semantics를 사용해야 한다.
- `WPF`가 offline mode에서 적재한 command는 `pending replay` 상태로만 취급하며 서버 수용 전에는 authoritative state change가 아니다.
- `Web`은 offline business command를 기본 지원 대상으로 두지 않는다.
- `Edge`는 device event를 전달할 수 있지만 `release-hold`, `approve-override`, `post-production-actuals` 같은 business command authority를 직접 가지지 않는다.

## 10. 최소 happy path mapping

현재 설계 기준 기본 path는 아래다.

1. `ingest-order-release`
2. `start-operation`
3. `record-material-scan`
4. `record-material-consumption`
5. `record-quality-result`
6. `complete-operation`
7. `production-actuals-ready`
8. `post-production-actuals`

주의:

- 현재 shared BFF host는 2번부터 6번까지의 operator-execution command를 실행 가능한 범위로 가진다.
- 1번, 7번, 8번은 broader slice vocabulary이며 provider and integration hardening에 따라 점진적으로 연결된다.

## 11. 최소 exception path mapping

### 11.1 Material mismatch

1. `record-material-scan`
2. `material-validation-failed`
3. optional future `request-override`
4. future `approve-override` or `reject-override`

### 11.2 Quality hold

1. `place-hold`
2. `hold-placed`
3. optional `record-quality-result`
4. `release-hold`
5. `hold-released`

### 11.3 Inventory discrepancy

1. discrepancy detection
2. `line-side-discrepancy-detected`
3. future `acknowledge-discrepancy`
4. future `resolve-discrepancy`

## 12. 현재 실행 경계와 구현에서 남은사항

### 12.1 현재 backend까지 실행 가능한 범위

- `start-operation`
- `record-material-scan`
- `record-material-consumption`
- `place-hold`
- `release-hold`
- `record-quality-result`
- `complete-operation`

### 12.2 현재 WPF shell에 노출된 범위

- `start-operation`
- `record-material-scan`
- `record-material-consumption`
- `complete-operation`

### 12.3 backend에는 있으나 WPF shell에는 아직 없는 범위

- `place-hold`
- `record-quality-result`
- `release-hold`

### 12.4 아직 설계만 있고 구현이 남은 범위

- `pause-operation`, `resume-operation`
- `record-material-return`, `record-scrap`
- override approval lifecycle
- line-side discrepancy lifecycle
- broader ERP, WMS, QMS integration completion

## 13. 다음 정렬 포인트

- pilot line과 representative product family가 정해지면 planned catalog 중 어떤 command가 Release 1로 내려오는지 다시 좁힌다.
- WPF와 Web channel matrix는 이 문서의 executable-now vs planned distinction을 그대로 따라가야 한다.
- 새 command가 code로 promotion되면 이 문서와 `src/Mes.Application.Contracts/OperatorExecution/`를 같은 work unit에서 함께 갱신한다.
