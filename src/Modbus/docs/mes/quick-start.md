# MES Quick Start

## 1. 문서 목적

이 문서는 현재 저장소에서 바로 실행 가능한 MES pilot 경로를 빠르게 따라할 수 있도록
사용법과 테스트 명령만 모아 둔 퀵 가이드다.

설계 배경은 아래 문서를 참고한다.

- `docs/mes/architecture-blueprint.md`
- `docs/mes/pilot-slice-01-application-design.md`
- `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md`
- `docs/mes/command-event-catalog.md`

## 2. 현재 바로 실행 가능한 범위

현재 코드 기준으로 바로 실행 가능한 operator-execution 범위는 아래다.

### 2.1 Backend and BFF

- `start-operation`
- `record-material-scan`
- `record-material-consumption`
- `place-hold`
- `release-hold`
- `record-quality-result`
- `complete-operation`

### 2.2 WPF station shell

현재 WPF shell에서 직접 사용할 수 있는 command는 아래다.

- `start-operation`
- `record-material-scan`
- `record-material-consumption`
- `complete-operation`

아래 command는 backend contract와 API route는 있지만 현재 WPF 화면에는 아직 노출되지 않았다.

- `place-hold`
- `record-quality-result`
- `release-hold`

## 3. 가장 빠른 실행 방법

가장 쉬운 방법은 no-equipment example을 쓰는 것이다.
이 경로는 실제 PLC, Modbus, scanner 장비 없이도 현재 BFF와 WPF shell을 바로 검증할 수 있다.

### 3.1 API와 WPF를 같이 실행

```powershell
pwsh -File example/Mes.MockStation.Example/Run-MockStationDemo.ps1 -LaunchWpf
```

이 스크립트가 해주는 일:

- example SQLite DB와 manifest 생성
- Experience API를 새 PowerShell 창에서 실행
- WPF shell을 새 PowerShell 창에서 실행
- example station, actor, operation ID를 콘솔에 출력

### 3.2 Headless smoke test만 실행

```powershell
pwsh -File example/Mes.MockStation.Example/Test-MockStationDemo.ps1
```

이 smoke test는 아래 흐름을 자동 검증한다.

- work queue 조회
- `start-operation`
- `record-material-consumption`
- `complete-operation`

## 4. 수동 실행 방법

no-equipment example 대신 API와 WPF를 수동으로 따로 띄우고 싶다면 아래 순서를 사용한다.

### 4.1 Example runtime 자산 먼저 생성

```powershell
dotnet run --project example/Mes.MockStation.Example/Mes.MockStation.Example.csproj -- --output-root example/Mes.MockStation.Example/.runtime
```

생성 결과:

- `example/Mes.MockStation.Example/.runtime/operator-execution-example.db`
- `example/Mes.MockStation.Example/.runtime/mock-station-scenario.json`

### 4.2 Experience API 실행

```powershell
$env:Mes__OperatorExecutionSqliteDatabasePath = (Resolve-Path 'example/Mes.MockStation.Example/.runtime/operator-execution-example.db')
$env:ASPNETCORE_URLS = 'http://127.0.0.1:51398/'
dotnet run --no-launch-profile --project src/Mes.ExperienceApi/Mes.ExperienceApi.csproj
```

메모:

- 별도 provider 설정이 없으면 host는 기본적으로 `Sqlite`를 사용한다.
- `Postgres`는 아직 reserved provider일 뿐이고 구현되어 있지 않다.

### 4.3 WPF shell 실행

다른 PowerShell 창에서:

```powershell
$env:Mes__OperatorExecutionBff__BaseAddress = 'http://127.0.0.1:51398'
$env:Mes__OperatorExecutionBff__DefaultStationId = 'ST-EXAMPLE-01'
$env:Mes__OperatorExecutionBff__DefaultActorId = 'operator.mock.example'
$env:Mes__OperatorExecutionBff__DefaultCompletionQuantityUnit = 'EA'
dotnet run --no-launch-profile --project src/Mes.Client.Wpf/Mes.Client.Wpf.csproj
```

### 4.4 Example 기본 식별자

manual test에 바로 쓸 수 있는 example 값:

- Station: `ST-EXAMPLE-01`
- Actor: `operator.mock.example`
- Queued operation: `OP-EXAMPLE-START`
- Running operation: `OP-EXAMPLE-RUN`
- Running WIP: `WIP-EXAMPLE-01`
- Running material lot: `LOT-EXAMPLE-01`

## 5. 권장 사용 순서

처음 보는 사람 기준으로는 아래 순서가 가장 안전하다.

1. `pwsh -File example/Mes.MockStation.Example/Test-MockStationDemo.ps1`로 smoke를 먼저 돌린다.
2. `pwsh -File example/Mes.MockStation.Example/Run-MockStationDemo.ps1 -LaunchWpf`로 API와 WPF를 같이 띄운다.
3. WPF에서 station bind 후 queue를 새로고침한다.
4. queued item에서 `start-operation`을 실행한다.
5. running item에서 `record-material-scan`으로 authoritative material code와 unit feedback을 확인한다.
6. 같은 running item에서 `record-material-consumption`을 실행한다.
7. 마지막으로 `complete-operation`을 실행한다.

## 6. 자주 쓰는 테스트 명령

### 6.1 가장 빠른 smoke

```powershell
pwsh -File example/Mes.MockStation.Example/Test-MockStationDemo.ps1
```

### 6.2 프로젝트별 focused test

```powershell
dotnet test tests/Mes.Domain.Tests/Mes.Domain.Tests.csproj -v minimal
dotnet test tests/Mes.Application.Tests/Mes.Application.Tests.csproj -v minimal
dotnet test tests/Mes.ExperienceApi.Tests/Mes.ExperienceApi.Tests.csproj -v minimal
dotnet test tests/Mes.Client.Wpf.Tests/Mes.Client.Wpf.Tests.csproj -v minimal
```

### 6.3 전체 회귀

```powershell
dotnet build Mes.slnx -v minimal
dotnet test Mes.slnx -v minimal
```

## 7. 현재 설정 포인트

### 7.1 WPF appsettings

기본 WPF 설정 파일:

- `src/Mes.Client.Wpf/appsettings.json`

기본값:

- BaseAddress: `http://localhost:51398/`
- DefaultStationId: `ST-1001`
- DefaultActorId: `operator.demo`
- DefaultCompletionQuantityUnit: `EA`

example를 쓸 때는 env var override로 `ST-EXAMPLE-01`과 `operator.mock.example`를 주는 편이 맞다.

### 7.2 Experience API provider 관련

현재 host 구성의 핵심 키:

- `Mes:OperatorExecutionDurableProvider`
- `Mes:OperatorExecutionSqliteDatabasePath`
- `Mes:OperatorExecutionFileStorePath`
- `Mes:OperatorExecutionConnectionString`

현재 provider 상태:

- `Sqlite`: active default runtime
- `FileStore`: comparison-only path
- `Postgres`: future reserved provider

## 8. 현재 제한사항

- real scanner, printer, local bridge abstraction은 아직 붙지 않았다.
- generic Modbus or edge path는 실제 파일럿 라인과 설비 inventory 확정 전까지 의도적으로 보류 중이다.
- offline replay conflict, reconnect or stale-state, alarm flood 같은 운영 hardening 시나리오는 아직 후속 작업이다.
- WPF shell에는 아직 `place-hold`, `record-quality-result`, `release-hold` 화면이 없다.

## 9. 문제 해결 팁

### 9.1 포트 충돌

`Run-MockStationDemo.ps1`는 기본 포트가 사용 중이면 자동으로 빈 포트를 잡아 준다.
수동 실행 시에는 `ASPNETCORE_URLS`를 직접 바꿔서 사용한다.

### 9.2 WPF에서 queue가 비어 보일 때

아래를 먼저 확인한다.

- API가 실제로 떠 있는지
- WPF `BaseAddress`가 API 주소와 맞는지
- station ID가 example station과 같은지
- example SQLite DB가 생성되었는지

### 9.3 command는 되는데 장비 경로는 없는 이유

현재 저장소의 baseline은 shared BFF seam 검증까지다.
scanner, printer, Modbus, device handshake는 후속 phase로 분리되어 있다.

## 10. 관련 문서

- 상위 설계: `docs/mes/architecture-blueprint.md`
- pilot application hardening: `docs/mes/pilot-slice-01-application-design.md`
- station and edge sub-plan: `docs/mes/MES_WPF_Modbus_IMPLEMENTATION_PLAN.md`
- command and event vocabulary: `docs/mes/command-event-catalog.md`
- no-equipment example detail: `example/Mes.MockStation.Example/README.md`
