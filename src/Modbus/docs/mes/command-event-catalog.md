# MES Command and Event Catalog

## 1. 목적

이 문서는 파일럿 Release 1 기준으로 `BFF -> MES Core` command contract와 주요 domain/integration event를 정리한다.

이 문서의 목적은 다음과 같다.

- WPF와 Web이 같은 업무 명령 semantics를 사용하도록 기준선 고정
- command와 event를 분리해 idempotency와 audit을 명확히 함
- BFF, MES core, Edge, ERP/WMS/QMS 간 상호작용을 같은 언어로 정리

## 2. 범위와 기본 원칙

- 범위는 Release 1 pilot workflow 중심이다.
- 모든 business command는 `Experience API / BFF`를 통해 진입한다.
- 모든 command는 `command_id`, `command_type`, `actor_id`, `channel`, `idempotency_key`, `correlation_id`를 가진다.
- 모든 server-accepted command는 최소 1개 이상의 domain event를 발생시킨다.
- equipment/edge에서 들어오는 입력은 raw device event일 수 있지만, authoritative state change는 MES core 검증 후에만 확정된다.

## 3. Command Envelope

| Field | 설명 |
|---|---|
| command_id | 클라이언트가 생성하거나 서버가 수락 시 부여하는 고유 명령 식별자 |
| command_type | 예: `start-operation`, `record-material-consumption` |
| actor_id | 사용자 또는 system actor 식별자 |
| channel | `wpf`, `web`, `integration`, `edge` 중 하나 |
| station_id | 현장 station context가 필요한 경우 포함 |
| correlation_id | 하나의 작업 흐름을 묶는 상관관계 ID |
| idempotency_key | 중복 제출 방지를 위한 키 |
| revision_refs | item, routing, BOM, spec revision 참조 |
| client_timestamp | 클라이언트 발생 시각 |
| server_received_at | 서버 수신 시각 |

## 4. Business Command Catalog

| Command | Primary initiator | Preferred channel | Offline | Preconditions | Success events | External sync |
|---|---|---|---|---|---|---|
| `start-operation` | Operator | WPF-first | Conditional | station bound, order/operation ready, revision valid | `operation-started` | optional APS feedback later |
| `pause-operation` | Operator | WPF-first | Conditional | operation running | `operation-paused` | none |
| `resume-operation` | Operator | WPF-first | Conditional | operation paused | `operation-resumed` | none |
| `complete-operation` | Operator or equipment-assisted station | WPF-first | Conditional | operation running, required scans/results satisfied | `operation-completed`, `production-actuals-ready` | ERP posting |
| `record-material-scan` | Operator | WPF-first | Conditional | station active, material identifier present | `material-scanned`, `material-validation-passed` or `material-validation-failed` | none |
| `record-material-consumption` | Operator | WPF-first | Conditional | material validated, qty rules satisfied | `material-consumption-recorded`, `genealogy-link-created` | WMS-lite sync |
| `record-material-return` | Operator | WPF-first | Conditional | issued material exists | `material-return-recorded` | WMS-lite sync |
| `record-scrap` | Operator or line leader | WPF-first | Conditional | active WIP exists, reason required | `scrap-recorded`, `production-actuals-ready` | ERP posting |
| `place-hold` | Operator, quality | WPF-first | Yes | WIP or operation exists, hold reason required | `hold-placed` | QMS trigger optional |
| `release-hold` | Quality, supervisor | Web-first | No | hold active, approval satisfied | `hold-released` | QMS notification optional |
| `request-override` | Operator, line leader | WPF-first | Conditional | override reason required | `override-requested` | none |
| `approve-override` | Supervisor, quality | Web-first | No | pending override exists, approval authority valid | `override-approved` | optional audit/export |
| `reject-override` | Supervisor, quality | Web-first | No | pending override exists | `override-rejected` | none |
| `acknowledge-discrepancy` | Warehouse, supervisor | Web-first | No | discrepancy exists | `line-side-discrepancy-acknowledged` | WMS-lite reconciliation |
| `resolve-discrepancy` | Warehouse, production support | Web-first | No | discrepancy investigated | `line-side-discrepancy-resolved` | WMS-lite reconciliation |

## 5. System and Integration Commands

| Command | Initiator | Owner | Notes |
|---|---|---|---|
| `ingest-order-release` | ERP integration | MES core | 생산오더 릴리즈 수신 |
| `sync-master-data-revision` | Integration API | MES core | item/BOM/routing/spec revision 동기화 |
| `ingest-equipment-completion` | Edge | MES core | raw device event를 validated completion candidate로 입력 |
| `post-production-actuals` | MES core/system | Integration API | good/scrap/consumption/completion 회신 |
| `sync-line-side-material-movement` | MES core/system | Integration API | Release 1 WMS-lite event or batch sync |

## 6. Domain Event Catalog

| Event | Triggered by | Meaning | Primary consumers |
|---|---|---|---|
| `order-released-ingested` | `ingest-order-release` | ERP 릴리즈 오더가 MES 실행 가능 상태로 수신됨 | orchestration, dispatch |
| `operation-started` | `start-operation` | 작업이 authoritative하게 시작됨 | dispatch, audit, reporting |
| `operation-paused` | `pause-operation` | 작업이 일시중지됨 | dispatch, audit |
| `operation-resumed` | `resume-operation` | 작업이 재개됨 | dispatch, audit |
| `operation-completed` | `complete-operation` | 작업 완료가 authoritative하게 확정됨 | ERP sync, reporting, next step |
| `material-scanned` | `record-material-scan` | 자재 식별자가 입력됨 | validation, audit |
| `material-validation-passed` | `record-material-scan` | 자재가 공정 규칙을 통과함 | station UX, consumption flow |
| `material-validation-failed` | `record-material-scan` | 자재가 공정 규칙에 맞지 않음 | station UX, exception monitoring |
| `material-consumption-recorded` | `record-material-consumption` | 자재 소모가 확정됨 | genealogy, ERP/WMS sync |
| `material-return-recorded` | `record-material-return` | 자재 반납이 확정됨 | WMS-lite sync |
| `genealogy-link-created` | `record-material-consumption` | parent-child genealogy link 생성 | genealogy search, audit |
| `scrap-recorded` | `record-scrap` | scrap이 확정됨 | ERP sync, reporting |
| `hold-placed` | `place-hold` | WIP 또는 operation에 hold가 설정됨 | execution gate, quality, monitoring |
| `hold-released` | `release-hold` | hold가 release되어 다음 진행 가능 | execution gate, quality |
| `override-requested` | `request-override` | 예외 승인 요청이 생성됨 | supervisor web workflow |
| `override-approved` | `approve-override` | 예외 승인이 확정됨 | execution flow, audit |
| `override-rejected` | `reject-override` | 예외 승인이 반려됨 | execution flow, audit |
| `line-side-discrepancy-detected` | reconciliation or validation logic | line-side 재고 차이가 감지됨 | warehouse, supervisor |
| `line-side-discrepancy-acknowledged` | `acknowledge-discrepancy` | discrepancy 대응이 시작됨 | warehouse workflow |
| `line-side-discrepancy-resolved` | `resolve-discrepancy` | discrepancy가 닫힘 | warehouse, audit |
| `production-actuals-ready` | completion/scrap/consumption consolidation | 외부 posting 가능한 actuals 세트 준비됨 | Integration API |

## 7. Integration Event Catalog

| Event | Direction | Meaning |
|---|---|---|
| `production-actuals-posted-to-erp` | MES -> ERP | good/scrap/completion/consumption이 ERP에 반영됨 |
| `production-actuals-post-failed` | MES -> ERP result | ERP posting 실패 또는 재시도 필요 |
| `material-movement-sent-to-wms-lite` | MES -> WMS | line-side issue/return/discrepancy 관련 최소 정산 이벤트 전송 |
| `material-movement-sync-failed` | MES -> WMS result | WMS-lite 정산 실패 또는 보류 |
| `quality-notification-sent` | MES -> QMS/LIMS | hold/NCR/lab gate 관련 알림 전달 |

## 8. Channel-Specific Rules

- `WPF`와 `Web`가 같은 command type을 사용할 때 payload 의미와 validation rule은 동일해야 한다.
- `WPF`가 offline mode에서 적재한 command는 `pending replay` 상태이며 서버 수락 전까지 authoritative state change가 아니다.
- `Web`는 offline business command를 지원하지 않는 것을 기본으로 둔다.
- `Edge`는 device event를 올릴 수 있지만 `approve-override`, `release-hold`, `post-production-actuals` 같은 business command를 직접 수행하지 않는다.

## 9. 최소 Happy Path 맵핑

1. `ingest-order-release`
2. `start-operation`
3. `record-material-scan`
4. `record-material-consumption`
5. `complete-operation`
6. `production-actuals-ready`
7. `post-production-actuals`

## 10. 최소 Exception Path 맵핑

### 10.1 Material mismatch

1. `record-material-scan`
2. `material-validation-failed`
3. optional `request-override`
4. `approve-override` or `reject-override`

### 10.2 Quality hold

1. `place-hold`
2. `hold-placed`
3. quality review on web
4. `release-hold`
5. `hold-released`

### 10.3 Inventory discrepancy

1. discrepancy detection
2. `line-side-discrepancy-detected`
3. `acknowledge-discrepancy`
4. `resolve-discrepancy`

## 11. 다음 단계

- 이 카탈로그를 기준으로 BFF API spec 초안 작성
- command별 payload schema와 validation rule 정의
- domain event별 subscriber와 retry rule 정의
- ERP/WMS/QMS integration contract를 파일럿 범위에 맞게 축약 명세화
