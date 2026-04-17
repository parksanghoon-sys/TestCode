# Mes.Application.Contracts

## 한 줄 설명

`Mes.Application.Contracts`는 WPF와 Web이 공유하는 BFF 계약과 endpoint 시그니처를 담는 프로젝트입니다. 도메인 객체를 직접 노출하지 않고, 채널 공통의 transport 모델을 별도로 유지하기 위해 존재합니다.

## 이 프로젝트가 존재하는 이유

- 도메인 모델과 HTTP/BFF 계약을 분리하기 위해 존재합니다.
- WPF와 Web이 같은 비즈니스 의미를 공유하되, 도메인 타입에 직접 결합되지 않도록 하기 위해 존재합니다.
- endpoint 이름, route, command type, request/response shape를 한곳에서 관리하기 위해 존재합니다.

## 책임 경계

- 담당
  - BFF 공통 envelope 계약
  - operator execution slice의 command/query/response 계약
  - endpoint signature metadata
  - transport 계층에서 쓰는 상수값
- 담당하지 않음
  - 도메인 상태 전이
  - 애플리케이션 오케스트레이션
  - 저장소, HTTP host, DI

## 참조 관계

- 참조하는 프로젝트: 없음
- 이 프로젝트를 참조하는 프로젝트
  - `Mes.Application`
  - `Mes.Infrastructure`
  - `Mes.ExperienceApi`
  - `Mes.ExperienceApi.Tests`

## 폴더 구조

```text
Common/
  BffContracts.cs
OperatorExecution/
  OperatorExecutionCommandTypes.cs
  OperatorExecutionContracts.cs
  OperatorExecutionEndpointSignatures.cs
```

## 핵심 파일과 클래스 의미

### `Common/BffContracts.cs`

- `BffChannelValues`
  - 명령이 들어온 채널 값을 표준화합니다.
  - 예: `wpf`, `web`, `integration`, `edge`
- `RevisionReferencesContract`
  - 요청 시점의 revision 문맥을 담습니다.
- `MeasuredQuantityContract`
  - transport 계층에서 수량과 단위를 전달합니다.
- `CommandIdentityContract`
  - `command_id`, `correlation_id`, `idempotency_key`를 묶습니다.
- `CommandOriginContract`
  - `actor_id`, `channel`, `station_id`를 묶습니다.
- `CommandContextContract`
  - Identity, Origin, ClientTimestamp, RevisionRefs를 묶는 공통 메타데이터입니다.
- `BffCommandEnvelope<TPayload>`
  - 모든 명령 계약의 공통 기반입니다.
- `BffCommandResponse`
  - 모든 명령 응답의 공통 기반입니다.
- `BffEndpointSignature`
  - operation name, HTTP method, route, request/response type을 문서와 코드에서 함께 추적하기 위한 메타데이터입니다.

### `OperatorExecution/OperatorExecutionCommandTypes.cs`

- 현재 slice에서 쓰는 canonical command type 이름을 정의합니다.
- application idempotency와 endpoint adapter가 같은 이름을 보도록 고정하는 역할을 합니다.

### `OperatorExecution/OperatorExecutionContracts.cs`

- operator execution slice의 실제 transport 모델이 모여 있습니다.
- 주요 상수
  - `HoldSubjectTypeValues`
  - `QualityDecisionValues`
  - `CompletionModeValues`
  - `QualityGateStateValues`
  - `ProductionActualsStatusValues`
- 주요 요청/응답
  - `StartOperationCommandContract`
  - `RecordMaterialConsumptionCommandContract`
  - `PlaceHoldCommandContract`
  - `ReleaseHoldCommandContract`
  - `RecordQualityResultCommandContract`
  - `CompleteOperationCommandContract`
  - `GetStationWorkQueueRequestContract`
  - `GetStationWorkQueueResponseContract`
  - `OperationStateChangedNotificationContract`

### `OperatorExecution/OperatorExecutionEndpointSignatures.cs`

- 문서에서 정의한 operator execution BFF route를 코드 메타데이터로 보존합니다.
- 현재 `Mes.ExperienceApi`는 이 시그니처를 기준으로 route를 매핑합니다.

## 현재 계약 스타일

- 모든 command는 `Context + Payload` 형태를 사용합니다.
- 이는 장문의 flat constructor를 피하고, 메타데이터와 업무 본문을 분리하기 위한 설계입니다.

## 읽는 순서 추천

1. `Common/BffContracts.cs`
2. `OperatorExecution/OperatorExecutionCommandTypes.cs`
3. `OperatorExecution/OperatorExecutionContracts.cs`
4. `OperatorExecution/OperatorExecutionEndpointSignatures.cs`

## 현재 한계

- 아직 operator execution slice 중심입니다.
- 다른 MES 모듈의 계약은 아직 정의되지 않았습니다.
- 계약은 만들어졌지만, durable persistence나 전체 인증/권한 metadata까지는 아직 포함하지 않습니다.
