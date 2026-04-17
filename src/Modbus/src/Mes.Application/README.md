# Mes.Application

## 한 줄 설명

`Mes.Application`은 도메인 객체를 조합해 실제 use case를 실행하는 애플리케이션 계층입니다. 품질 hold 조정, 멱등성 판단, command handling, query composition, adapter-facing port orchestration을 담당합니다.

## 이 프로젝트가 존재하는 이유

- 도메인 Aggregate 하나로 끝나지 않는 흐름을 조정하기 위해 존재합니다.
- 중복 명령 재시도, stored response replay, quality gate 조정 같은 cross-aggregate 정책을 한곳에 두기 위해 존재합니다.
- 상위 호스트나 인프라가 비즈니스 분기를 직접 구현하지 않도록 하기 위해 존재합니다.

## 책임 경계

- 담당
  - cross-aggregate workflow coordination
  - idempotency 평가와 receipt 생성
  - preloaded state 기반 command handling
  - station work queue query 조합
  - adapter-facing application service
- 담당하지 않음
  - 실제 저장소 구현
  - 실제 HTTP route 매핑
  - UI 계약 정의

## 참조 관계

- 참조하는 프로젝트
  - `Mes.Domain`
  - `Mes.Application.Contracts`
- 이 프로젝트를 참조하는 프로젝트
  - `Mes.Infrastructure`
  - `Mes.Application.Tests`
  - `Mes.ExperienceApi`

## 폴더 구조

```text
Idempotency/
  CanonicalCommandFingerprintBuilder.cs
  CommandReceiptIdempotencyPolicy.cs
  CommandReceiptModels.cs
OperatorExecution/
  OperatorExecutionApplicationPorts.cs
  OperatorExecutionApplicationRequests.cs
  OperatorExecutionApplicationService.cs
  OperatorExecutionCommandHandler.cs
  OperatorExecutionCommandHandler.Shared.cs
  OperatorExecutionCommandHandlingModels.cs
  OperatorExecutionExceptions.cs
  ProductionActualsPreparationService.cs
  QualityGateContext.cs
  QualityGateRequests.cs
  QualityGateSnapshot.cs
  QualityHoldGateCoordinator.cs
  StoredCommandResponseSerializer.cs
  WorkQueue/
    GetStationWorkQueueQueryHandler.cs
    OperationMaterialRequirementProjector.cs
    OperationMaterialRequirementProjectionModels.cs
    StationWorkQueueModels.cs
    StationWorkQueueReadService.cs
```

## 핵심 파일과 클래스 의미

### `Idempotency/`

- `CanonicalCommandFingerprintBuilder`
  - command payload에서 canonical business fingerprint를 계산합니다.
- `CommandReceiptIdempotencyPolicy`
  - 기존 receipt와 새 요청을 비교해 accept, replay, conflict를 판단합니다.
- `CommandReceiptModels`
  - receipt scope, stored receipt, result code, evaluation result 등을 정의합니다.

### `OperatorExecution/`

- `OperatorExecutionCommandHandler`
  - 현재 slice의 command handling 핵심입니다.
  - start, material consumption, hold, release, quality result, complete 흐름을 처리합니다.
- `OperatorExecutionCommandHandlingModels`
  - handler가 필요로 하는 preloaded state와 결과 모델을 모아둡니다.
- `OperatorExecutionExceptions`
  - host가 안정적인 problem details로 승격할 수 있도록 `not_found`, `conflict`, `validation_failed` 분류를 정의합니다.
- `QualityHoldGateCoordinator`
  - `QualityRecord`와 `OperationExecution` 사이의 hold propagation 정책을 담당합니다.
- `ProductionActualsPreparationService`
  - 공정 완료 후 `production_actuals_batch` 초안을 준비합니다.
- `StoredCommandResponseSerializer`
  - replay를 위해 응답을 deterministic하게 직렬화하고 복원합니다.
- `OperatorExecutionApplicationPorts`
  - 인프라 구현이 따라야 할 load/save port 인터페이스입니다.
- `OperatorExecutionApplicationRequests`
  - application service 진입 요청 모델입니다.
- `OperatorExecutionApplicationService`
  - load -> receipt lookup -> handler -> save 흐름을 오케스트레이션합니다.

### `OperatorExecution/WorkQueue/`

- `OperationMaterialRequirementProjector`
  - operation attachment를 기준으로 MES-side 자재 requirement snapshot을 생성합니다.
- `StationWorkQueueReadService`
  - execution state, WIP, quality, requirement snapshot을 합쳐 작업 큐 스냅샷을 만듭니다.
- `GetStationWorkQueueQueryHandler`
  - application query 결과를 transport 응답으로 매핑합니다.
- `StationWorkQueueModels`
  - read model 전용 identity/state/item 구조를 정의합니다.

## 대표 실행 흐름

### Command 흐름

1. 상위 계층이 command 계약과 서버 수신 시각을 `OperatorExecutionApplicationService`에 전달합니다.
2. Service가 port를 통해 상태와 기존 receipt를 로드합니다.
3. `OperatorExecutionCommandHandler`가 도메인 규칙과 coordinator를 호출합니다.
4. accept된 결과는 receipt, prepared batch, domain event 기준으로 저장 후보가 됩니다.
5. 저장은 port 하나의 `SaveAsync`로 내려갑니다.

### Query 흐름

1. 상위 계층이 station ID 기준으로 query를 요청합니다.
2. source port가 MES-side snapshot을 로드합니다.
3. `StationWorkQueueReadService`와 `GetStationWorkQueueQueryHandler`가 큐 응답을 구성합니다.

## 읽는 순서 추천

1. `Idempotency/CommandReceiptModels.cs`
2. `Idempotency/CanonicalCommandFingerprintBuilder.cs`
3. `OperatorExecution/QualityHoldGateCoordinator.cs`
4. `OperatorExecution/OperatorExecutionCommandHandler.cs`
5. `OperatorExecution/OperatorExecutionApplicationPorts.cs`
6. `OperatorExecution/OperatorExecutionApplicationService.cs`
7. `OperatorExecution/WorkQueue/StationWorkQueueReadService.cs`

## 현재 구현 범위

- operator execution slice 한정 애플리케이션 정책이 구현되어 있습니다.
- quality hold gate, command receipt idempotency, order completion progression, work queue read model, production actuals 준비, host-facing exception taxonomy까지 포함합니다.

## 현재 한계

- provider별 transaction, concurrency, physical persistence semantics는 이 프로젝트 바깥의 infrastructure 책임입니다.
- malformed transport input, authentication and authorization, HTTP problem-details 매핑 같은 transport semantics는 host 책임입니다.
- pilot line scope와 master data ownership 같은 외부 운영 전제는 아직 확정되지 않았습니다.
