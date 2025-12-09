# NetworkHostLibrary 프로토콜 구조 비교

## 개요

NetworkHostLibrary에는 **3가지 다른 프로토콜 구조**가 공존하고 있습니다:

1. **Flight 프로토콜** - Binary Struct 방식 (StructLayout)
2. **GMTIList/MovingTarget 프로토콜** - Binary Struct 방식 (StructLayout)
3. **MCRCData 프로토콜** - Text 기반 Reflection 방식

---

## 1. Flight 프로토콜 (Binary Struct 방식)

### 특징
- **StructLayout 사용**: C 스타일 고정 크기 구조체
- **PropertyChanged 이벤트**: 실시간 속성 변경 감지
- **직접 멤버 할당**: 리플렉션 없이 직접 struct 멤버에 값 복사
- **메시지 ID**: `e_messageid_flightstatus = 89`

### 프로토콜 구조

```
[E_HOSTNETWORK_HEADER (10 bytes)]
├─ sync: 0xE179 (2 bytes)
├─ messageid: 89 (4 bytes)  ← Flight Status
└─ messagesize: N (4 bytes)

[Payload: SFlightStatus × N]
├─ SFlightStatus[0]
│   ├─ m_FlightID: int (4 bytes)
│   ├─ m_Latitude: double (8 bytes)
│   ├─ m_Longitude: double (8 bytes)
│   ├─ m_Altitude: double (8 bytes)
│   ├─ ... (고정 크기 필드들)
│   └─ 총: 약 100~200 bytes (고정)
├─ SFlightStatus[1]
└─ ...
```

### 코드 예시 (FlightStatusManager.cs)

```csharp
// 1. 구조체 정의 (고정 크기)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SFlightStatus
{
    public int m_FlightID;
    public double m_Latitude;
    public double m_Longitude;
    public double m_Altitude;
    // ... 모든 필드가 고정 크기
}

// 2. PropertyChanged 이벤트로 변경 감지
public override void DataPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    Flight flight = sender as Flight;
    if (flight != null)
    {
        // 직접 할당 (리플렉션 없음)
        m_datalist[i].m_data.m_Latitude = flight.Position.Latitude;
        m_datalist[i].m_data.m_Longitude = flight.Position.Longitude;
        m_datalist[i].m_data.m_Altitude = flight.Position.Altitude;

        SetSendUpdate();
        SendData();  // 즉시 전송
    }
}

// 3. 바이너리 직렬화 (Marshal 사용)
public override byte[] GetObject(int index)
{
    return ByteProcess.EncodeByteArray(m_datalist[index]);
}
```

### 장점
- ✅ **빠른 성능**: 고정 크기, 직접 메모리 복사
- ✅ **작은 패킷 크기**: 바이너리 형식
- ✅ **실시간 전송**: PropertyChanged 즉시 반영
- ✅ **낮은 CPU 사용률**: 리플렉션 없음

### 단점
- ❌ **유연성 부족**: 구조체 변경 시 재컴파일 필요
- ❌ **디버깅 어려움**: 바이너리라 육안 확인 불가
- ❌ **확장성 제한**: 새 필드 추가 시 전체 시스템 재배포

---

## 2. GMTIList/MovingTarget 프로토콜 (Binary Struct 방식)

### 특징
- **Flight와 동일한 방식** 사용
- **메시지 ID**: `e_messageid_movingtargetstatus = 43`
- **동적 배열**: `CMovingTargetStatus[]` 사용

### 프로토콜 구조

```
[E_HOSTNETWORK_HEADER (10 bytes)]
├─ sync: 0xE179 (2 bytes)
├─ messageid: 43 (4 bytes)  ← Moving Target Status
└─ messagesize: N (4 bytes)

[Payload: SMovingTargetStatus × N]
├─ SMovingTargetStatus[0]
│   ├─ m_TargetID: int (4 bytes)
│   ├─ m_Latitude: double (8 bytes)
│   ├─ m_Longitude: double (8 bytes)
│   ├─ m_Altitude: double (8 bytes)
│   ├─ m_Heading: double (8 bytes)
│   ├─ m_Speed: double (8 bytes)
│   ├─ m_MoveTargetIndex: int (4 bytes)
│   └─ 총: 52 bytes (고정)
├─ SMovingTargetStatus[1]
└─ ...
```

### 코드 예시 (MovingTargetStatusManager.cs)

```csharp
// 1. 구조체 정의
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SMovingTargetStatus
{
    public int m_TargetID;
    public double m_Latitude;
    public double m_Longitude;
    public double m_Altitude;
    public double m_Heading;
    public double m_Speed;
    public int m_MoveTargetIndex;
}

// 2. PropertyChanged 이벤트 처리 (Flight와 동일)
public override void DataPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    Target target = sender as Target;
    if (target != null)
    {
        m_datalist[i].m_data.m_TargetID = target.ID;
        m_datalist[i].m_data.m_Latitude = target.Position.Latitude;
        m_datalist[i].m_data.m_Longitude = target.Position.Longitude;
        // ...
        SetSendUpdate();
    }
}
```

### Flight와의 차이점
- **동일점**: 바이너리 구조체 직렬화, PropertyChanged, 직접 할당
- **차이점**:
  - Target 객체 대신 Flight 객체
  - 메시지 ID와 필드 구성만 다름
  - **본질적으로 같은 프로토콜 방식**

---

## 3. MCRCData 프로토콜 (Text 기반 Reflection 방식)

### 특징
- **텍스트 기반**: 문자열 파싱 ("Property(Command,Index)")
- **Reflection 사용**: 런타임 속성 조회 및 수정
- **델타 업데이트**: 변경된 속성만 전송
- **메시지 ID**: `e_messageid_mcrcdata = 340`

### 프로토콜 구조

```
[E_HOSTNETWORK_HEADER (10 bytes)]
├─ sync: 0xE179 (2 bytes)
├─ messageid: 340 (4 bytes)  ← MCRC Data
└─ messagesize: N (4 bytes)

[Payload: Text-based Commands]
"TargetList(Remove,2)\n
ExtAircraftList[0].Altitude=15000\n
FlightList<3>.Position.Latitude=37.5\n
..."

또는

[Payload: SMCRCControl Struct - 예외적으로 Binary]
├─ m_ACFTNO: byte[] (가변)
├─ m_ACFTFNFDVCD: byte[] (가변)
├─ m_Latitude: double (8 bytes)
├─ m_Longitude: double (8 bytes)
├─ ...
```

### 코드 예시 (MCRCControlManager.cs)

**주의**: MCRCControlManager는 실제로는 **Binary Struct 방식**을 사용하고 있습니다!

```csharp
// 1. 구조체 정의 (하지만 byte[] 사용으로 가변 길이)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct SMCRCControl
{
    public byte[] m_ACFTNO;       // 가변 길이!
    public byte[] m_ACFTFNFDVCD;  // 가변 길이!
    public double m_Latitude;
    public double m_Longitude;
    // ...
}

// 2. PropertyChanged 이벤트 처리
public override void DataPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    Aircraft aircraft = sender as Aircraft;
    if (aircraft != null && aircraft.UpdateTrigger)
    {
        // UTF-8 인코딩 (가변 길이 필드)
        m_data.m_ACFTNO = System.Text.Encoding.UTF8.GetBytes(aircraft.ACFTNO);
        m_data.m_ACFTFNFDVCD = System.Text.Encoding.UTF8.GetBytes(aircraft.ACFTFNFDVCD);
        m_data.m_Latitude = aircraft.Position.Latitude;
        // ...

        SetSendUpdate();
        SendData(0);
        aircraft.UpdateTrigger = false;
    }
}
```

### 텍스트 프로토콜 (이론상 MCRCData의 원래 의도)

실제 ITS 시스템의 다른 부분에서 사용되는 델타 프로토콜:

```csharp
// NetworkHostManager.cs의 DecodeCommand 함수 참고
// 정규식 파싱:
// "FlightList[0].Altitude" → Property: FlightList, Index: 0
// "TargetList(Add,5)" → Property: TargetList, Command: Add, Index: 5
// "GMTIList(Remove,2)" → Property: GMTIList, Command: Remove, Index: 2

string stringformat2 = @"([0-9A-Za-z_`]+)([\[]*)([0-9]*)([\]]*)([\(]*)([0-9A-Za-z_`]+)(,)([0-9]*)([\)]*)";
Match match = Regex.Match(matches[i].Groups[1].ToString(), stringformat2);

// 파싱 후 리플렉션으로 처리
PropertyInfo pi = sender.GetType().GetProperty(e.PropertyName);
var value = pi.GetValue(sender, null);
```

### 장점
- ✅ **유연성**: 구조체 변경 없이 새 속성 추가 가능
- ✅ **디버깅 용이**: 텍스트라 육안 확인 가능
- ✅ **델타 업데이트**: 변경된 부분만 전송 가능
- ✅ **확장성**: 런타임 동적 처리

### 단점
- ❌ **느린 성능**: 문자열 파싱 + 리플렉션
- ❌ **큰 패킷 크기**: 텍스트 형식 오버헤드
- ❌ **복잡한 구현**: 정규식 파싱 로직 필요
- ❌ **높은 CPU 사용률**: 리플렉션 오버헤드

---

## 프로토콜 비교 표

| 특성 | Flight/GMTI (Binary) | MCRCData (Mixed) |
|------|---------------------|------------------|
| **직렬화 방식** | StructLayout + Marshal | StructLayout (but byte[]) |
| **데이터 포맷** | Binary (고정 크기) | Binary (가변 길이) |
| **메시지 크기** | 작음 (52~200 bytes) | 중간 (가변) |
| **전송 속도** | 매우 빠름 | 빠름 |
| **CPU 사용률** | 낮음 | 낮음 |
| **메모리 사용** | 고정 | 가변 (GC 부담) |
| **디버깅** | 어려움 (바이너리) | 어려움 (바이너리) |
| **확장성** | 낮음 (재컴파일) | 중간 (byte[] 크기만) |
| **유연성** | 낮음 | 중간 |
| **실시간성** | 우수 (즉시 전송) | 우수 (트리거 방식) |
| **사용 케이스** | 실시간 위치/상태 | 외부 시스템 연동 |

---

## 왜 다른 프로토콜을 사용하나?

### 1. Flight/GMTI → Binary Struct 프로토콜
**이유**: 실시간 시뮬레이션 요구사항
- 초당 수십~수백 개의 객체 업데이트
- 낮은 지연시간 필수 (밀리초 단위)
- 고정된 데이터 구조 (비행 상태는 변하지 않음)
- **성능이 최우선**

### 2. MCRCData → Mixed 프로토콜
**이유**: 외부 시스템 연동
- IME (외부 시스템)과의 통신
- 업데이트 빈도가 낮음 (UpdateTrigger 사용)
- 가변 길이 문자열 필드 필요 (ACFTNO, CLSGN 등)
- **호환성이 우선**

### 3. 기존 Text 기반 델타 프로토콜 (NetworkHostManager.DecodeCommand)
**이유**: 유연한 객체 관리
- FlightList, GMTIList 등의 동적 추가/제거
- 런타임 속성 변경
- **개발 편의성이 우선**

---

## ITS.Serialization 라이브러리 적용 시 예상 프로토콜

새로운 바이너리 직렬화 라이브러리를 적용하면:

```
[E_HOSTNETWORK_HEADER (10 bytes)]
├─ sync: 0xE179 (2 bytes)
├─ messageid: 340 (4 bytes)  ← MCRC Data (새 프로토콜)
└─ messagesize: N (4 bytes)

[Payload: NetworkMessage]
├─ Type: MessageType (1 byte)
│   ├─ DeltaUpdate (0)
│   ├─ FullSync (1)
│   └─ Batch (2)
│
├─ Delta: DeltaCommand (Type == DeltaUpdate 일 때)
│   ├─ Path: string (가변)
│   │   예: "ExtAircraftList[0].Altitude"
│   ├─ Command: CommandType (4 bytes)
│   │   ├─ Update (0)
│   │   ├─ Add (1)
│   │   ├─ Remove (2)
│   │   ├─ Set (3)
│   │   └─ Reset (4)
│   ├─ Index: int (4 bytes)
│   ├─ ID: int (4 bytes)
│   └─ Payload: byte[] (가변)
│       └─ BinarySerializer로 직렬화된 실제 값
│
├─ FullState: byte[] (Type == FullSync 일 때)
│   └─ MCRCData 전체 객체 직렬화
│
└─ Batch: DeltaBatch (Type == Batch 일 때)
    └─ Commands: DeltaCommand[] (가변)
```

### 예시 패킷

**1. 델타 업데이트 (Altitude 변경)**
```
Header: [0x79 0xE1] [0x54 0x01 0x00 0x00] [0x3A 0x00 0x00 0x00]
        ↑sync       ↑messageid=340        ↑size=58

Payload:
  Type: 0x00 (DeltaUpdate)
  Delta:
    Path: "ExtAircraftList[0].Altitude" (30 bytes)
    Command: 0x00 0x00 0x00 0x00 (Update)
    Index: 0xFF 0xFF 0xFF 0xFF (-1, 사용 안 함)
    ID: 0xFF 0xFF 0xFF 0xFF (-1, 사용 안 함)
    Payload: [0x00 0x00 0x00 0x00 0x00 0x50 0xC3 0x40] (double: 15000.0, 8 bytes)
```

**2. 리스트 제거 (Target 제거)**
```
Header: [0x79 0xE1] [0x54 0x01 0x00 0x00] [0x1C 0x00 0x00 0x00]
        ↑sync       ↑messageid=340        ↑size=28

Payload:
  Type: 0x00 (DeltaUpdate)
  Delta:
    Path: "TargetList" (11 bytes)
    Command: 0x02 0x00 0x00 0x00 (Remove)
    Index: 0x02 0x00 0x00 0x00 (2)
    ID: 0xFF 0xFF 0xFF 0xFF (-1)
    Payload: null (0 bytes)
```

**3. 전체 동기화**
```
Header: [0x79 0xE1] [0x54 0x01 0x00 0x00] [0xF4 0x03 0x00 0x00]
        ↑sync       ↑messageid=340        ↑size=1012

Payload:
  Type: 0x01 (FullSync)
  FullState: [... MCRCData 전체 객체 바이너리 ...] (1008 bytes)
```

---

## 마이그레이션 전략

### Phase 1: MCRCData만 새 프로토콜 적용
- 기존 Flight/GMTI는 그대로 유지 (성능 검증됨)
- MCRCData만 ITS.Serialization으로 교체
- 델타 업데이트로 대역폭 50~70% 절감 예상

### Phase 2: 성능 측정 및 비교
- 기존 Binary Struct vs 새 Binary Serializer
- 지연시간, CPU, 메모리 비교
- 병목 지점 식별

### Phase 3: 필요시 Flight/GMTI도 점진적 전환
- 성능 저하 없으면 통합
- 단일 프로토콜로 시스템 단순화

---

## 결론

**NetworkHostLibrary는 2가지 프로토콜 패밀리를 사용**:

1. **Binary Struct 프로토콜** (Flight, GMTI, MCRC)
   - StructLayout + PropertyChanged
   - 고성능, 실시간 요구사항
   - 고정 크기 구조체 (일부 가변)

2. **Text Reflection 프로토콜** (NetworkHostManager.DecodeCommand)
   - 정규식 파싱 + Reflection
   - 유연한 객체 관리
   - 개발 편의성

**새 ITS.Serialization 프로토콜**은:
- Binary 성능 + Text 유연성 결합
- 델타 업데이트로 대역폭 절감
- 타입 안정성 + 확장성
- **점진적 마이그레이션 가능**

MCRCData는 현재 Binary Struct를 사용하지만, `byte[]` 필드 때문에 가변 길이 특성을 가지고 있어, 새 프로토콜로 전환 시 가장 큰 이득을 볼 수 있는 후보입니다.
