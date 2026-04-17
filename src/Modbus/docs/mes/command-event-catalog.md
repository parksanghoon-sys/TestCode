# MES Command and Event Catalog

## 1. 紐⑹쟻

??臾몄꽌???뚯씪??Release 1 湲곗??쇰줈 `BFF -> MES Core` command contract? 二쇱슂 domain or integration event瑜??뺣━?쒕떎.

??臾몄꽌??紐⑹쟻? ?ㅼ쓬怨?媛숇떎.

- WPF? Web??媛숈? ?낅Т 紐낅졊 semantics瑜??ъ슜?섎룄濡?湲곗??좎쓣 怨좎젙?쒕떎.
- command? event瑜?遺꾨━??idempotency? audit 湲곗???紐낇솗???쒕떎.
- BFF, MES core, Edge, ERP/WMS/QMS 媛??곹샇?묒슜??媛숈? ?몄뼱濡??뺣━?쒕떎.
- ?꾩옱 `Mes.Domain` 肄붾뱶???대? 援ы쁽???대깽?몄?, ?꾩쭅 application or integration layer???④꺼???뚰겕?뚮줈???대깽?몃? 援щ텇?쒕떎.

## 2. 踰붿쐞? 湲곕낯 ?먯튃

- 踰붿쐞??Release 1 pilot workflow 以묒떖?대떎.
- 紐⑤뱺 business command??`Experience API / BFF`瑜??듯빐 吏꾩엯?쒕떎.
- 紐⑤뱺 command??`command_id`, `command_type`, `actor_id`, `channel`, `idempotency_key`, `correlation_id`瑜?媛吏꾨떎.
- 紐⑤뱺 server-accepted command??理쒖냼 1媛??댁긽??authoritative state change ?먮뒗 workflow output???④릿??
- equipment or edge?먯꽌 ?ㅼ뼱?ㅻ뒗 ?낅젰? raw device event?????덉?留?authoritative state change??MES core 寃利??댄썑?먮쭔 ?뺤젙?쒕떎.

## 3. Command Envelope

| Field | ?ㅻ챸 |
|---|---|
| `command_id` | ?대씪?댁뼵?멸? ?앹꽦?섍굅???쒕쾭媛 ?섎씫 ??遺?ы븯??怨좎쑀 紐낅졊 ?앸퀎??|
| `command_type` | ?? `start-operation`, `record-material-consumption` |
| `actor_id` | ?ъ슜???먮뒗 system actor ?앸퀎??|
| `channel` | `wpf`, `web`, `integration`, `edge` 以??섎굹 |
| `station_id` | ?꾩옣 station context媛 ?꾩슂??寃쎌슦 ?ы븿 |
| `correlation_id` | ?섎굹???묒뾽 ?먮쫫??臾띕뒗 ?곴?愿怨?ID |
| `idempotency_key` | 以묐났 ?쒖텧 諛⑹?瑜??꾪븳 ??|
| `revision_refs` | item, routing, BOM, spec revision 李몄“ |
| `client_timestamp` | ?대씪?댁뼵??諛쒖깮 ?쒓컖 |
| `server_received_at` | ?쒕쾭 ?섏떊 ?쒓컖 |

## 4. Business Command Catalog

| Command | Primary initiator | Preferred channel | Offline | Preconditions | Success events | External sync |
|---|---|---|---|---|---|---|
| `start-operation` | Operator | WPF-first | Conditional | station bound, order or operation ready, revision valid | `operation-started` | optional APS feedback later |
| `pause-operation` | Operator | WPF-first | Conditional | operation running | `operation-paused` | none |
| `resume-operation` | Operator | WPF-first | Conditional | operation paused | `operation-resumed` | none |
| `complete-operation` | Operator or equipment-assisted station | WPF-first | Conditional | operation running, required scans or results satisfied | `operation-completed`, `production-actuals-ready` | ERP posting |
| `record-material-scan` | Operator | WPF-first | Conditional | station active, material identifier present | `material-scanned`, `material-validation-passed` or `material-validation-failed` | none |
| `record-material-consumption` | Operator | WPF-first | Conditional | material validated, qty rules satisfied | `material-consumption-recorded`, `genealogy-link-created` | WMS-lite sync |
| `record-material-return` | Operator | WPF-first | Conditional | issued material exists | `material-return-recorded` | WMS-lite sync |
| `record-scrap` | Operator or line leader | WPF-first | Conditional | active WIP exists, reason required | `scrap-recorded`, `production-actuals-ready` | ERP posting |
| `place-hold` | Operator, quality | WPF-first | Yes | WIP, operation, or quality record exists, hold reason required | `hold-placed` | QMS trigger optional |
| `record-quality-result` | Quality inspector or station quality step | Shared | Conditional | quality record exists, inspection is pending or in progress, decision note present | `quality-result-recorded` | optional QMS notification |
| `release-hold` | Quality, supervisor | Web-first | No | hold active, approval satisfied | `hold-released` | QMS notification optional |
| `request-override` | Operator or line leader | WPF-first | Conditional | override reason required | `override-requested` | none |
| `approve-override` | Supervisor or quality | Web-first | No | pending override exists, approval authority valid | `override-approved` | optional audit export |
| `reject-override` | Supervisor or quality | Web-first | No | pending override exists | `override-rejected` | none |
| `acknowledge-discrepancy` | Warehouse, supervisor | Web-first | No | discrepancy exists | `line-side-discrepancy-acknowledged` | WMS-lite reconciliation |
| `resolve-discrepancy` | Warehouse, production support | Web-first | No | discrepancy investigated | `line-side-discrepancy-resolved` | WMS-lite reconciliation |

## 5. System and Integration Commands

| Command | Initiator | Owner | Notes |
|---|---|---|---|
| `ingest-order-release` | ERP integration | MES core | ?앹궛?ㅻ뜑 release ?섏떊 |
| `sync-master-data-revision` | Integration API | MES core | item/BOM/routing/spec revision ?숆린??|
| `ingest-equipment-completion` | Edge | MES core | raw device event瑜?validated completion candidate濡??낅젰 |
| `post-production-actuals` | MES core or system | Integration API | good, scrap, consumption, completion ?뚯떊 |
| `sync-line-side-material-movement` | MES core or system | Integration API | Release 1 WMS-lite event or batch sync |

## 6. Domain Event Catalog

| Event | Triggered by | Meaning | Primary consumers |
|---|---|---|---|
| `order-released-ingested` | `ingest-order-release` | ERP release order媛 MES?먯꽌 executable state濡??섎씫??| orchestration, dispatch |
| `operation-started` | `start-operation` | ?묒뾽 ?쒖옉??authoritative?섍쾶 湲곕줉??| dispatch, audit, reporting |
| `operation-paused` | `pause-operation` | ?묒뾽 ?쇱떆?뺤?媛 authoritative?섍쾶 湲곕줉??| dispatch, audit |
| `operation-resumed` | `resume-operation` | ?쇱떆?뺤????묒뾽???ㅼ떆 吏꾪뻾??| dispatch, audit |
| `operation-completed` | `complete-operation` | ?묒뾽 ?꾨즺媛 authoritative?섍쾶 ?뺤젙??| ERP sync, reporting, next step |
| `material-scanned` | `record-material-scan` | ?먯옱 ?앸퀎?먭? station?먯꽌 罹≪쿂??| validation, audit |
| `material-validation-passed` | `record-material-scan` | ?ㅼ틪 ?먯옱媛 ?꾩옱 workflow validation???듦낵??| station UX, consumption flow |
| `material-validation-failed` | `record-material-scan` | ?ㅼ틪 ?먯옱媛 ?꾩옱 workflow validation???듦낵?섏? 紐삵븿 | station UX, exception monitoring |
| `material-consumption-recorded` | `record-material-consumption` | ?먯옱 ?뚮え媛 authoritative?섍쾶 湲곕줉??| genealogy, ERP/WMS sync |
| `material-return-recorded` | `record-material-return` | ?먯옱 諛섎궔??authoritative?섍쾶 湲곕줉??| WMS-lite sync |
| `genealogy-link-created` | `record-material-consumption` | parent-child genealogy link媛 ?앹꽦??| genealogy search, audit |
| `scrap-recorded` | `record-scrap` | scrap??authoritative?섍쾶 湲곕줉??| ERP sync, reporting |
| `hold-placed` | `place-hold` | WIP, operation, or quality gate媛 hold ?곹깭濡??꾪솚??| execution gate, quality, monitoring |
| `hold-released` | `release-hold` | hold媛 release?섏뼱 ?ㅼ쓬 吏꾪뻾 媛???곹깭媛 ??| execution gate, quality |
| `quality-result-recorded` | `record-quality-result` | inspection decision??authoritative?섍쾶 湲곕줉??| quality workflow, audit, hold decision |
| `override-requested` | `request-override` | ?덉쇅 ?뱀씤 ?붿껌???앹꽦??| supervisor web workflow |
| `override-approved` | `approve-override` | ?덉쇅 ?뱀씤???뺤젙??| execution flow, audit |
| `override-rejected` | `reject-override` | ?덉쇅 ?뱀씤 ?붿껌??諛섎젮??| execution flow, audit |
| `line-side-discrepancy-detected` | reconciliation or validation logic | line-side inventory discrepancy媛 媛먯???| warehouse, supervisor |
| `line-side-discrepancy-acknowledged` | `acknowledge-discrepancy` | discrepancy ??묒씠 ?쒖옉??| warehouse workflow |
| `line-side-discrepancy-resolved` | `resolve-discrepancy` | discrepancy媛 醫낅즺??| warehouse, audit |
| `production-actuals-ready` | completion, scrap, or consumption consolidation | ?몃? posting 媛?ν븳 actuals ?명듃媛 以鍮꾨맖 | Integration API |

### 6.1 Current code alignment notes

- The current `Mes.Domain` seed directly models these domain events: `order-released-ingested`, `operation-started`, `operation-paused`, `operation-resumed`, `operation-completed`, `scrap-recorded`, `material-consumption-recorded`, `material-return-recorded`, `genealogy-link-created`, `hold-placed`, `hold-released`, `quality-result-recorded`, `override-requested`, `override-approved`, and `override-rejected`.
- `material-scanned`, `material-validation-passed`, `material-validation-failed`, `production-actuals-ready`, and discrepancy events remain application or integration workflow outputs for the first pilot slice. They are intentionally not `Mes.Domain` aggregate events yet.
- The canonical command name for material usage is `record-material-consumption`; shorthand such as `record-consumption` should not appear in machine-facing specs.
- The canonical executable state names for the current code seed are `ProductionOrderStatus.InProgress`, `ProductionOrderStatus.PartiallyCompleted`, `OperationExecutionStatus.Paused`, `OperationExecutionStatus.Done`, `WipUnitStatus.InProcess`, `QualityRecordStatus.InInspection`, and `OverrideRequestStatus.Requested`, `Approved`, `Rejected`.

## 7. Integration Event Catalog

| Event | Direction | Meaning |
|---|---|---|
| `production-actuals-posted-to-erp` | MES -> ERP | good, scrap, completion, consumption??ERP??諛섏쁺??|
| `production-actuals-post-failed` | MES -> ERP result | ERP posting ?ㅽ뙣 ?먮뒗 ?ъ떆???꾩슂 |
| `material-movement-sent-to-wms-lite` | MES -> WMS | line-side issue, return, discrepancy 愿??理쒖냼 ?뺤궛 ?대깽???꾩넚 |
| `material-movement-sync-failed` | MES -> WMS result | WMS-lite ?뺤궛 ?ㅽ뙣 ?먮뒗 蹂대쪟 |
| `quality-notification-sent` | MES -> QMS/LIMS | hold, NCR, lab gate 愿???뚮┝ ?꾨떖 |

## 8. Channel-Specific Rules

- `WPF`? `Web`媛 媛숈? command type???ъ슜????payload ?섎?? validation rule? ?숈씪?댁빞 ?쒕떎.
- `WPF`媛 offline mode?먯꽌 ?곸옱??command??`pending replay` ?곹깭?대ŉ ?쒕쾭 ?섎씫 ?꾧퉴吏 authoritative state change媛 ?꾨땲??
- `Web`? offline business command瑜?吏?먰븯吏 ?딅뒗 寃껋쓣 湲곕낯?쇰줈 ?붾떎.
- `Edge`??device event瑜??щ┫ ???덉?留?`approve-override`, `release-hold`, `post-production-actuals` 媛숈? business command瑜?吏곸젒 ?섑뻾?섏? ?딅뒗??

## 9. 理쒖냼 Happy Path 留듯븨

1. `ingest-order-release`
2. `start-operation`
3. `record-material-scan`
4. `record-material-consumption`
5. `record-quality-result`
6. `complete-operation`
7. `production-actuals-ready`
8. `post-production-actuals`

## 10. 理쒖냼 Exception Path 留듯븨

### 10.1 Material mismatch

1. `record-material-scan`
2. `material-validation-failed`
3. optional `request-override`
4. `approve-override` or `reject-override`

### 10.2 Quality hold

1. `place-hold`
2. `hold-placed`
3. quality review on web
4. optional `record-quality-result`
5. `release-hold`
6. `hold-released`

### 10.3 Inventory discrepancy

1. discrepancy detection
2. `line-side-discrepancy-detected`
3. `acknowledge-discrepancy`
4. `resolve-discrepancy`

## 11. ?ㅼ쓬 ?④퀎

- ??移댄깉濡쒓렇瑜?湲곗??쇰줈 BFF payload spec??concrete application contract濡??대┛??
- command蹂?validation rule怨?idempotency receipt ???諛⑹떇??援ъ껜?뷀븳??
- domain event? application workflow event??subscriber, retry, projection 梨낆엫???섎늿??
- ERP/WMS/QMS integration contract瑜??뚯씪??踰붿쐞??留욊쾶 ??援ъ껜?뷀븳??

