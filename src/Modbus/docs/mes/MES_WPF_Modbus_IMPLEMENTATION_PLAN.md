# MES WPF Station Client + Modbus/Edge 구현 계획

## 1. 문서 역할

이 문서는 저장소 전체 MES 마스터 계획이 아니다.

이 문서는 아래 상위 설계 문서를 구현 관점에서 보조하는 하위 계획이다.

- `docs/mes/architecture-blueprint.md`
- `docs/mes/implementation-roadmap.md`
- `docs/mes/client-channel-matrix.md`

즉, 이 문서는 `WPF station client`, `local device integration`, `Modbus/edge adapter`를 어떤 순서와 원칙으로 붙일지 설명하는 실행 계획이다.

이 문서가 다시 정의하지 않는 것:

- MES 전체 시스템 경계
- ERP, WMS, QMS, LIMS, SCADA, PLC ownership
- Release 1 canonical execution slice
- WPF와 Web의 business command ownership 원칙

## 2. 현재 저장소와 정렬 기준

현재 저장소의 기준선은 아래와 같다.

- 모든 business command는 `Experience API / BFF`를 통해 들어간다.
- `Mes.Application`은 execution workflow, idempotency, save boundary, completion progression을 소유한다.
- `Mes.Infrastructure`는 provider별 durable adapter와 host seam을 소유하며, 현재 기본 runtime은 `Sqlite`다.
- `Mes.Client.Wpf`는 station binding, queue 조회, `start-operation`, `record-material-scan`, `record-material-consumption`, `complete-operation`까지 이미 shared BFF seam 위에서 동작한다.
- `example/Mes.MockStation.Example`은 실장비 없이 현재 station flow를 검증하는 example-layer mock path다.
- `WPF`는 operator-critical workflow의 1차 채널이지만 authoritative business state owner는 아니다.
- `Site Edge`는 protocol translation, buffering, handshake를 담당하지만 business authority를 직접 결정하지 않는다.
- 현재 pilot profile의 working assumptions는 `docs/mes/pilot-profile-working-assumptions.md`에 정리되어 있다.

따라서 WPF와 Modbus 구현은 backend core를 우회하는 별도 command path를 만들면 안 된다.

## 3. 이 문서의 범위

이 문서가 다루는 범위:

- station login과 session binding UX
- station work queue와 operator execution 화면
- scanner, printer, local device bridge 연동
- 파일럿 라인에서 필요할 경우의 Modbus TCP 중심 통신 구조
- local polling, command queue, read-back, reconnect 정책
- operator-facing alarm과 status projection
- offline UX와 pending replay visibility
- mock, simulator, device-oriented integration test 전략

이 문서가 직접 다루지 않는 범위:

- order release, genealogy, quality hold/release의 canonical business rule 정의
- DB schema 또는 backend persistence provider 결정
- Web portal의 조회, 감사, 승인 화면 상세 설계
- enterprise integration 전체 구조 재설계

## 4. 전달 설계 원칙

1. WPF는 화면 상태와 local device UX만 책임진다.
2. business command는 항상 `WPF -> BFF -> Mes.Application` 경로로만 들어간다.
3. ViewModel에서 DB, SQLite, PostgreSQL, Modbus register 주소를 직접 다루지 않는다.
4. device read path와 business command path는 분리한다.
5. tag definition, endian mode, scaling, polling group은 코드에 하드코딩하지 않고 외부 정의로 둔다.
6. scanner, printer, local bridge는 WPF가 직접 붙일 수 있지만, business truth는 서버 command accept 이후에만 성립한다.
7. manual action, command failure, reconnect, read-back mismatch는 모두 operator-visible 상태와 audit 대상으로 남겨야 한다.
8. offline 중 생성된 실행 결과는 authoritative completion이 아니라 `pending replay` 상태로 본다.
9. `hold release`, `override approval`, `ERP posting`, `master change`는 local authority로 처리하지 않는다.

## 5. 책임 경계

| 영역 | 책임 | 직접 가지지 않는 책임 |
|---|---|---|
| WPF Station Client | 화면, local session, scanner/printer UX, provisional offline queue 표시, BFF 호출 | canonical business rule, DB persistence, direct PLC business authority |
| Experience API / BFF | task-oriented command/query contract, transport error normalization, thin host composition | UI state, protocol parsing, equipment polling |
| MES Core (`Mes.Application` + `Mes.Domain`) | execution truth, hold/release, completion progression, idempotency, save contract | Modbus frame 처리, printer/scanner driver |
| Site Edge / Modbus Adapter | protocol translation, polling, write queue, reconnect, read-back, device snapshot normalization | 생산오더 완료 결정, quality authority, override approval |
| PLC/SCADA/HMI | deterministic machine control, machine-native status and alarm signal | MES business workflow and audit truth |

## 6. 권장 흐름

### 6.1 Operator execution command path

1. operator가 WPF 화면에서 `start-operation`, `record-material-consumption`, `complete-operation` 같은 action을 수행한다.
2. WPF는 task-oriented request를 `Experience API / BFF`로 전송한다.
3. `Mes.Application`은 validation, replay check, hold gate, completion progression, save contract를 적용한다.
4. backend는 accepted result를 저장하고 notification 또는 query-refresh 기준 상태를 만든다.
5. WPF는 authoritative result를 반영한다.

이 경로에서 WPF는 직접 DB나 PLC를 business command owner로 사용하지 않는다.

### 6.2 Device telemetry path

1. Modbus 또는 edge adapter가 polling group 단위로 설비 값을 읽는다.
2. raw register 값은 parser가 endian, scale, type 규칙으로 업무 값으로 변환한다.
3. normalized equipment snapshot 또는 device event를 만든다.
4. 필요하면 edge가 MES로 전달하고, MES 또는 BFF가 operator-facing state로 재구성한다.
5. WPF는 직접 register 주소가 아니라 normalized 상태를 소비한다.

### 6.3 Device command path

설비 제어 또는 handshake가 필요한 경우에도 business authority는 분리한다.

1. WPF가 command를 BFF로 요청한다.
2. MES가 권한, 상태, safety precondition을 검증한다.
3. 허용된 경우에만 edge 또는 device queue로 command를 전달한다.
4. edge가 실제 실행하고 read-back으로 결과를 확인한다.
5. 결과는 audit 또는 event로 남고 WPF에 반영된다.

즉 `UI -> PLC direct control`이 아니라 `UI -> MES authorize -> edge execute -> read-back -> MES record`가 기본이다.

## 7. WPF Station Client 설계 규칙

### 7.1 WPF가 가져야 할 기능

- station login과 session binding
- 현재 station work queue 표시
- `start-operation`, `record-material-consumption`, `complete-operation`
- `record-material-scan`과 authoritative validation feedback 반영
- label print와 reprint
- hold placement 또는 quality feedback 표시
- offline queue 상태와 replay 충돌 표시
- 설비 연결과 bridge 상태 가시화

### 7.2 WPF에서 맡지 않아야 할 기능

- canonical hold release 승인
- override approval
- master data 변경 authority
- ERP posting 결정
- backend persistence provider 분기

### 7.3 ViewModel 규칙

- ViewModel은 BFF client abstraction만 호출한다.
- register address, SQL query, provider name, filesystem path를 알지 않는다.
- UI thread와 polling 또는 device callback thread를 분리한다.
- provisional 상태와 authoritative 상태를 화면에서 구분한다.
- command context는 공유 factory를 통해 만들고, 화면별 ad hoc metadata 생성을 피한다.

## 8. Modbus/Edge 설계 규칙

### 8.1 Modbus 채택 전 전제

Modbus engine을 generic하게 먼저 크게 만드는 것보다, 파일럿 라인에서 실제 필요한 protocol과 tag set을 먼저 확인하는 것이 우선이다.

아래 질문이 확인된 뒤에 implementation을 넓힌다.

- 파일럿 라인이 실제로 Modbus TCP 또는 RTU를 쓰는지
- 어떤 설비가 station workflow와 직접 연결되는지
- 읽기 전용인지, write와 ack가 필요한지
- 어떤 태그가 operator workflow에 실제로 연결되는지

### 8.2 통신 계층 정책

- `read polling`과 `write command queue`를 분리한다.
- 설비별 reconnect 정책은 adapter가 가진다.
- 동일 설비에 대한 write는 설비 단위 직렬 실행을 기본으로 둔다.
- write 이후에는 read-back 또는 ack verification을 기본으로 둔다.
- timeout, retry, reconnect는 operator-visible 상태와 log를 남긴다.

### 8.3 태그 정의 정책

tag mapping은 코드에 박지 말고 설정 또는 DB에서 공급한다.

권장 필드:

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

예시:

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
  "description": "설비 운전 상태"
}
```

### 8.4 알람 처리 정책

- raw bit 또는 vendor code는 edge 또는 adapter에서 normalized alarm input으로 변환한다.
- operator-facing alarm acknowledgement가 business audit를 남겨야 한다면 BFF command를 통해 처리한다.
- alarm flood, reconnect storm, stale snapshot은 시나리오 테스트로 먼저 검증한다.

## 9. 화면 우선순위

현재 채널 정책과 맞는 WPF 우선 화면 순서는 아래가 자연스럽다.

1. station login과 session binding
2. current station work queue
3. `start-operation`, `record-material-consumption`, `complete-operation`
4. material scan, label print, reprint
5. hold placement, quality feedback
6. offline queue, reconnect, bridge status
7. 파일럿 라인에서 필요한 device handshake 전용 화면

Web-first로 남기는 화면:

- dispatch board
- hold release
- override approval
- genealogy search
- quality history review
- discrepancy dashboard
- admin 및 master data review

## 10. 권장 구현 단계

### Phase A. Station shell alignment

목표:

- shared BFF contract를 사용하는 최소 WPF shell 정립

현재 상태:

- 완료됨. station binding, queue 조회, severity-aware message, stale snapshot reset이 이미 구현되어 있다.

남은 후속:

- session recovery와 richer notification이 필요해지면 같은 seam 위에서 확장한다.

### Phase B. Operator execution station flow

목표:

- 현재 구현된 operator-execution slice를 WPF station flow로 연결

현재 상태:

- `start-operation`, `record-material-scan`, `record-material-consumption`, `complete-operation`까지 구현 완료
- queue 기반 quantity unit 투영과 command context policy도 정리 완료

남은 후속:

- hold placement, quality feedback, release authority 화면을 어떤 채널에 둘지 파일럿 workflow 기준으로 결정한다.

### Phase C. Peripheral integration

목표:

- scanner, printer, local bridge를 station UX와 안전하게 연결

현재 상태:

- example 기반 no-equipment validation 경로는 준비되어 있으나, 실제 scanner와 printer abstraction은 파일럿 대상 기준 검증이 필요하다.

남은 후속:

- scanner input abstraction
- printer service abstraction
- local bridge contract 정리
- 실패 및 재시도 시 operator feedback 정책 구체화

### Phase D. Modbus/edge integration

목표:

- 파일럿 라인에서 실제 필요한 설비만 최소 Modbus 또는 edge path로 연결

현재 상태:

- 의도적으로 보류 중이다. 실제 파일럿 라인, 설비 inventory, protocol이 확정되기 전에는 generic 구현을 키우지 않는다.

남은 후속:

- pilot equipment inventory 기반 tag shortlist 확정
- polling scheduler
- device command queue
- value parser
- reconnect와 read-back policy
- simulator 기반 verification

### Phase E. Hardening and operations

목표:

- 파일럿 도입 전 station과 edge의 운영 리스크를 검증

현재 상태:

- `example/Mes.MockStation.Example`과 smoke script로 no-equipment path는 이미 검증 가능하다.

남은 후속:

- offline replay 충돌 시나리오
- reconnect 및 stale-state 시나리오
- alarm flood
- 동시 polling 부하
- operator action audit 점검

## 11. 테스트 전략

### 11.1 단위 테스트

- ViewModel state transition
- problem-details to UI message mapping
- tag parser
- endian과 scaling conversion
- read-back 판정
- offline queue 상태 전이

### 11.2 통합 테스트

- WPF client abstraction과 BFF contract 연동
- command -> problem-details -> UI feedback 흐름
- scanner 및 printer abstraction 연동
- Modbus simulator 기반 polling과 read-back
- edge reconnect와 queue flush

### 11.3 운영 시나리오 테스트

- station network loss
- duplicate submit
- polling overload
- read-back mismatch
- alarm burst
- reconnect after stale local cache

### 11.4 No-equipment example 검증

- `example/Mes.MockStation.Example/Run-MockStationDemo.ps1`
- `example/Mes.MockStation.Example/Test-MockStationDemo.ps1`
- example은 core에 mock branch를 넣지 않고, 동일한 `Mes.ExperienceApi`와 WPF seam을 재사용해야 한다.

## 12. 주요 위험과 대응

### 12.1 WPF가 backend authority를 침범하는 위험

대응:

- command는 항상 BFF를 통해 보낸다.
- local queue는 provisional state만 가진다.

### 12.2 UI freeze

대응:

- UI thread와 device thread를 분리한다.
- async flow와 bounded queue를 사용한다.
- Dispatcher 경계를 최소화한다.

### 12.3 태그 하드코딩

대응:

- tag definition을 외부화한다.
- 설비별 parser 테스트를 유지한다.

### 12.4 파일럿 라인 미확정 상태에서 generic Modbus 구현을 과도하게 키우는 위험

대응:

- protocol, equipment, tag shortlist가 확정되기 전에는 generic framework를 과도하게 설계하지 않는다.

### 12.5 WPF와 Web semantics drift

대응:

- 같은 업무는 같은 BFF command와 같은 audit semantics를 사용한다.
- 채널별 차이는 화면 표현과 local UX에만 둔다.

### 12.6 Example과 실라인 가정이 어긋나는 위험

대응:

- example은 seam 검증용으로만 사용한다.
- 실제 파일럿 라인 확정 후 scanner, printer, handshake, offline 정책을 다시 검증한다.

## 13. 현재 기준 추천 다음 단계

이 문서를 기준으로 바로 구현을 넓히기 전에 먼저 확인해야 할 것은 아래다.

1. 파일럿 라인 한 개와 대표 제품군 한 개를 고정한다.
2. 해당 라인에 direct Modbus 또는 PLC handshake가 정말 필요한지, 아니면 scanner와 printer 중심 station shell이면 충분한지 결정한다.
3. 필요하다면 어떤 설비, 어떤 protocol, 어떤 최소 tag set이 operator workflow와 직접 연결되는지 좁힌다.
4. 그 다음에만 hold placement 또는 quality feedback surface, peripheral integration, device handshake를 실제 우선순위대로 구현한다.

## 14. 관련 문서

- 상위 아키텍처: `docs/mes/architecture-blueprint.md`
- 빠른 실행 가이드: `docs/mes/quick-start.md`
- pilot profile assumptions: `docs/mes/pilot-profile-working-assumptions.md`
- 상위 로드맵: `docs/mes/implementation-roadmap.md`
- 채널 정책: `docs/mes/client-channel-matrix.md`
- command/event 기준: `docs/mes/command-event-catalog.md`
- 현재 execution slice 설계: `docs/mes/pilot-slice-01-application-design.md`
- no-equipment example: `example/Mes.MockStation.Example/README.md`
