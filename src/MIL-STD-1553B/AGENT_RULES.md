# AGENT_RULES

## 목적

- 이 파일은 연속 세션에서 가장 먼저 읽는 작업 인덱스이다.
- 상세 저장소 정책은 `AGENTS.md`를 기준으로 하고, 현재 실행 상태는 `CURRENT_PLAN.md`, `TODOS.md`, `DECISIONS.md`, `CHANGELOG_AGENT.md`를 따른다.

## 항상 먼저 확인할 문서

1. `AGENTS.md`
2. `CURRENT_PLAN.md`
3. `TODOS.md`
4. 최신 `CHANGELOG_AGENT.md`
5. 최신 `DECISIONS.md`
6. `Agent_Plan_MIL_STD_1553B.md`

## 작업 순서

1. 설계 검토
2. 구현
3. 테스트 및 검증

## 현재 기준선

- 2026-04-17 설계 기준선 문서: `docs/design/2026-04-17_전체하네스MVP_설계검토.md`
- 현재 아키텍처 결정은 `DECISIONS.md`의 최신 항목을 우선한다.

## 기록 규칙

- 매 사이클 종료 시 `CURRENT_PLAN.md`, `TODOS.md`, `CHANGELOG_AGENT.md`를 갱신한다.
- 의미 있는 구조, 경계, 범위 결정은 `DECISIONS.md`에 남긴다.
- 구현 단계에서는 `docs/test/` 아래 테스트 계획 및 결과 문서를 함께 관리한다.
