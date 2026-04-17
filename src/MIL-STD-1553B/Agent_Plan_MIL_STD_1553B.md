# Agent_Plan — MIL-STD-1553B 예제 프로젝트 설계방안

## 1. 문서 목적

이 문서는 **MIL-STD-1553B를 사용하는 예제 프로젝트**를 설계하기 위한 실행형 계획서이다.  
목표는 다음 3가지를 동시에 만족하는 것이다.

1. **표준 역할(BC / RT / BM)** 을 명확히 분리한다.
2. **C++ 기반의 네이티브 계층**과 **C# 기반의 운영/응용 계층**을 함께 제시한다.
3. 실제 하드웨어 카드가 있든 없든, **시뮬레이터 모드와 실장비 모드**를 모두 지원하는 구조를 만든다.

---

## 2. 대상 예제 프로젝트 정의

프로젝트 이름 예시: **Agent1553Lab**

### 핵심 기능
- **BC(Bus Controller)** 시나리오 실행
- **RT(Remote Terminal)** 응답 시뮬레이션 또는 실장비 연동
- **BM(Bus Monitor)** 기반 트래픽 수집/기록
- **A/B 이중 버스 전환** 및 장애 복구 실험
- **주기 스케줄링**(예: 20ms, 50ms, 100ms 메시지)
- **로그 저장 / 재생 / 필터링**
- **에러 주입**(타임아웃, 잘못된 응답, 재시도 유도)
- **JSON 설정 기반 구성 변경**

### 예제 시나리오
- BC가 RT 1번에 20ms 주기로 상태 요청
- RT 2번에서 센서 데이터 전송
- BM이 전체 메시지를 time-tag와 함께 기록
- A 버스 오류 발생 시 B 버스로 전환
- 특정 Mode Code를 이용한 동기화/상태 점검

---

## 3. 표준 기반 핵심 전제

MIL-STD-1553 계열은 **digital time division command/response multiplex data bus**로 정의되며, 시스템 통합을 위한 데이터 버스의 개념, 정보 흐름, 전기적/기능적 형식을 규정한다. 기본 설계는 이 전제를 그대로 따라야 한다.  
또한 1553 설계/적용 가이드는 MIL-HDBK-1553에서 1553B Notice 2 기준 적용 관점을 설명하고 있으므로, 예제 프로젝트도 가능하면 **1553B Notice 2 해석을 기준선**으로 삼는 것이 좋다.

### 이 프로젝트에서 반영할 핵심 전제
- **Bus Controller(BC)** 가 명령을 주도한다.
- **Remote Terminal(RT)** 은 BC 명령에 따라 송수신한다.
- **Bus Monitor(BM)** 은 버스를 관찰하고 기록하지만 데이터 전송 주체가 되지 않는다.
- 메시지는 **Command / Status / Data word** 기반으로 다룬다.
- 설계는 **주 버스 + 예비 버스(A/B)** 를 고려한 이중화 구조를 가진다.
- 상위 소프트웨어는 “메시지” 단위로 다루고, 하위 어댑터는 “벤더 API 호출” 단위로 숨긴다.

---

## 4. 전체 아키텍처 방향

권장 구조는 **4계층 아키텍처**다.

```text
[Operator / UI / Scenario]
        ↓
[Application Services]
        ↓
[Protocol Core / Domain]
        ↓
[Vendor Adapter / Hardware Driver]
```

### 4.1 계층 설명

#### A. Operator / UI / Scenario
- 운용자 화면
- 시나리오 선택
- 로그 조회
- RT 상태 확인
- 버스 전환 수동 트리거
- 메시지 송수신 통계 표시

#### B. Application Services
- BC 스케줄 실행 서비스
- RT 응답 서비스
- BM 기록 서비스
- 재시도 정책
- 장애 감지 및 버스 전환 정책
- 시뮬레이터와 실장비 모드 전환

#### C. Protocol Core / Domain
- CommandWord / StatusWord / DataWord 모델
- 메시지 프레임 조립/해석
- 타이밍 정책
- 모드 코드 해석
- 결과(Result) 객체
- 오류 분류 및 상태 전이

#### D. Vendor Adapter / Hardware Driver
- 상용 1553 카드 SDK 호출
- 채널 오픈/클로즈
- BC/RT/BM 초기화
- 버스 A/B 선택
- 송수신 버퍼 접근
- 인터럽트/콜백/폴링 처리

---

## 5. 언어별 역할 분담

## 5.1 C++ 역할
C++은 다음 용도로 배치한다.

- 하드웨어 SDK와 가장 가까운 **Native Adapter**
- 성능/지연 제어가 중요한 **Protocol Runtime**
- 저수준 버퍼 처리
- time-tag / raw frame 가공
- 테스트 장비 연결부
- 시뮬레이터 엔진 핵심부

### C++이 적합한 이유
- 대부분의 1553 벤더 SDK가 **C / C++ 친화적**
- 실시간성, 메모리 제어, 네이티브 드라이버 연계가 유리
- 타 언어에서 호출 가능한 **안정적인 C ABI** 노출 가능

---

## 5.2 C# 역할
C#은 다음 용도로 배치한다.

- 운용 UI(WPF, WinUI, 콘솔 대시보드)
- 시나리오 편집기
- 로그 뷰어 / 리플레이 툴
- 구성(JSON) 관리
- 테스트 자동화 오케스트레이션
- Native DLL 래핑 또는 벤더 .NET SDK 활용

### C#이 적합한 이유
- UI/운용 툴 생산성이 높음
- JSON, DI, 로깅, 설정, 테스트 자동화가 편함
- 벤더가 .NET API를 제공하면 빠르게 연동 가능
- .NET API가 없더라도 **P/Invoke로 C API 래핑** 가능

---

## 6. 권장 개발 형태

가장 현실적인 방식은 다음 둘 중 하나다.

### 방식 A. C++ Core + C# App
- C++: `Agent1553.Native`
- C#: `Agent1553.App`
- C#이 C++ DLL을 P/Invoke로 호출
- UI, 운영, 설정은 C#
- 하드웨어 제어와 프로토콜 핵심은 C++

### 방식 B. 벤더 .NET SDK 직접 사용 + C++ 보조 도구
- C#: 메인 앱
- C++: 성능 측정기, 테스트 툴, 네이티브 유틸리티
- 벤더 .NET SDK가 충분히 안정적일 때 적합

### 최종 권장
**예제 프로젝트 관점에서는 방식 A를 더 권장**한다.  
이유는 벤더 종속성을 Adapter 안에 가두기 쉽고, C# UI와 C++ Native Core의 책임 분리가 명확하기 때문이다.

---

## 7. 논리 컴포넌트 설계

### 7.1 공통 도메인 모델

```text
BusChannel
BusLine (A/B)
TerminalAddress
SubAddress
ModeCode
WordType
CommandWord
StatusWord
DataWord
MessageFrame
TransferRequest
TransferResult
TimeTag
ErrorCode
HealthState
```

### 7.2 서비스 컴포넌트

#### BusControllerService
- 스케줄 기반 송신
- 재시도
- 응답 시간 감시
- RT to RT 전송 오케스트레이션

#### RemoteTerminalService
- 명령 수신
- 서브어드레스별 데이터 맵핑
- 상태 워드 설정
- 모드 코드 처리

#### BusMonitorService
- 전체 메시지 수집
- time-tag 기록
- 필터링 저장
- replay 파일 생성

#### BusHealthService
- timeout 집계
- retry 횟수 추적
- active bus 상태 판정
- failover 조건 평가

#### ScenarioService
- 예제 시나리오 로딩
- 스케줄 적용
- test step 실행
- 결과 요약 보고서 생성

---

## 8. 런타임 상태 모델

```text
Stopped
  -> Initializing
  -> Ready
  -> Running
  -> Degraded
  -> FailoverSwitching
  -> Recovery
  -> Faulted
  -> Stopped
```

### 상태 전이 예시
- 초기화 성공 → `Ready`
- BC 시작 → `Running`
- 응답 누락 증가 → `Degraded`
- A 버스 오류 임계치 초과 → `FailoverSwitching`
- B 버스 정상화 → `Recovery` → `Running`
- 장치 오픈 실패 → `Faulted`

---

## 9. 메시지 처리 흐름

## 9.1 BC → RT 수신 명령
1. BC가 Command Word 생성
2. Vendor Adapter에 송신 요청
3. RT가 명령 해석
4. 필요 시 Data Word 수신
5. RT가 Status Word 반환
6. 결과를 TransferResult로 변환
7. BM이 동일 트래픽 기록

## 9.2 RT → BC 송신 요청
1. BC가 RT transmit command 발행
2. RT가 상태 확인
3. RT가 Status Word 전송
4. RT가 Data Word 전송
5. BC가 수신 검증
6. 결과 저장 및 UI 갱신

## 9.3 RT → RT 전송
1. BC가 RT A receive command 발행
2. 이어서 RT B transmit command 발행
3. RT B가 Status + Data 송신
4. RT A가 수신 후 Status 반환
5. 전체 트랜잭션 성공 여부 판정

---

## 10. C++ 예제 설계

## 10.1 프로젝트 구조

```text
src/cpp/
  Agent1553.Native/
    include/
      Agent1553Api.h
      BusTypes.h
      Result.h
      IBusAdapter.h
      IVendorDevice.h
    src/
      Agent1553Api.cpp
      BusControllerService.cpp
      RemoteTerminalService.cpp
      BusMonitorService.cpp
      BusHealthService.cpp
      MessageCodec.cpp
      ScenarioEngine.cpp
      VendorAdapterFactory.cpp
      SimulatedAdapter.cpp
      VendorXAdapter.cpp
    tests/
      MessageCodecTests.cpp
      ScenarioEngineTests.cpp
      BusHealthTests.cpp
```

## 10.2 핵심 인터페이스 예시

```cpp
class IBusAdapter
{
public:
    virtual ~IBusAdapter() = default;
    virtual Result Open(const ChannelConfig& config) = 0;
    virtual Result Close() = 0;
    virtual Result StartBc(const BcStartOptions& options) = 0;
    virtual Result StartRt(const RtStartOptions& options) = 0;
    virtual Result StartBm(const BmStartOptions& options) = 0;
    virtual Result Send(const TransferRequest& request) = 0;
    virtual Result Receive(TransferResult& result) = 0;
    virtual Result SelectBus(BusLine line) = 0;
};
```

## 10.3 C++ 설계 원칙
- RAII로 장치 수명 관리
- 예외보다 `Result<T>` 중심 오류 흐름
- `std::chrono` 기반 시간 처리
- 벤더 의존 타입 노출 금지
- raw buffer는 adapter 내부에만 격리
- 테스트 가능하도록 시뮬레이터 어댑터 제공

---

## 11. C# 예제 설계

## 11.1 프로젝트 구조

```text
src/csharp/
  Agent1553.App/
    Views/
    ViewModels/
    Services/
    Commands/
  Agent1553.Core/
    Models/
    Contracts/
    UseCases/
  Agent1553.Interop/
    NativeMethods/
    Agent1553NativeClient.cs
  Agent1553.Simulator/
  Agent1553.Tests/
```

## 11.2 C# 주요 책임
- 설정 파일 편집
- 채널 상태 표시
- 메시지 통계 차트
- 시나리오 시작/중지
- 로그/리플레이 뷰어
- Native 호출 래핑
- BM 기록을 CSV/JSON으로 내보내기

## 11.3 C# 서비스 예시

```csharp
public interface IScenarioRunner
{
    Task<Result> StartAsync(string scenarioName, CancellationToken cancellationToken);
    Task<Result> StopAsync(CancellationToken cancellationToken);
}

public interface IBusDashboardService
{
    Task<BusHealthSnapshot> GetHealthAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BusMessageView>> GetRecentMessagesAsync(CancellationToken cancellationToken);
}
```

## 11.4 C# 설계 원칙
- UI와 장치 제어를 직접 결합하지 않기
- Native 호출은 `Interop` 프로젝트로 한정
- `record`, `readonly`, `CancellationToken`, `IAsyncEnumerable` 등 최신 문법 적극 사용
- ViewModel은 상태 표현만 담당
- 실제 장비가 없어도 Simulator 모드로 화면 개발 가능하게 설계

---

## 12. C# ↔ C++ 연동 방식

### 권장 순서
1. **벤더 .NET SDK가 있으면 우선 사용**
2. 없거나 제약이 크면 **C API 형태의 C++ DLL** 작성
3. C#에서 **P/Invoke** 로 호출

### 예시 C ABI
```cpp
extern "C"
{
    __declspec(dllexport) int Agent1553_OpenChannel(int channelId);
    __declspec(dllexport) int Agent1553_CloseChannel(int channelId);
    __declspec(dllexport) int Agent1553_StartBc(int channelId);
    __declspec(dllexport) int Agent1553_SendCommand(
        int channelId,
        int rtAddress,
        int subAddress,
        int wordCount,
        const unsigned short* dataWords,
        int dataWordCount);
}
```

### C# P/Invoke 예시 방향
```csharp
internal static partial class NativeMethods
{
    [LibraryImport("Agent1553.Native")]
    internal static partial int Agent1553_OpenChannel(int channelId);

    [LibraryImport("Agent1553.Native")]
    internal static partial int Agent1553_StartBc(int channelId);
}
```

---

## 13. 설정 파일 예시

```json
{
  "deviceMode": "Simulator",
  "channelId": 0,
  "activeBus": "A",
  "redundancy": {
    "enabled": true,
    "retryCountBeforeSwitch": 3,
    "switchToBackupOnTimeout": true
  },
  "bcSchedules": [
    {
      "name": "PollRt1Status",
      "periodMs": 20,
      "rtAddress": 1,
      "subAddress": 2,
      "direction": "Receive"
    },
    {
      "name": "GetRt2Sensor",
      "periodMs": 100,
      "rtAddress": 2,
      "subAddress": 5,
      "direction": "Transmit"
    }
  ],
  "bm": {
    "enabled": true,
    "captureAll": true,
    "saveFile": "logs/session_001.jsonl"
  }
}
```

---

## 14. 예제 프로젝트 기능 범위 정의

## 14.1 MVP
- 단일 채널
- BC 한 개
- RT 2개 시뮬레이션
- BM 기록
- A/B 버스 수동 전환
- 메시지 로그 확인

## 14.2 확장 1
- 재시도 정책
- 자동 failover
- 모드 코드 일부 지원
- time-tag 기반 replay

## 14.3 확장 2
- 실장비 연동
- 멀티채널
- RT별 데이터 맵 편집기
- 테스트 리포트 자동 생성

## 14.4 확장 3
- HIL(Hardware-in-the-Loop)
- 성능 측정
- 장애 주입 스크립트
- 인증/검증 보조 도구

---

## 15. Agent 작업 단계

## Phase 0. 요구사항 고정
- BC/RT/BM 중 어떤 역할을 구현할지 결정
- 시뮬레이터 우선인지 실장비 우선인지 결정
- 벤더 카드 SDK 사용 여부 결정
- 채널 수, RT 수, 주기 요구사항 정리

## Phase 1. 도메인 모델링
- CommandWord / StatusWord / DataWord 정의
- 메시지 흐름 모델링
- Result / ErrorCode 정의
- BusHealth 상태 정의

## Phase 2. Adapter 인터페이스 작성
- `IBusAdapter`
- `IScenarioRepository`
- `ILogWriter`
- `ITimeProvider`

## Phase 3. Simulator 구현
- 실장비 없이도 BC/RT/BM 흐름 검증
- 단위 테스트 기반으로 메시지 왕복 구현

## Phase 4. Native/Vendor Adapter 구현
- 실제 카드 SDK 연결
- 채널 오픈/클로즈
- 송수신 이벤트 처리
- 오류 코드 표준화

## Phase 5. C# 운영 도구 구현
- 대시보드
- 시나리오 실행기
- 로그 뷰어
- 설정 편집기

## Phase 6. 통합 테스트 및 검증
- 시뮬레이터
- 실장비
- 장시간 soak test
- failover 시험
- 보고서 자동화

---

## 16. 테스트 전략

## 16.1 단위 테스트
대상:
- MessageCodec
- CommandWord 파싱/조립
- 상태 전이
- 재시도 정책
- failover 조건

## 16.2 통합 테스트
대상:
- BC ↔ RT 시뮬레이터
- BM 기록
- schedule 실행
- timeout / retry / recovery

## 16.3 실장비 테스트
대상:
- 채널 초기화
- 주기 메시지 송수신
- A/B 버스 전환
- RT 응답 시간 측정
- BM 파일 기록

## 16.4 검증 관점
- RT 구현은 **AS4111 / AS4112 관점의 검증 시나리오**를 참고
- 프로젝트 성격상 전체 인증 도구를 완성하는 것이 아니라,
  **표준 친화적인 메시지/응답/오류 처리 구조를 갖추는 것**을 목표로 한다

---

## 17. 리스크와 대응

### 리스크 1. 벤더 SDK 종속성
대응:
- Adapter 패턴 적용
- 벤더 타입 숨김
- 공통 DTO 유지

### 리스크 2. 실시간성 부족
대응:
- 스케줄러는 Native 우선
- UI는 비동기 분리
- 로깅도 비차단 큐 사용

### 리스크 3. 장치 없이는 개발 진척이 느림
대응:
- 시뮬레이터 먼저 구현
- replay 기반 테스트 제공
- BM 로그 재생 툴 준비

### 리스크 4. 버스 전환 정책이 불명확
대응:
- timeout, retry, line error 임계치를 설정 파일로 분리
- 자동/수동 failover 모두 지원

### 리스크 5. 물리 계층 이슈를 소프트웨어로만 오판
대응:
- coupler, termination, bus A/B wiring 점검 절차 문서화
- 하드웨어 로그와 소프트웨어 로그를 분리 보관

---

## 18. 완료 기준(Definition of Done)

다음 조건을 만족하면 예제 프로젝트 1차 완료로 본다.

- BC가 설정 기반으로 주기 메시지를 송신할 수 있다
- RT 시뮬레이터가 정상 응답할 수 있다
- BM이 메시지를 time-tag와 함께 기록할 수 있다
- A/B 버스 전환을 수동 또는 자동으로 수행할 수 있다
- C++ Native 계층과 C# 운영 계층이 분리되어 있다
- 장비 없음 모드(Simulator)와 장비 연동 모드(Hardware)가 모두 존재한다
- 기본 테스트(단위/통합)가 자동 실행된다

---

## 19. 최종 권장안 요약

### 가장 추천하는 구조
- **C++**
  - 1553 Native Core
  - Vendor Adapter
  - Scheduler / Runtime
  - Simulator
- **C#**
  - 운영 UI
  - 시나리오/설정 관리
  - 로그/리플레이 도구
  - 테스트 오케스트레이션

### 이 구조의 장점
- 벤더 교체 영향 최소화
- UI와 프로토콜 핵심의 책임 분리
- 시뮬레이터 우선 개발 가능
- 실제 장비 연동으로 자연스럽게 확장 가능

---

## 20. 후속 산출물 제안

이 Agent_Plan 다음 단계로 바로 만들기 좋은 산출물은 아래 4개다.

1. **AGENT.md**
   - 구현 규칙
   - 커밋 단위
   - 테스트 우선 원칙
   - 폴더별 책임

2. **Solution Skeleton**
   - C++ 프로젝트 뼈대
   - C# 프로젝트 뼈대
   - Interop 샘플

3. **시뮬레이터 우선 MVP 코드**
   - BC/RT/BM 최소 동작
   - JSON 시나리오 로더
   - 로그 출력

4. **실장비 Adapter 템플릿**
   - 벤더 SDK 연결 포인트만 남긴 래퍼

---

## 21. 참고 메모

실제 장비 환경에서는 벤더별로 API와 초기화 절차가 다르므로,  
프로젝트 초반부터 **“벤더 API 직접 노출 금지”** 원칙을 강하게 적용하는 것이 좋다.

또한 C#만으로 바로 들어가기보다,  
**“C++ Native Core → C API 노출 → C# App 연결”** 순서로 가면 구조가 더 오래 간다.
