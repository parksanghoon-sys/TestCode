# MES WPF Station Client + Modbus/Edge ?몃? 援ы쁽 怨꾪쉷

## 1. 臾몄꽌 ??븷

??臾몄꽌????μ냼???꾩껜 MES 留덉뒪???뚮옖???꾨땲??

??臾몄꽌???꾨옒 ?곸쐞 臾몄꽌瑜?援ы쁽 愿?먯뿉??蹂댁“?섎뒗 ?섏쐞 怨꾪쉷?쒕떎.

- `docs/mes/architecture-blueprint.md`
- `docs/mes/implementation-roadmap.md`
- `docs/mes/client-channel-matrix.md`

利? ??臾몄꽌??`WPF station client`, `local device integration`, `Modbus/edge adapter`瑜??대뼸寃?遺숈씪吏 ?ㅻ챸?섎뒗 ?몃? 援ы쁽 怨꾪쉷?대떎.

??臾몄꽌媛 ?ㅼ떆 ?뺤쓽?섏? ?딅뒗 寃?

- MES ?꾩껜 ?쒖뒪??寃쎄퀎
- ERP/WMS/QMS/LIMS/SCADA/PLC ownership
- Release 1??canonical execution slice
- WPF/Web command ownership ?먯튃

## 2. ?꾩옱 ??μ냼????뺣젹 湲곗?

????μ냼???꾩옱 湲곗?? ?ㅼ쓬怨?媛숇떎.

- business command???좎씪??吏꾩엯?먯? `Experience API / BFF`??
- `Mes.Application`??execution workflow? idempotency, save boundary, completion progression???뚯쑀?쒕떎.
- `Mes.Infrastructure`??provider蹂?durable adapter? host seam???뚯쑀?쒕떎.
- ?꾩옱 backend runtime 湲곕낯媛믪? `Sqlite`?대ŉ, future relational provider??媛숈? host seam?쇰줈 援먯껜?쒕떎.
- `WPF`??operator-critical workflow??1湲?梨꾨꼸?댁?留?authoritative business state owner???꾨땲??
- `Site Edge` ?먮뒗 device bridge??protocol translation, buffering, handshake瑜??대떦?섏?留?business authority瑜?吏곸젒 ?뺤젙?섏? ?딅뒗??
- ?꾩옱 pilot profile??working assumptions??`docs/mes/pilot-profile-working-assumptions.md`??蹂꾨룄濡??뺣━?섏뼱 ?덈떎.

?곕씪??WPF/Modbus 援ы쁽? backend core瑜??고쉶?섎뒗 蹂꾨룄 command path瑜?留뚮뱾硫????쒕떎.

## 3. ??臾몄꽌??踰붿쐞

??臾몄꽌媛 ?ㅻ（??踰붿쐞:

- station login 諛?session binding UX
- station work queue? operator execution ?붾㈃
- scanner, printer, local device bridge ?곕룞
- pilot line???꾩슂??寃쎌슦??Modbus TCP ?곗꽑 ?듭떊 援ъ“
- local polling, command queue, read-back, reconnect ?뺤콉
- operator-facing alarm/status projection
- offline UX? provisional replay visibility
- simulator? device-oriented integration test ?꾨왂

??臾몄꽌媛 吏곸젒 ?ㅻ（吏 ?딅뒗 踰붿쐞:

- order release, genealogy, quality hold/release??canonical business rule ?뺤쓽
- DB schema??backend persistence provider 寃곗젙
- web portal??議고쉶/媛먯떆/?뱀씤 ?붾㈃ ?곸꽭 ?ㅺ퀎
- full enterprise integration architecture

## 4. ?듭떖 ?ㅺ퀎 ?먯튃

1. WPF???붾㈃ ?곹깭? local device UX留?梨낆엫吏꾨떎.
2. business command????긽 `WPF -> BFF -> Mes.Application` 寃쎈줈濡쒕쭔 ?ㅼ뼱媛꾨떎.
3. ViewModel?먯꽌 DB, SQLite, PostgreSQL, Modbus register 二쇱냼瑜?吏곸젒 ?ㅻ（吏 ?딅뒗??
4. device read path? business command path??遺꾨━?쒕떎.
5. tag definition, endian mode, scaling, polling group? 肄붾뱶???섎뱶肄붾뵫?섏? ?딄퀬 ?몃??뷀븳??
6. scanner, printer, local bridge??WPF媛 吏곸젒 ?ㅻ０ ???덉?留? 洹?寃곌낵媛 business truth媛 ?섎젮硫??쒕쾭 command accept媛 ?꾩슂?섎떎.
7. manual action, command failure, reconnect, read-back mismatch??紐⑤몢 operator-visible ?곹깭??audit ??곸쑝濡??④꺼???쒕떎.
8. offline 以??앹꽦???ㅽ뻾 寃곌낵??authoritative completion???꾨땲??`pending replay` ?곹깭濡?痍④툒?쒕떎.
9. `hold release`, `override approval`, `ERP posting`, `master change`??local authority濡?泥섎━?섏? ?딅뒗??

## 5. 梨낆엫 寃쎄퀎

| ?곸뿭 | 梨낆엫 | 吏곸젒 媛吏吏 ?딅뒗 梨낆엫 |
|---|---|---|
| WPF Station Client | ?붾㈃, local session, scanner/printer UX, provisional offline queue ?쒖떆, BFF ?몄텧 | canonical business rule, DB persistence, direct PLC business authority |
| Experience API / BFF | task-oriented command/query contract, transport error normalization, thin host composition | UI state, protocol parsing, equipment polling |
| MES Core (`Mes.Application` + `Mes.Domain`) | execution truth, hold/release, completion progression, idempotency, save contract | Modbus frame 泥섎━, printer/scanner driver |
| Site Edge / Modbus Adapter | protocol translation, polling, write queue, reconnect, read-back, device snapshot normalization | ?앹궛?ㅻ뜑 ?꾨즺 ?뺤젙, quality authority, override approval |
| PLC/SCADA/HMI | deterministic machine control, machine-native status and alarm signal | MES business workflow and audit truth |

## 6. 沅뚯옣 ?고????먮쫫

### 6.1 Operator execution command path

1. operator媛 WPF ?붾㈃?먯꽌 `start-operation`, `record-material-consumption`, `complete-operation` 媛숈? action???섑뻾?쒕떎.
2. WPF??task-oriented request瑜?`Experience API / BFF`濡??꾩넚?쒕떎.
3. `Mes.Application`??validation, replay check, hold gate, completion progression, save contract瑜??곸슜?쒕떎.
4. backend媛 accepted result瑜???ν븯怨?notification ?먮뒗 query-refresh 湲곗? ?곹깭瑜?留뚮뱺??
5. WPF??authoritative result瑜?諛섏쁺?쒕떎.

??寃쎈줈?먯꽌 WPF??吏곸젒 DB??PLC瑜?business command owner濡??ъ슜?섏? ?딅뒗??

### 6.2 Device telemetry path

1. Modbus/edge adapter媛 polling group ?⑥쐞濡??ㅻ퉬 媛믪쓣 ?쎈뒗??
2. raw register 媛믪? parser媛 endian, scale, type 洹쒖튃?쇰줈 ?낅Т媛믪쑝濡?蹂?섑븳??
3. normalized equipment snapshot ?먮뒗 device event瑜?留뚮뱺??
4. ?꾩슂??寃쎌슦 edge媛 MES???꾨떖?섍퀬, MES/BFF媛 operator-facing state濡??ш뎄?깊븳??
5. WPF??吏곸젒 register 二쇱냼媛 ?꾨땲??normalized ?곹깭瑜??ъ슜?쒕떎.

### 6.3 Device command path

?λ퉬 ?쒖뼱??handshake媛 ?꾩슂??寃쎌슦?먮룄 business authority??遺꾨━?쒕떎.

1. WPF媛 command瑜?BFF???붿껌?쒕떎.
2. MES媛 沅뚰븳, ?곹깭, safety precondition??寃利앺븳??
3. ?덉슜??寃쎌슦?먮쭔 edge/device queue濡?command瑜??꾨떖?쒕떎.
4. edge媛 ?쒖감 ?ㅽ뻾?섍퀬 read-back?쇰줈 寃곌낵瑜??뺤씤?쒕떎.
5. 寃곌낵??audit/event濡??④퀬 WPF??諛섑솚?쒕떎.

利? `UI -> PLC direct control`???꾨땲??`UI -> MES authorize -> edge execute -> read-back -> MES record`媛 湲곕낯?대떎.

## 7. WPF Station Client ?ㅺ퀎 洹쒖튃

### 7.1 WPF媛 諛붾줈 媛?몄빞 ??湲곕뒫

- station login / session binding
- ?꾩옱 station work queue ?쒖떆
- ?묒뾽 ?쒖옉, ?쇱떆?뺤?, ?ш컻, ?꾨즺
- ?먯옱 lot/serial scan UX
- label print / reprint
- hold placement ?붿껌
- offline queue ?곹깭? replay 異⑸룎 ?쒖떆
- ?λ퉬 ?곌껐/bridge ?곹깭 媛?쒗솕

### 7.2 WPF???먯? 留먯븘????湲곕뒫

- canonical hold release ?뱀씤
- override approval
- master data 蹂寃?authority
- ERP posting 寃곗젙
- backend persistence provider 遺꾧린

### 7.3 ViewModel 洹쒖튃

- ViewModel? BFF client abstraction留??몄텧?쒕떎.
- register address, SQL query, provider name, filesystem path瑜??뚯? ?딅뒗??
- UI thread? polling ?먮뒗 device callback thread瑜?遺꾨━?쒕떎.
- provisional ?곹깭? authoritative ?곹깭瑜??붾㈃?먯꽌 援щ텇?쒕떎.

## 8. Modbus/Edge ?ㅺ퀎 洹쒖튃

### 8.1 Modbus 梨꾪깮 ???꾩젣

Modbus engine??generic?섍쾶 癒쇱? ?ш쾶 留뚮뱶??寃껊낫?? pilot line?먯꽌 ?ㅼ젣 ?꾩슂??protocol怨?tag set??癒쇱? ?뺤씤?섎뒗 寃껋씠 ?곗꽑?대떎.

?곸뼱???꾨옒媛 ?뺤씤???ㅼ뿉 implementation???볧엺??

- pilot line???ㅼ젣濡?Modbus TCP ?먮뒗 RTU瑜??곕뒗吏
- ?대뼡 ?ㅻ퉬媛 station workflow??吏곸젒 ?곌껐?섎뒗吏
- ?쎄린 ?꾩슜?몄?, write/ack媛 ?꾩슂?쒖?
- ?대뼡 ?쒓렇媛 operator workflow? ?ㅼ젣濡??곌껐?섎뒗吏

### 8.2 ?듭떊 怨꾩링 ?먯튃

- `read polling`怨?`write command queue`瑜?遺꾨━?쒕떎.
- ?λ퉬蹂?reconnect ?뺤콉? adapter???붾떎.
- ?숈씪 ?ㅻ퉬?????write???λ퉬 ?⑥쐞 ?쒖감 ?ㅽ뻾??湲곕낯?쇰줈 ?쒕떎.
- write ?댄썑?먮뒗 read-back ?먮뒗 ack verification??湲곕낯?쇰줈 ?쒕떎.
- timeout, retry, reconnect??operator-visible ?곹깭? log瑜??④릿??

### 8.3 ?쒓렇 ?뺤쓽 ?먯튃

tag mapping? 肄붾뱶??諛뺤? ?딄퀬 ?ㅼ젙 ?먮뒗 DB?먯꽌 怨듦툒?쒕떎.

沅뚯옣 ?꾨뱶:

- `EquipmentCode`
- `TagName`
- `AddressType`
- `Address`
- `DataType`
- `WordLength`
- `EndianMode`
- `Scale`
- `Offset`
- `PollingGroup`
- `PollingIntervalMs`
- `Writable`
- `ReadBackRequired`
- `Description`

?덉떆:

```json
{
  "equipmentCode": "EQ-01",
  "tagName": "RunStatus",
  "addressType": "HoldingRegister",
  "address": 40010,
  "dataType": "UInt16",
  "wordLength": 1,
  "endianMode": "BigEndian",
  "scale": 1.0,
  "offset": 0,
  "pollingGroup": "StatusFast",
  "pollingIntervalMs": 500,
  "writable": false,
  "readBackRequired": false,
  "description": "?ㅻ퉬 ?댁쟾 ?곹깭"
}
```

### 8.4 ?뚮엺 泥섎━ ?먯튃

- raw bit ?먮뒗 vendor code??edge/adapter?먯꽌 normalized alarm input?쇰줈 蹂?섑븳??
- operator-facing alarm acknowledgement媛 business audit瑜??④꺼???섎㈃ BFF command瑜??듯빐 泥섎━?쒕떎.
- alarm flood, reconnect storm, stale snapshot? ?쒕??덉씠???뚯뒪?몃줈 癒쇱? 寃利앺븳??

## 9. ?붾㈃ ?곗꽑?쒖쐞

?꾩옱 梨꾨꼸 ?뺤콉怨?留욌뒗 WPF ?곗꽑 ?붾㈃? ?꾨옒 ?쒖꽌媛 ?곸젅?섎떎.

1. station login / session binding
2. current station work queue
3. start / pause / resume / complete
4. material scan and consumption
5. label print / reprint
6. hold placement
7. offline queue / reconnect / bridge status

web-first濡??④꺼???붾㈃:

- dispatch board
- hold release
- override approval
- genealogy search
- quality history review
- discrepancy dashboard
- admin/master data review

## 10. 沅뚯옣 援ы쁽 ?④퀎

### Phase A. Station shell alignment

紐⑺몴:

- ?꾩옱 BFF contract瑜??ъ슜?섎뒗 理쒖냼 WPF shell ?뺤쓽

?댁빞 ????

- station session model ?뺤쓽
- WPF shell, navigation, status bar 援ъ“ ?뺤쓽
- BFF client abstraction ?뺤쓽
- work queue, command result, problem-details ?쒖떆 洹쒖튃 ?뺤쓽

?꾨즺 湲곗?:

- WPF媛 backend provider 醫낅쪟瑜?紐곕씪???꾩옱 queue? command 寃곌낵瑜??쒖떆?????덈떎.

### Phase B. Operator execution station flow

紐⑺몴:

- ?꾩옱 援ы쁽??operator-execution slice瑜?WPF station flow濡??곌껐

?댁빞 ????

- start / pause / resume / complete ?붾㈃ ?먮쫫 ?뺤쓽
- material scan UX ?뺤쓽
- offline provisional ?쒖떆 洹쒖튃 ?뺤쓽
- `400/404/409/422/500` problem-details瑜??붾㈃ 硫붿떆吏 ?뺤콉?쇰줈 ?뺣━

?꾨즺 湲곗?:

- operator媛 ?꾩옱 slice 踰붿쐞??紐낅졊??WPF?먯꽌 ?섑뻾?????덈떎.

### Phase C. Peripheral integration

紐⑺몴:

- scanner, printer, local bridge瑜?station UX? ?덉쟾?섍쾶 ?곌껐

?댁빞 ????

- scanner input abstraction
- printer service abstraction
- local bridge contract ?뺤쓽
- ?ㅽ뙣 ???ъ떆?꾩? operator feedback ?뺤콉 ?뺣━

?꾨즺 湲곗?:

- scanner/printer ?섏〈 ?낅Т媛 ViewModel?먯꽌 driver ?몃??ы빆 ?놁씠 ?숈옉?쒕떎.

### Phase D. Modbus/edge integration

紐⑺몴:

- pilot line?먯꽌 ?뺣쭚 ?꾩슂???ㅻ퉬留?理쒖냼 Modbus/edge path濡??곌껐

?댁빞 ????

- pilot equipment inventory 湲곕컲 tag shortlist ?뺤젙
- polling scheduler
- device command queue
- value parser
- reconnect / read-back policy
- simulator 湲곕컲 verification

?꾨즺 湲곗?:

- pilot line??理쒖냼 ?λ퉬 ?곹깭? ?꾩슂??handshake媛 ?덉젙?곸쑝濡??ы쁽?쒕떎.

二쇱쓽:

- ???④퀎??pilot line怨?protocol???뺤젙?섍린 ?꾩뿉???쇰컲??援ы쁽?쇰줈 ?볧엳吏 ?딅뒗??

### Phase E. Hardening and operations

紐⑺몴:

- ?꾩옣 ?뚯씪???ъ엯 ??station/edge ?덉젙??寃利?
?댁빞 ????

- offline replay 異⑸룎 ?쒕굹由ъ삤
- reconnect 諛?stale-state ?쒕굹由ъ삤
- alarm flood
- ?μ떆媛?polling
- operator action audit ?뺤씤

?꾨즺 湲곗?:

- station UI媛 硫덉텛吏 ?딄퀬, reconnect? read-back ?ㅽ뙣媛 媛?쒗솕?쒕떎.

## 11. ?뚯뒪???꾨왂

### 11.1 ?⑥쐞 ?뚯뒪??
- ViewModel state transition
- problem-details to UI message mapping
- tag parser
- endian/scaling conversion
- read-back ?먯젙
- offline queue ?곹깭 ?꾩씠

### 11.2 ?듯빀 ?뚯뒪??
- WPF client abstraction怨?BFF contract ?곕룞
- command -> problem-details -> UI feedback ?먮쫫
- scanner/printer abstraction ?곕룞
- Modbus simulator 湲곕컲 polling/read-back
- edge reconnect? queue flush

### 11.3 ?댁쁺 ?쒕??덉씠??
- station network loss
- duplicate submit
- polling overload
- read-back mismatch
- alarm burst
- reconnect after stale local cache

## 12. 二쇱슂 ?꾪뿕怨????
### 12.1 WPF媛 backend authority瑜?移⑤쾾?섎뒗 ?꾪뿕

???

- command????긽 BFF瑜??듯븳??
- local queue??provisional state留?媛吏꾨떎.

### 12.2 UI freeze

???

- UI thread? device thread 遺꾨━
- async flow? bounded queue ?ъ슜
- Dispatcher 寃쎄퀎 理쒖냼??
### 12.3 tag hardcoding

???

- tag definition ?몃???- ?λ퉬蹂?parser ?뚯뒪??
### 12.4 pilot line 誘명솗???곹깭?먯꽌 generic Modbus 援ы쁽??怨쇰룄?섍쾶 ?볧엳???꾪뿕

???

- protocol, equipment, tag shortlist媛 ?뺤젙?섍린 ?꾩뿉??generic framework瑜?怨쇰? ?ㅺ퀎?섏? ?딅뒗??

### 12.5 WPF? Web semantics drift

???

- 媛숈? ?낅Т??媛숈? BFF command? 媛숈? audit semantics瑜??ъ슜?쒕떎.
- 梨꾨꼸蹂?李⑥씠???붾㈃ ?쒗쁽怨?local UX?먮쭔 ?붾떎.

## 13. ?꾩옱 湲곗???異붿쿇 ?ㅼ쓬 ?④퀎

??臾몄꽌瑜?湲곗??쇰줈 諛붾줈 援ы쁽???볧엳湲??꾩뿉 癒쇱? ?뺤씤?댁빞 ?섎뒗 寃껋? ?꾨옒??

1. pilot line???앹궛 諛⑹떇??discrete, batch, hybrid 以?臾댁뾿?몄?
2. genealogy depth媛 serial, lot, hybrid 以??대뵒源뚯? ?꾩슂?쒖?
3. pilot station???ㅼ젣濡?Modbus/PLC handshake媛 ?꾩슂?쒖?, ?꾨땲硫?scanner/printer 以묒떖?몄?
4. ?꾩슂?섎떎硫??대뼡 ?ㅻ퉬, ?대뼡 protocol, ?대뼡 理쒖냼 tag set??operator workflow? ?곌껐?섎뒗吏

????媛吏媛 ?좉꺼??WPF/Modbus 援ы쁽???덉쟾?섍쾶 ?몃텇?뷀븷 ???덈떎.

## 14. 愿??臾몄꽌

- ?곸쐞 ?꾪궎?띿쿂: `docs/mes/architecture-blueprint.md`
- pilot profile assumptions: `docs/mes/pilot-profile-working-assumptions.md`
- ?곸쐞 濡쒕뱶留? `docs/mes/implementation-roadmap.md`
- 梨꾨꼸 ?뺤콉: `docs/mes/client-channel-matrix.md`
- command/event 湲곗??? `docs/mes/command-event-catalog.md`
- ?꾩옱 execution slice ?ㅺ퀎: `docs/mes/pilot-slice-01-application-design.md`

