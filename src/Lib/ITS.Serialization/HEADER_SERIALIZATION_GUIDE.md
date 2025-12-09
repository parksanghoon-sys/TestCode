# SerializeWithHeader 사용 가이드

이 문서는 `SerializeWithHeader` / `SerializeWithHeaderClass` 메서드의 올바른 사용법과 주의사항을 설명합니다.

## 📋 목차

1. [개요](#개요)
2. [메서드 설명](#메서드-설명)
3. [ITS 시스템에서의 올바른 사용법](#its-시스템에서의-올바른-사용법)
4. [잘못된 사용 예시](#잘못된-사용-예시)
5. [올바른 사용 예시](#올바른-사용-예시)
6. [수신 처리 방식](#수신-처리-방식)
7. [사용 시나리오별 가이드](#사용-시나리오별-가이드)

---

## 개요

### SerializeWithHeader 메서드란?

네트워크 프로토콜에서 **헤더 + 페이로드**를 하나의 바이트 배열로 결합하는 편의 메서드입니다.

### 제공되는 메서드

```csharp
// 1. struct 페이로드용
byte[] SerializeWithHeader<THeader, TPayload>(THeader header, TPayload payload)
    where THeader : struct
    where TPayload : struct

// 2. class 페이로드용
byte[] SerializeWithHeaderClass<THeader, TPayload>(THeader header, TPayload payload)
    where THeader : struct
    where TPayload : class

// 3. struct 역직렬화
(THeader header, TPayload payload) DeserializeWithHeader<THeader, TPayload>(byte[] data)
    where THeader : struct
    where TPayload : struct

// 4. class 역직렬화
(THeader header, TPayload payload) DeserializeWithHeaderClass<THeader, TPayload>(byte[] data)
    where THeader : struct
    where TPayload : class
```

---

## 메서드 설명

### 동작 방식

```csharp
// 직렬화
var header = new E_HOSTNETWORK_HEADER { sync = 0xE179, messageid = 1, messagesize = 49 };
var target = new Target { ID = 101, Name = "Alpha" };

byte[] packet = serializer.SerializeWithHeaderClass(header, target);
// 결과: [Header(10 bytes)][Target(49 bytes)] = 59 bytes
```

### 메모리 레이아웃

```
┌───────────────────────────────────────────────────────┐
│                   결과 바이트 배열                      │
├───────────────────────────────────────────────────────┤
│ [0x79 0xE1][0x01 0x00 0x00 0x00][0x31 0x00 0x00 0x00]│ ← 헤더 (10 bytes)
│  ↑ sync    ↑ messageid           ↑ messagesize       │
├───────────────────────────────────────────────────────┤
│ [0x01 0x65 0x00 0x00 0x00 0x0C ...]                   │ ← 페이로드 (49 bytes)
│  ↑ Target 직렬화 데이터                                │
└───────────────────────────────────────────────────────┘
```

---

## ITS 시스템에서의 올바른 사용법

### ⚠️ 중요: ITS 시스템의 송신 구조

ITS 시스템의 `NetworkDataObject.SendData()`는 **자동으로 헤더를 붙여줍니다!**

```csharp
// NetworkDataObject.cs - SendData() 내부
public unsafe void SendData(int index = -1, int DESTINATIONID = -1)
{
    // 1. GetObject()에서 페이로드만 가져옴
    byte[] bytedata = GetObject(i);

    // 2. ★ 헤더 자동 생성
    E_HOSTNETWORK_HEADER header = new E_HOSTNETWORK_HEADER();
    header.sync = 57721;  // 0xE179
    header.messageid = messagenumber;
    header.messagesize = datasize;

    // 3. ★ 헤더 + 페이로드 결합
    int headersize = sizeof(E_HOSTNETWORK_HEADER);
    byte[] arr = new byte[headersize + datasize];

    Array.Copy(bytedata, 0, arr, headersize, datasize);  // 페이로드 복사

    IntPtr headerptr = Marshal.AllocHGlobal(headersize);
    Marshal.StructureToPtr(header, headerptr, true);
    Marshal.Copy(headerptr, arr, 0, headersize);  // 헤더 복사
    Marshal.FreeHGlobal(headerptr);

    // 4. 전송
    m_networklist[n].SendData(arr);
}
```

### ⚠️ 중요: ITS 시스템의 수신 구조

수신측 `ParsingPacket()`은 **자동으로 헤더를 제거**합니다!

```csharp
// NetworkHostManager.cs - ParsingPacket() 내부
public unsafe int ParsingPacket(string remoteendpoint, byte[] Data, int Length)
{
    // 1. ★ 헤더 파싱
    if (Data[0] != 0x79 || Data[1] != 0xE1) { return Length; }
    int type = BitConverter.ToInt32(Data, 2);    // messageid
    int count = BitConverter.ToInt32(Data, 6);   // messagesize

    // 2. ★ 헤더 제거, 페이로드만 추출
    byte[] Buffer = new byte[count];
    Array.Copy(Data, sizeof(E_HOSTNETWORK_HEADER), Buffer, 0, count);

    // 3. ★ 페이로드만 Manager로 전달
    int processlength = Receive(type, Buffer, count);
}
```

### 결론

```
송신: GetObject() → [페이로드만] → SendData() → [헤더 자동 추가] → 네트워크
수신: 네트워크 → ParsingPacket() → [헤더 자동 제거] → Receive() → [페이로드만]
```

따라서 **ITS 시스템 내부에서는 `SerializeWithHeader`를 사용하면 안 됩니다!**

---

## 잘못된 사용 예시

### ❌ 예시 1: GetObject()에서 SerializeWithHeaderClass 사용 (헤더 중복)

```csharp
// FlightStatusManager.cs
public override byte[] GetObject(int index)
{
    // ❌ 잘못된 방법: 헤더를 직접 생성
    var header = new E_HOSTNETWORK_HEADER
    {
        sync = 0xE179,
        messageid = (int)E_MESSAGEID.e_messageid_flightstatus,
        messagesize = 450
    };

    // ❌ SerializeWithHeaderClass 사용
    return serializer.SerializeWithHeaderClass(header, m_datalist[index].m_data);
}

// 문제: SendData()가 또 헤더를 붙임!
// 결과: [Header][Header][Data] (헤더 중복!)
```

### 실제 메모리 상태 (헤더 중복)

```
┌─────────────────────────────────────────────────────────────┐
│ GetObject() 반환값                                           │
├─────────────────────────────────────────────────────────────┤
│ [0x79 0xE1][msgid][size][FlightStatus 450 bytes]            │ ← 헤더 포함 (460 bytes)
└─────────────────────────────────────────────────────────────┘
                    ↓ SendData() 호출
┌─────────────────────────────────────────────────────────────┐
│ SendData() 최종 패킷                                          │
├─────────────────────────────────────────────────────────────┤
│ [0x79 0xE1][msgid][size] ← SendData()가 추가한 헤더          │
│ [0x79 0xE1][msgid][size][FlightStatus 450 bytes]            │ ← GetObject() 반환값
└─────────────────────────────────────────────────────────────┘
  ↑ 헤더가 2번 들어감! (470 bytes)

수신측에서 파싱 실패!
```

### ❌ 예시 2: Receive()에서 DeserializeWithHeaderClass 사용

```csharp
// FlightStatusManager.cs
public unsafe override int Receive(byte[] Data, int Length)
{
    // ❌ 잘못된 방법: DeserializeWithHeaderClass 사용
    var (header, flightStatus) = serializer.DeserializeWithHeaderClass<E_HOSTNETWORK_HEADER, SFlightStatus>(Data);

    // 문제: Data는 이미 페이로드만 포함 (ParsingPacket이 헤더 제거함)
    // 예외 발생: "Data too small. Expected at least 10 bytes..."
}
```

---

## 올바른 사용 예시

### ✅ 예시 1: ITS 시스템 내부 - 페이로드만 직렬화

```csharp
// FlightStatusManager.cs
public class FlightStatusManager : NetworkDataObject
{
    private BinarySerializer _serializer = new BinarySerializer();

    // ✅ 올바른 방법: 페이로드만 직렬화
    public override byte[] GetObject(int index)
    {
        // SendData()가 헤더를 자동으로 붙여주므로 페이로드만 반환
        return _serializer.Serialize(m_datalist[index].m_data);
    }

    // ✅ 올바른 방법: 페이로드만 역직렬화
    public unsafe override int Receive(byte[] Data, int Length)
    {
        // ParsingPacket()이 헤더를 제거했으므로 페이로드만 역직렬화
        var flightStatus = _serializer.Deserialize<SFlightStatus>(Data);

        // 데이터 처리
        ProcessReceiveData(flightStatus);

        return Length;
    }
}
```

### ✅ 예시 2: 외부 시스템 직접 연동 - 헤더 포함 전송

```csharp
// 외부 TCP 소켓으로 직접 전송하는 경우
public class ExternalSystemClient
{
    private BinarySerializer _serializer = new BinarySerializer();
    private TcpClient _client;

    public void SendToExternalSystem(Target target)
    {
        // ✅ 외부 시스템은 SendData() 없이 직접 전송하므로 헤더 포함 필요
        var header = new E_HOSTNETWORK_HEADER
        {
            sync = 0xE179,
            messageid = 999,  // 외부 시스템 메시지 ID
            messagesize = 0   // 아래에서 자동 계산됨
        };

        // Serialize로 페이로드 크기 먼저 계산
        byte[] payload = _serializer.Serialize(target);
        header.messagesize = payload.Length;

        // ✅ 헤더 + 페이로드 결합
        byte[] packet = _serializer.SerializeWithHeaderClass(header, target);

        // 직접 전송
        NetworkStream stream = _client.GetStream();
        stream.Write(packet, 0, packet.Length);
    }

    public Target ReceiveFromExternalSystem()
    {
        NetworkStream stream = _client.GetStream();
        byte[] buffer = new byte[2048];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        // ✅ 외부 시스템에서 받은 패킷은 헤더 포함
        var (header, target) = _serializer.DeserializeWithHeaderClass<E_HOSTNETWORK_HEADER, Target>(buffer);

        // 헤더 검증
        if (header.sync != 0xE179)
            throw new Exception("Invalid packet");

        return target;
    }
}
```

### ✅ 예시 3: 파일 저장/로드

```csharp
public class FlightDataRecorder
{
    private BinarySerializer _serializer = new BinarySerializer();

    public void SaveToFile(SFlightStatus flightStatus, string filePath)
    {
        // ✅ 파일 형식: [헤더][데이터]로 저장
        var header = new E_HOSTNETWORK_HEADER
        {
            sync = 0xE179,
            messageid = (int)E_MESSAGEID.e_messageid_flightstatus,
            messagesize = 450  // SFlightStatus 고정 크기
        };

        byte[] packet = _serializer.SerializeWithHeader(header, flightStatus);
        File.WriteAllBytes(filePath, packet);
    }

    public SFlightStatus LoadFromFile(string filePath)
    {
        byte[] packet = File.ReadAllBytes(filePath);

        // ✅ 헤더 + 데이터 분리 역직렬화
        var (header, flightStatus) = _serializer.DeserializeWithHeader<E_HOSTNETWORK_HEADER, SFlightStatus>(packet);

        // 헤더 검증
        if (header.sync != 0xE179)
            throw new Exception("Invalid file format");

        return flightStatus;
    }
}
```

---

## 수신 처리 방식

### ITS 시스템의 수신 흐름

```
┌────────────────────────────────────────────────────────────┐
│ 1. 네트워크 수신                                             │
├────────────────────────────────────────────────────────────┤
│ TCP/IP: [0x79 0xE1][msgid=89][size=450][FlightStatus...]   │
│         ↑ 헤더 (10)              ↑ 페이로드 (450)          │
└────────────────────────────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────────┐
│ 2. ParsingPacket() - 헤더 파싱 및 제거                       │
├────────────────────────────────────────────────────────────┤
│ sync 확인: 0x79 0xE1 ✓                                      │
│ type 추출: BitConverter.ToInt32(data, 2) = 89               │
│ size 추출: BitConverter.ToInt32(data, 6) = 450              │
│                                                             │
│ Buffer 생성: new byte[450]                                  │
│ Array.Copy(data, 10, Buffer, 0, 450)  ← 헤더 제거!          │
└────────────────────────────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────────┐
│ 3. Receive(type=89, Buffer, 450)                            │
├────────────────────────────────────────────────────────────┤
│ switch (type)                                               │
│ {                                                           │
│     case 89:  // FlightStatus                               │
│         return m_flightstatusmanager.Receive(Buffer, 450);  │
│ }                                                           │
└────────────────────────────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────────┐
│ 4. FlightStatusManager.Receive(Buffer, 450)                 │
├────────────────────────────────────────────────────────────┤
│ // ✅ Buffer는 순수 페이로드만 포함 (헤더 없음!)              │
│ var flightStatus = serializer.Deserialize<SFlightStatus>(  │
│     Buffer  ← [FlightStatus 450 bytes만]                    │
│ );                                                          │
└────────────────────────────────────────────────────────────┘
```

### 핵심 정리

1. **ParsingPacket()**: 헤더를 파싱하고 **제거**
2. **Receive()**: **페이로드만** 받음
3. **따라서**: `DeserializeWithHeader`를 사용하면 **에러 발생**

---

## 사용 시나리오별 가이드

### 시나리오 1: ITS 내부 Manager → Manager 통신

```
✅ 사용: Serialize() / Deserialize()
❌ 사용 금지: SerializeWithHeader / DeserializeWithHeader

이유: SendData()와 ParsingPacket()이 헤더를 자동 처리
```

**예시:**
```csharp
// 송신
public override byte[] GetObject(int index)
{
    return serializer.Serialize(m_datalist[index].m_data);  // ✅
}

// 수신
public override int Receive(byte[] Data, int Length)
{
    var obj = serializer.Deserialize<SFlightStatus>(Data);  // ✅
    return Length;
}
```

---

### 시나리오 2: 외부 TCP 소켓 직접 사용

```
✅ 사용: SerializeWithHeader / DeserializeWithHeader
❌ 사용 금지: Serialize() 단독 (헤더 누락)

이유: SendData() 없이 직접 전송하므로 헤더 필요
```

**예시:**
```csharp
// 송신 (헤더 포함)
var header = new E_HOSTNETWORK_HEADER { ... };
byte[] packet = serializer.SerializeWithHeaderClass(header, target);  // ✅
tcpClient.GetStream().Write(packet, 0, packet.Length);

// 수신 (헤더 포함)
byte[] buffer = new byte[2048];
int len = tcpClient.GetStream().Read(buffer, 0, buffer.Length);
var (header, target) = serializer.DeserializeWithHeaderClass<E_HOSTNETWORK_HEADER, Target>(buffer);  // ✅
```

---

### 시나리오 3: 파일 저장/로드

```
✅ 사용: SerializeWithHeader / DeserializeWithHeader
또는
✅ 사용: Serialize() / Deserialize() (헤더 없이 데이터만)

파일 형식에 따라 선택
```

**예시 A: 헤더 포함 형식 (ICD 호환)**
```csharp
// 저장
var header = new E_HOSTNETWORK_HEADER { sync = 0xE179, ... };
byte[] packet = serializer.SerializeWithHeader(header, flightStatus);  // ✅
File.WriteAllBytes("flight.bin", packet);

// 로드
byte[] data = File.ReadAllBytes("flight.bin");
var (header, flightStatus) = serializer.DeserializeWithHeader<E_HOSTNETWORK_HEADER, SFlightStatus>(data);  // ✅
```

**예시 B: 헤더 없는 형식 (데이터만)**
```csharp
// 저장
byte[] data = serializer.Serialize(flightStatus);  // ✅
File.WriteAllBytes("flight_data.bin", data);

// 로드
byte[] data = File.ReadAllBytes("flight_data.bin");
var flightStatus = serializer.Deserialize<SFlightStatus>(data);  // ✅
```

---

### 시나리오 4: 테스트 코드

```
✅ 사용: SerializeWithHeader / DeserializeWithHeader

이유: 전체 패킷 구조 검증
```

**예시:**
```csharp
[Test]
public void TestHeaderSerialization()
{
    var header = new E_HOSTNETWORK_HEADER { sync = 0xE179, messageid = 1, messagesize = 49 };
    var target = new Target { ID = 101, Name = "Alpha" };

    // ✅ 헤더 + 페이로드 직렬화
    byte[] packet = serializer.SerializeWithHeaderClass(header, target);

    // 검증
    Assert.AreEqual(59, packet.Length);  // 10 + 49
    Assert.AreEqual(0x79, packet[0]);
    Assert.AreEqual(0xE1, packet[1]);

    // ✅ 역직렬화
    var (receivedHeader, receivedTarget) = serializer.DeserializeWithHeaderClass<E_HOSTNETWORK_HEADER, Target>(packet);

    Assert.AreEqual(0xE179, receivedHeader.sync);
    Assert.AreEqual(101, receivedTarget.ID);
}
```

---

## 요약

### ✅ 올바른 사용

| 상황 | 송신 | 수신 | 이유 |
|-----|------|------|------|
| **ITS 내부 Manager** | `Serialize()` | `Deserialize()` | SendData/ParsingPacket이 헤더 처리 |
| **외부 TCP 직접** | `SerializeWithHeaderClass()` | `DeserializeWithHeaderClass()` | 헤더 자동 처리 없음 |
| **파일 (ICD 형식)** | `SerializeWithHeader()` | `DeserializeWithHeader()` | 표준 패킷 형식 유지 |
| **파일 (데이터만)** | `Serialize()` | `Deserialize()` | 헤더 불필요 |
| **테스트** | `SerializeWithHeader()` | `DeserializeWithHeader()` | 전체 구조 검증 |

### ❌ 잘못된 사용

| 상황 | 문제 | 결과 |
|-----|------|------|
| **GetObject()에서 SerializeWithHeader** | 헤더 중복 | 송신 실패 (헤더 2개) |
| **Receive()에서 DeserializeWithHeader** | 헤더 누락 | 수신 실패 (예외 발생) |
| **외부 TCP에서 Serialize만** | 헤더 누락 | 파싱 실패 (sync 없음) |

### 핵심 원칙

```
ITS 내부: GetObject()와 Receive()는 순수 페이로드만 다룸
외부 시스템: 헤더를 직접 관리해야 하므로 SerializeWithHeader 사용
```
