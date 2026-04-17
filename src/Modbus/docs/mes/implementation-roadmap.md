# MES Implementation Roadmap

## 목표

이 문서는 아키텍처 초안을 실제 프로젝트 실행 순서로 바꾸기 위한 초기 로드맵이다. 일정 수치는 조직 규모와 기존 시스템 상태에 따라 달라질 수 있으므로, 여기서는 순서와 게이트를 우선 정의한다.

## Phase 0. Discovery and Baseline

### 목표

- 파일럿 사이트와 라인 범위를 고정
- 현재 업무 흐름과 시스템 경계를 확인
- master data 품질과 interface readiness를 점검

### 산출물

- current-state process map
- ownership matrix
- pilot line scope
- pilot profile assumption sheet
- canonical object shortlist
- interface inventory
- role-by-workflow channel matrix
- offline authority matrix
- release 1 material reconciliation rule
- release 1 quality authority rule
- initial BFF command/event catalog

### 완료 기준

- 어떤 이벤트를 MES가 authoritative하게 소유할지 합의됨
- ERP, WMS, QMS, SCADA 담당자와 인터페이스 범위가 합의됨
- MVP 대상 제품군과 작업 흐름이 확정됨
- `BFF`와 `Edge`의 command ownership 경계가 합의됨

## Phase 1. Execution MVP

### 목표

- 생산오더 수신부터 작업 완료 및 실적 회신까지의 핵심 실행 루프 구축

### 포함 범위

- order release ingestion
- dispatch board 또는 station work queue
- 작업 시작, 일시정지, 완료, 재작업
- barcode scan 기반 material verification
- 기본 genealogy
- quality hold/release
- ERP actual posting
- edge buffering
- WPF station client for operator-critical flow
- web portal for supervisory view and exception monitoring
- `BFF` 기반 task command catalog
- WMS-lite reconciliation
- MES-owned in-process hold/release

### 완료 기준

- 파일럿 라인에서 수동 엑셀/수기 없이 작업 추적 가능
- good, scrap, material consumption이 재현 가능
- duplicate event와 offline recovery가 검증됨
- WPF와 Web이 같은 command semantics와 audit log를 사용함
- line-side discrepancy가 정의된 reconciliation 절차로 닫힘

## Phase 2. Quality and Material Deepening

### 목표

- 품질 실행과 material traceability를 실사용 수준으로 강화

### 포함 범위

- inspection plan versioning
- nonconformance trigger
- substitution control
- WMS 연계
- pallet 또는 container genealogy
- hold/release rule 확장

### 완료 기준

- 품질 hold 이력과 genealogy가 출하 판단에 사용 가능
- line-side material discrepancy를 reconcile할 수 있음

## Phase 3. Equipment and Performance Visibility

### 목표

- 설비 이벤트와 생산 실행을 더 강하게 연결

### 포함 범위

- equipment eligibility
- downtime reason capture
- basic OEE
- Andon/event alert
- historian correlation

### 완료 기준

- 설비 상태와 생산 상태가 같은 언어로 해석됨
- 현장 다운타임 원인과 작업 영향이 연결됨

## Phase 4. Multi-Site and Optimization

### 목표

- 공통 템플릿화와 고도화된 최적화 기반 마련

### 포함 범위

- site template
- shared integration contracts
- APS feedback loop refinement
- advanced analytics
- governance and rollout playbook

### 완료 기준

- 신규 사이트 적용 시 core model 재사용 가능
- 운영 규칙과 배포 절차가 문서화됨

## 병행 워크스트림

| 워크스트림 | 핵심 책임 |
|---|---|
| Process and Product | 공정 흐름, SOP, 예외 시나리오, 현장 adoption |
| Master Data | item/BOM/routing/resource/spec 정합성 확보 |
| Integration | ERP/WMS/QMS/SCADA interface 설계와 검증 |
| Client Experience | WPF station shell, web portal, BFF contract, role-based workflow split, offline behavior |
| Platform and Ops | 배포, observability, backup, security, edge 운영 |
| Validation | SIT/UAT, pilot rehearsal, cutover plan, rollback strategy |

## 권장 팀 구성

- Product Owner 또는 MES PM
- MES Architect
- Domain Lead for Manufacturing Process
- Integration Engineer
- Backend/Application Engineer
- Client Engineer for WPF/Web
- QA/Validation Lead
- OT/Equipment Integration Engineer

## 첫 3주 권장 액션

1. pilot line value stream을 event 기준으로 그린다.
2. `docs/mes/pilot-profile-working-assumptions.md`의 manufacturing mode, genealogy depth, and device-handshake assumptions를 실제 파일럿 라인 기준으로 검증한다.
3. 생산오더, 작업, WIP, material lot, quality hold에 대한 canonical ID 규칙을 정한다.
4. `BFF` command path와 `Edge` device path를 나눠 command ownership을 문서화한다.
5. operator station의 최소 화면 흐름을 정의하고 WPF 우선 화면과 web 우선 화면을 나눈다.
6. offline 시나리오, duplicate event 시나리오, line-side reconciliation 시나리오를 테스트 케이스로 먼저 만든다.

## 참조 상세 문서

- `docs/mes/client-channel-matrix.md`
- `docs/mes/command-event-catalog.md`

## 주요 리스크

- master data 품질이 낮으면 execution 자동화보다 예외 처리만 늘어난다.
- line-side inventory ownership이 불명확하면 WMS/MES 경계 충돌이 커진다.
- 현장 우회 프로세스를 무시하면 시스템 도입 후 shadow process가 남는다.
- equipment integration을 너무 빨리 넓히면 MVP 일정이 쉽게 무너진다.
- KPI를 먼저 만들고 execution truth를 나중에 만들면 숫자 신뢰가 무너진다.
- WPF/Web 화면이 각각 다른 workflow semantics를 가지면 audit와 운영 규칙이 쉽게 깨진다.
- `Edge`가 장비 브리지를 넘어서 business command path로 확장되면 채널별 규칙 분기가 다시 생긴다.
