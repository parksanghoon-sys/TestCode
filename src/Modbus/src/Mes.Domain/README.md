# Mes.Domain

## 프로젝트 목적

`Mes.Domain`은 MES 파일럿의 순수 도메인 모델을 담는 프로젝트입니다.  
생산 오더, 공정 실행, 자재 lot, 품질 기록, WIP 같은 핵심 개념의 상태 전이와 규칙을 이 계층에서 소유합니다.

## 책임 경계

- 담당
  - Aggregate, Entity, Value Object 정의
  - 상태 전이 규칙과 입력 검증
  - Domain Event 발생
  - hold provenance, quality decision 같은 도메인 상태 보존
  - detached persistence를 위한 aggregate/entity 복원 경계
- 담당하지 않음
  - HTTP/BFF 계약
  - application orchestration
  - repository, SQL, 파일 저장
  - DI, host, 인증, 로깅

## 폴더 구조

```text
Abstractions/
  AggregateRoot.cs
  DomainException.cs
  IDomainEvent.cs
Aggregates/
  ProductionOrder.cs
  OperationExecution.cs
  MaterialLot.cs
  QualityRecord.cs
  OverrideRequest.cs
Common/
  DomainGuard.cs
  HoldSourceTypes.cs
Entities/
  WipUnit.cs
Events/
  DomainEvents.cs
Statuses/
  DomainStatuses.cs
ValueObjects/
  Identifiers.cs
  MeasuredQuantity.cs
  GenealogyLink.cs
```

## 주요 클래스와 의미

### `ProductionOrder`

- 생산 오더의 lifecycle을 표현합니다.
- `Release`, `AttachOperation`, `MarkInProgress`, `MarkPartiallyCompleted`, `MarkCompleted`, `Close`, `Cancel`을 담당합니다.
- `Restore(ProductionOrderRestoreState)`로 영속 스냅샷에서 detached aggregate를 복원할 수 있습니다.

### `OperationExecution`

- operator execution slice의 중심 aggregate입니다.
- `QueueForExecution`, `Start`, `Pause`, `Resume`, `PlaceHold`, `ReleaseHold`, `RecordScrap`, `Complete`를 담당합니다.
- `StatusBeforeHold`, `HoldSourceType`, `HoldSourceId`로 hold provenance를 유지합니다.
- `Restore(OperationExecutionRestoreState)`로 파일/DB 스냅샷에서 정확한 실행 상태를 복원할 수 있습니다.

### `MaterialLot`

- 라인 투입, 소모, 반납, block, genealogy 생성 규칙을 담당합니다.
- `ConsumeFor`가 genealogy link와 material consumption domain event를 함께 만듭니다.
- `Restore(MaterialLotRestoreState)`로 genealogy까지 포함한 lot 상태를 복원합니다.

### `QualityRecord`

- 검사 시작, 합격/불합격 판정, hold/release를 담당합니다.
- 현재 workflow 상태와 마지막 판정 결과를 분리해서 보존합니다.
- `DecisionStatus`는 마지막 판정, `Status`는 현재 hold 포함 workflow 상태입니다.
- `Restore(QualityRecordRestoreState)`로 품질 게이트 상태를 재구성합니다.

### `WipUnit`

- 현재 WIP 단위의 진행 상태와 연결된 공정 실행을 표현합니다.
- `PlaceHold`와 `ReleaseHold`는 내부적으로 `StatusBeforeHold`를 유지합니다.
- `Restore(WipUnitRestoreState)`로 detached persistence 경계에서 다시 만들 수 있습니다.

### `AggregateRoot<TId>`

- 모든 aggregate의 공통 기반입니다.
- domain event 컬렉션을 관리하고 `Raise`, `ClearDomainEvents`를 제공합니다.

## 이번 사이클에서 중요한 설계 포인트

- in-memory reference adapter만 있을 때는 객체 참조가 상태를 대신 유지했지만, durable store에서는 그 가정이 깨집니다.
- 그래서 `ProductionOrder`, `OperationExecution`, `MaterialLot`, `QualityRecord`, `WipUnit`에 모두 명시적 `Restore(...)` 경계를 추가했습니다.
- 의도는 persistence 기술이 바뀌어도 domain 규칙은 그대로 두고, 인프라가 스냅샷을 aggregate로 다시 세울 수 있게 만드는 것입니다.

## 의존 관계

- 참조하는 프로젝트: 없음
- 이 프로젝트를 참조하는 프로젝트
  - `Mes.Application`
  - `Mes.Infrastructure`
  - `Mes.Domain.Tests`

## 읽는 순서 추천

1. `Statuses/DomainStatuses.cs`
2. `ValueObjects/Identifiers.cs`
3. `Abstractions/AggregateRoot.cs`
4. `Aggregates/OperationExecution.cs`
5. `Aggregates/QualityRecord.cs`
6. `Aggregates/ProductionOrder.cs`
7. `Aggregates/MaterialLot.cs`
8. `Entities/WipUnit.cs`
9. `Events/DomainEvents.cs`

## 현재 한계

- domain 자체는 여전히 persistence 기술을 모릅니다.
- repository 인터페이스나 concurrency 제어는 아직 없습니다.
- multi-operation order completion 판정은 아직 application/integration 후속 과제입니다.
- override request는 domain seed에 있지만 현재 operator execution slice의 durable adapter 경로에는 아직 포함되지 않습니다.
