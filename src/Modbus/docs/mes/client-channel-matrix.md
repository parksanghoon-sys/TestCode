# MES Client Channel Matrix

## 1. 목적

이 문서는 파일럿 MES 범위에서 어떤 역할과 어떤 워크플로우가 `WPF-first`, `Web-first`, `Shared`인지 명시적으로 정의한다.

이 문서의 목적은 다음과 같다.

- 채널 선택 때문에 업무 semantics가 갈라지지 않도록 기준을 고정
- operator-critical flow를 어디까지 WPF에 둘지 명확히 함
- web portal이 어디까지 실행 채널인지, 어디부터 조회/관리 채널인지 분리
- 오프라인 허용 범위를 역할과 업무 맥락으로 해석 가능하게 만듦

## 2. 파일럿 기본 가정

- 파일럿은 single-site, single-pilot-line 기준이다.
- operator station은 Windows 기반 단말을 사용하며 scanner, printer, 현장 주변기기와 연결된다.
- supervisory and management users는 브라우저 접근이 가능하다.
- 모든 business command는 `Experience API / BFF`를 통해 MES core에 들어간다.
- `WPF -> Edge` 직접 연결은 device bridge 목적에만 사용한다.

## 3. 채널 판정 원칙

- `WPF-first`: scan-heavy, printer-heavy, kiosk, local device dependency, offline tolerance가 핵심인 업무
- `Web-first`: 다수 사용자 동시 조회, cross-line visibility, admin workflow, configuration, dashboard 중심 업무
- `Shared`: 같은 업무 semantics를 유지한 채 표현 방식만 다르게 제공 가능한 경우

## 4. 역할별 채널 원칙

| 역할 | 주요 책임 | 기본 채널 |
|---|---|---|
| Operator | 작업 시작/완료, 자재 스캔, 현장 입력, 라벨 출력 | WPF-first |
| Line Leader | station 지원, 예외 대응, 현장 승인 요청 | WPF-first, 일부 Web |
| Production Supervisor | dispatch 확인, 진행률 확인, 병목/예외 모니터링 | Web-first |
| Quality Inspector | 검사 결과 조회, hold 상태 확인, quality exception 관리 | Web-first, 현장 입력은 WPF 가능 |
| Planner | dispatch board, order progress, 일정 조정 참고 | Web-first |
| Warehouse Coordinator | line-side discrepancy 확인, material status 조회 | Web-first |
| Admin/Master Data Owner | 설정, 조회, 기준정보 검토 | Web-first |
| OT/Equipment Support | 장비 상태 확인, edge/bridge 상태 확인 | Web-first, 현장 보조 시 WPF 가능 |

## 5. 워크플로우 매트릭스

| Workflow | Primary role | Preferred channel | Offline allowed | Peripheral dependency | Shared 여부 | 비고 |
|---|---|---|---|---|---|---|
| 로그인 후 station session 시작 | Operator | WPF-first | Conditional | scanner, station binding | No | 현장 단말/작업 위치 맥락 필요 |
| 작업 queue 조회 | Operator | Shared | Yes | none | Yes | WPF는 실행 직전, Web은 조회 중심 |
| 작업 시작 | Operator | WPF-first | Conditional | station context | No | kiosk and speed 중요 |
| 작업 일시정지/재개 | Operator | WPF-first | Conditional | station context | No | 현장 즉시성 중요 |
| 작업 완료 | Operator | WPF-first | Conditional | scanner, equipment ack | No | device/equipment evidence 연계 가능 |
| 자재 lot/serial 스캔 | Operator | WPF-first | Conditional | scanner | No | 가장 강한 WPF 우선 업무 |
| 자재 소모/반납 기록 | Operator | WPF-first | Conditional | scanner, local feedback | No | provisional queue 가능 |
| scrap 기록 | Operator, line leader | WPF-first | Conditional | scanner optional | No | 현장 즉시 처리 필요 |
| hold 설정 | Operator, quality | WPF-first | Yes | none | Limited | safety-oriented hold는 현장 허용 |
| hold release | Quality, supervisor | Web-first | No | none | No | 중앙 승인과 audit 필요 |
| override 요청 | Operator, line leader | WPF-first | Conditional | station context | No | 요청은 현장, 승인 자체는 중앙 |
| override 승인/반려 | Supervisor, quality | Web-first | No | none | No | authority, audit, optional e-signature |
| dispatch board 조회 | Supervisor, planner | Web-first | No | none | No | 다중 사용자, wider visibility |
| 진행률/병목 모니터링 | Supervisor, manager | Web-first | No | none | No | dashboard 성격 |
| genealogy search | Quality, warehouse, support | Web-first | No | none | No | cross-line search |
| 품질 이력 조회 | Quality | Web-first | No | none | No | hold/release 판단 참고 |
| 관리자 설정/기준정보 검토 | Admin | Web-first | No | none | No | broad reach, governance 필요 |
| label 재출력 | Operator, line leader | WPF-first | Conditional | printer | No | local printer dependency |
| edge/device bridge 상태 확인 | OT support | Web-first | No | none | No | 운영 관제 성격 |

## 6. 화면 묶음 권장안

### 6.1 WPF Station Client

- station login and session binding
- work queue focused on current station
- start, pause, resume, complete
- material scan and consumption
- label print and reprint
- hold placement
- override request
- offline queue status and replay feedback

### 6.2 Web Portal

- dispatch board
- line and station monitoring
- quality history, hold list, exception review
- genealogy search
- master data review
- warehouse discrepancy dashboard
- edge/queue operational visibility
- override approval and hold release

## 7. Shared 업무 규칙

- `Shared`로 분류된 화면도 command semantics는 반드시 동일해야 한다.
- 동일 업무는 같은 command name, 같은 validation, 같은 audit format을 사용한다.
- WPF는 execution speed와 peripheral UX를 최적화하고, Web은 visibility와 reach를 최적화한다.
- 채널 차이 때문에 상태 전이 규칙이 달라지면 안 된다.

## 8. 오프라인 해석 가이드

- `Conditional`은 단순 허용이 아니라 아래 조건을 만족할 때만 허용한다.
  - 유효한 station session이 존재함
  - 필요한 revision과 permission snapshot이 local cache에 있음
  - local queue 적재와 replay 식별자가 생성됨
  - operator가 provisional 상태임을 볼 수 있음
- 오프라인 중 발생한 command는 authoritative completion이 아니라 `pending replay` 상태로 본다.
- 오프라인 허용 업무라도 `hold release`, `override approval`, `ERP posting`, `master change`는 포함하지 않는다.

## 9. 파일럿 기준 우선 설계 대상

1. WPF station login/session
2. WPF operator execution flow
3. WPF material scan and label print
4. Web dispatch board and supervisor monitoring
5. Web hold/override approval flow
6. Web genealogy and discrepancy dashboard

## 10. 다음 단계

- 파일럿 라인 실제 역할명을 이 문서의 generic role에 매핑
- 각 workflow를 command/event catalog와 연결
- WPF와 Web 각각에 필요한 최소 화면 집합을 정의
- `Conditional offline` 업무에 대한 실제 replay UX를 설계
