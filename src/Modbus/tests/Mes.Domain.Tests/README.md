# Mes.Domain.Tests

## 한 줄 설명

`Mes.Domain.Tests`는 `Mes.Domain`의 Aggregate와 Entity가 기대한 상태 전이 규칙을 지키는지 검증하는 테스트 프로젝트입니다.

## 이 프로젝트가 존재하는 이유

- 도메인 규칙이 application이나 infrastructure 없이도 독립적으로 유지되는지 확인하기 위해 존재합니다.
- operator execution slice의 핵심 상태 전이를 가장 낮은 레벨에서 잠그기 위해 존재합니다.

## 참조 관계

- 참조하는 프로젝트
  - `Mes.Domain`
- 주요 패키지
  - `xUnit`
  - `Microsoft.NET.Test.Sdk`
  - `coverlet.collector`

## 폴더 구조

```text
MaterialLotTests.cs
OperationExecutionTests.cs
OperatorExecutionWorkflowTests.cs
OverrideRequestTests.cs
ProductionOrderTests.cs
QualityRecordTests.cs
```

## 테스트 파일 의미

- `ProductionOrderTests`
  - 생산 오더 release와 공정 연결, 상태 전이 기본 규칙을 검증합니다.
- `OperationExecutionTests`
  - 공정 실행의 queue, start, hold, release, complete 전이를 검증합니다.
- `MaterialLotTests`
  - 자재 소모, 반납, genealogy link 생성 규칙을 검증합니다.
- `QualityRecordTests`
  - 품질 기록의 inspection 시작, 판정, hold/release 규칙을 검증합니다.
- `OverrideRequestTests`
  - 예외 승인 요청, 승인, 반려 흐름을 검증합니다.
- `OperatorExecutionWorkflowTests`
  - 선택된 파일럿 slice 기준 happy path와 hold gate 예외 흐름을 도메인 수준에서 검증합니다.

## 테스트 스타일

- 가능하면 mocking 없이 실제 도메인 객체를 직접 조합합니다.
- 하나의 테스트는 규칙 하나를 선명하게 보여주는 방향을 우선합니다.
- application 계층 책임은 여기서 검증하지 않습니다.

## 이 프로젝트를 읽으면 좋은 사람

- 도메인 모델부터 이해하고 싶은 사람
- Aggregate 규칙이 어떻게 잠겨 있는지 확인하고 싶은 사람
- 상위 계층 변경 전에 도메인 회귀 가능성을 먼저 보고 싶은 사람

## 현재 한계

- cross-aggregate coordination은 여기서 다루지 않습니다.
- receipt replay, application service orchestration, endpoint route 같은 내용은 상위 테스트 프로젝트에서 검증합니다.
