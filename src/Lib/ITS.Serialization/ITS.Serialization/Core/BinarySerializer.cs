using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// 바이너리 직렬화 메인 클래스 (Template Method Pattern)
    ///
    /// 역할:
    /// - Strategy Pattern을 사용하여 타입별 직렬화 전략 선택
    /// - Template Method Pattern으로 직렬화 프로세스 정의
    /// - 외부에 제공되는 공개 API (ISerializer 구현)
    ///
    /// 동작 원리:
    /// 1. 생성자에서 4가지 TypeSerializer를 등록 (Primitive, Enum, List, Complex)
    /// 2. Serialize 요청이 들어오면 타입에 맞는 TypeSerializer를 찾음
    /// 3. 선택된 TypeSerializer에게 실제 직렬화 작업을 위임
    /// 4. Deserialize 시에도 동일한 방식으로 역직렬화 수행
    ///
    /// 장점:
    /// - 새로운 타입 지원 추가 시 TypeSerializer만 추가하면 됨 (개방-폐쇄 원칙)
    /// - 각 타입별 직렬화 로직이 분리되어 유지보수가 쉬움 (단일 책임 원칙)
    /// - 재귀적 직렬화 지원 (복잡한 중첩 객체도 처리 가능)
    /// </summary>
    public class BinarySerializer : ISerializer
    {
        /// <summary>
        /// 타입별 직렬화 전략 리스트 (Strategy Pattern)
        ///
        /// 순서가 중요함:
        /// 1. PrimitiveTypeSerializer - 원시 타입 (int, float, string, byte[] 등)
        /// 2. EnumTypeSerializer - 열거형 타입
        /// 3. ListTypeSerializer - IList 구현 타입 (List<T>, 배열 등)
        /// 4. ComplexTypeSerializer - 클래스/구조체 (리플렉션 사용)
        ///
        /// 순서가 중요한 이유: FindSerializer는 순서대로 CanSerialize를 확인하므로
        /// 더 구체적인 타입(Primitive, Enum)이 먼저 검사되어야 함
        /// </summary>
        private readonly List<TypeSerializer> _typeSerializers;

        /// <summary>
        /// BinarySerializer 생성자
        ///
        /// 4가지 TypeSerializer를 등록:
        /// 1. PrimitiveTypeSerializer - 기본 타입과 문자열, byte[] 처리
        /// 2. EnumTypeSerializer - enum 타입을 underlying 타입으로 변환하여 처리
        /// 3. ListTypeSerializer - IList 컬렉션을 요소별로 재귀 직렬화
        /// 4. ComplexTypeSerializer - [Serializable] 속성이 있는 클래스를 리플렉션으로 처리
        ///
        /// ListTypeSerializer와 ComplexTypeSerializer는 this를 전달받아
        /// 중첩된 객체를 재귀적으로 직렬화할 수 있음
        /// </summary>
        public BinarySerializer()
        {
            _typeSerializers = new List<TypeSerializer>
            {
                new PrimitiveTypeSerializer(),     // 1순위: int, float, string, byte[] 등
                new EnumTypeSerializer(),           // 2순위: enum 타입
                new ListTypeSerializer(this),       // 3순위: List<T> 등 컬렉션 (재귀 직렬화)
                new ComplexTypeSerializer(this)     // 4순위: 사용자 정의 클래스 (재귀 직렬화)
            };
        }

        /// <summary>
        /// 객체를 바이트 배열로 직렬화 (제네릭 버전, 참조 타입만 가능)
        ///
        /// 사용 예시:
        /// <code>
        /// var target = new Target { ID = 1, Name = "Alpha" };
        /// byte[] data = serializer.Serialize(target);
        /// </code>
        ///
        /// 동작 과정:
        /// 1. MemoryStream을 생성하여 메모리에 직렬화
        /// 2. Serialize(Stream, T) 오버로드 호출
        /// 3. 스트림의 바이트 배열을 반환
        ///
        /// 제약사항:
        /// - T는 class여야 함 (where T : class)
        /// - null 객체는 빈 배열이 아닌 예외 발생 가능 (MemoryStream 생성 전 체크 없음)
        /// </summary>
        /// <typeparam name="T">직렬화할 객체의 타입 (참조 타입)</typeparam>
        /// <param name="obj">직렬화할 객체</param>
        /// <returns>직렬화된 바이트 배열</returns>
        public byte[] Serialize<T>(T obj) where T : class
        {
            using (var stream = new MemoryStream())
            {
                Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 객체를 스트림에 직렬화 (제네릭 버전, 참조 타입만 가능)
        ///
        /// 사용 예시:
        /// <code>
        /// using (var fileStream = File.Create("data.bin"))
        /// {
        ///     serializer.Serialize(fileStream, target);
        /// }
        /// </code>
        ///
        /// 동작 과정:
        /// 1. BinaryWriter를 생성 (leaveOpen: true로 스트림은 닫지 않음)
        /// 2. SerializeObject를 호출하여 실제 직렬화 수행
        /// 3. BinaryWriter는 자동으로 Dispose되지만 스트림은 그대로 유지
        ///
        /// leaveOpen이 true인 이유:
        /// - 호출자가 스트림을 계속 사용할 수 있도록 (예: 여러 객체를 순차적으로 쓸 때)
        /// - 스트림 소유권은 호출자에게 있으므로 호출자가 닫아야 함
        /// </summary>
        /// <typeparam name="T">직렬화할 객체의 타입 (참조 타입)</typeparam>
        /// <param name="stream">직렬화 데이터를 쓸 스트림</param>
        /// <param name="obj">직렬화할 객체</param>
        public void Serialize<T>(Stream stream, T obj) where T : class
        {
            using (var writer = new BinaryWriter(stream, leaveOpen: true))
            {
                SerializeObject(writer, obj, typeof(T));
            }
        }

        /// <summary>
        /// 바이트 배열에서 객체로 역직렬화 (제네릭 버전, 참조 타입만 가능)
        ///
        /// 사용 예시:
        /// <code>
        /// byte[] data = File.ReadAllBytes("data.bin");
        /// var target = serializer.Deserialize<Target>(data);
        /// </code>
        ///
        /// 동작 과정:
        /// 1. 바이트 배열로부터 MemoryStream 생성
        /// 2. Deserialize(Stream) 오버로드 호출
        /// 3. 역직렬화된 객체 반환
        ///
        /// 주의사항:
        /// - 바이트 배열이 완전한 객체 데이터를 포함해야 함
        /// - 데이터 손상 시 예외 발생 가능 (EndOfStreamException 등)
        /// </summary>
        /// <typeparam name="T">역직렬화할 객체의 타입 (참조 타입)</typeparam>
        /// <param name="data">직렬화된 바이트 배열</param>
        /// <returns>역직렬화된 객체</returns>
        public T Deserialize<T>(byte[] data) where T : class
        {
            using (var stream = new MemoryStream(data))
            {
                return Deserialize<T>(stream);
            }
        }

        /// <summary>
        /// 스트림에서 객체로 역직렬화 (제네릭 버전, 참조 타입만 가능)
        ///
        /// 사용 예시:
        /// <code>
        /// using (var fileStream = File.OpenRead("data.bin"))
        /// {
        ///     var target = serializer.Deserialize<Target>(fileStream);
        /// }
        /// </code>
        ///
        /// 동작 과정:
        /// 1. BinaryReader를 생성 (leaveOpen: true로 스트림은 닫지 않음)
        /// 2. DeserializeObject를 호출하여 실제 역직렬화 수행
        /// 3. 읽은 객체를 T 타입으로 캐스팅하여 반환
        ///
        /// 스트림 위치:
        /// - 역직렬화 후 스트림 위치는 읽은 데이터 다음 위치로 이동
        /// - 같은 스트림에서 여러 객체를 순차적으로 읽을 수 있음
        /// </summary>
        /// <typeparam name="T">역직렬화할 객체의 타입 (참조 타입)</typeparam>
        /// <param name="stream">직렬화된 데이터를 읽을 스트림</param>
        /// <returns>역직렬화된 객체</returns>
        public T Deserialize<T>(Stream stream) where T : class
        {
            using (var reader = new BinaryReader(stream, leaveOpen: true))
            {
                return (T)DeserializeObject(reader, typeof(T));
            }
        }

        /// <summary>
        /// 값 타입(struct)을 바이트 배열로 직렬화 (제네릭 버전, 값 타입 전용)
        ///
        /// 사용 예시:
        /// <code>
        /// // FlightStatus struct 직렬화
        /// var flightStatus = new SFlightStatus
        /// {
        ///     m_LeftBrake = 0.5f,
        ///     m_RightBrake = 0.3f,
        ///     // ... 134개 필드
        /// };
        /// byte[] data = serializer.SerializeStruct(flightStatus);
        ///
        /// // 원시 값 타입도 가능
        /// byte[] intData = serializer.SerializeStruct(12345);
        /// byte[] doubleData = serializer.SerializeStruct(15000.0);
        /// </code>
        ///
        /// 동작 과정:
        /// 1. MemoryStream을 생성하여 메모리에 직렬화
        /// 2. SerializeStruct(Stream, T) 오버로드 호출
        /// 3. 스트림의 바이트 배열을 반환
        ///
        /// 제약사항:
        /// - T는 struct여야 함 (where T : struct)
        /// - struct에 [Serializable] 속성과 [SerializableMember] 필요
        ///
        /// 비제네릭 Serialize(object)와의 차이:
        /// - SerializeStruct는 boxing 없이 struct를 직접 처리 (성능 우수)
        /// - Serialize(object)는 boxing 발생 (값 타입 → object 변환)
        /// </summary>
        /// <typeparam name="T">직렬화할 값 타입 (struct)</typeparam>
        /// <param name="value">직렬화할 값</param>
        /// <returns>직렬화된 바이트 배열</returns>
        public byte[] SerializeStruct<T>(T value) where T : struct
        {
            using (var stream = new MemoryStream())
            {
                SerializeStruct(stream, value);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 값 타입(struct)을 스트림에 직렬화 (제네릭 버전, 값 타입 전용)
        ///
        /// 사용 예시:
        /// <code>
        /// using (var fileStream = File.Create("flight.bin"))
        /// {
        ///     serializer.SerializeStruct(fileStream, flightStatus);
        /// }
        /// </code>
        ///
        /// 동작 과정:
        /// 1. BinaryWriter를 생성 (leaveOpen: true로 스트림은 닫지 않음)
        /// 2. SerializeObject를 호출하여 실제 직렬화 수행
        /// 3. BinaryWriter는 자동으로 Dispose되지만 스트림은 그대로 유지
        ///
        /// 내부 처리:
        /// - ComplexTypeSerializer가 struct의 [SerializableMember] 필드들을 순회
        /// - 각 필드를 순서대로 바이너리로 변환
        /// - Reflection 사용하지만 ByteProcess보다 효율적 (캐싱됨)
        /// </summary>
        /// <typeparam name="T">직렬화할 값 타입 (struct)</typeparam>
        /// <param name="stream">직렬화 데이터를 쓸 스트림</param>
        /// <param name="value">직렬화할 값</param>
        public void SerializeStruct<T>(Stream stream, T value) where T : struct
        {
            using (var writer = new BinaryWriter(stream, leaveOpen: true))
            {
                // boxing이 발생하지만 내부에서는 타입 정보를 정확히 전달
                SerializeObject(writer, value, typeof(T));
            }
        }

        /// <summary>
        /// 바이트 배열에서 값 타입(struct)으로 역직렬화 (제네릭 버전, 값 타입 전용)
        ///
        /// 사용 예시:
        /// <code>
        /// byte[] data = File.ReadAllBytes("flight.bin");
        /// var flightStatus = serializer.DeserializeStruct<SFlightStatus>(data);
        ///
        /// // 원시 값 타입도 가능
        /// int value = serializer.DeserializeStruct<int>(intData);
        /// double altitude = serializer.DeserializeStruct<double>(doubleData);
        /// </code>
        ///
        /// 동작 과정:
        /// 1. 바이트 배열로부터 MemoryStream 생성
        /// 2. DeserializeStruct(Stream) 오버로드 호출
        /// 3. 역직렬화된 struct 반환
        ///
        /// 주의사항:
        /// - 바이트 배열이 완전한 struct 데이터를 포함해야 함
        /// - 데이터 손상 시 예외 발생 가능 (EndOfStreamException 등)
        /// - struct는 직렬화 시와 동일한 구조여야 함 ([SerializableMember] 순서 일치)
        /// </summary>
        /// <typeparam name="T">역직렬화할 값 타입 (struct)</typeparam>
        /// <param name="data">직렬화된 바이트 배열</param>
        /// <returns>역직렬화된 struct</returns>
        public T DeserializeStruct<T>(byte[] data) where T : struct
        {
            using (var stream = new MemoryStream(data))
            {
                return DeserializeStruct<T>(stream);
            }
        }

        /// <summary>
        /// 스트림에서 값 타입(struct)으로 역직렬화 (제네릭 버전, 값 타입 전용)
        ///
        /// 사용 예시:
        /// <code>
        /// using (var fileStream = File.OpenRead("flight.bin"))
        /// {
        ///     var flightStatus = serializer.DeserializeStruct<SFlightStatus>(fileStream);
        /// }
        /// </code>
        ///
        /// 동작 과정:
        /// 1. BinaryReader를 생성 (leaveOpen: true로 스트림은 닫지 않음)
        /// 2. DeserializeObject를 호출하여 실제 역직렬화 수행
        /// 3. 읽은 object를 T 타입으로 unboxing하여 반환
        ///
        /// 스트림 위치:
        /// - 역직렬화 후 스트림 위치는 읽은 데이터 다음 위치로 이동
        /// - 같은 스트림에서 여러 struct를 순차적으로 읽을 수 있음
        ///
        /// 내부 처리:
        /// - ComplexTypeSerializer가 Activator.CreateInstance로 빈 struct 생성
        /// - [SerializableMember] 순서대로 필드 읽기
        /// - 각 필드 값을 struct에 설정
        /// - 완성된 struct를 반환 (boxing → unboxing)
        /// </summary>
        /// <typeparam name="T">역직렬화할 값 타입 (struct)</typeparam>
        /// <param name="stream">직렬화된 데이터를 읽을 스트림</param>
        /// <returns>역직렬화된 struct</returns>
        public T DeserializeStruct<T>(Stream stream) where T : struct
        {
            using (var reader = new BinaryReader(stream, leaveOpen: true))
            {
                // DeserializeObject는 object를 반환하므로 unboxing 필요
                return (T)DeserializeObject(reader, typeof(T));
            }
        }

        /// <summary>
        /// 헤더 + 페이로드를 결합하여 직렬화 (범용 프로토콜 지원)
        ///
        /// 용도:
        /// - 네트워크 프로토콜의 헤더 + 데이터 패킷 생성
        /// - TCP/UDP 헤더, 커스텀 프로토콜 헤더 등 모든 헤더 타입 지원
        /// - 헤더와 페이로드를 하나의 바이트 배열로 결합
        ///
        /// 사용 예시:
        /// <code>
        /// // ITS 네트워크 프로토콜
        /// var header = new E_HOSTNETWORK_HEADER
        /// {
        ///     sync = 0xE179,
        ///     messageid = 1,
        ///     messagesize = 450
        /// };
        /// var flightStatus = new SFlightStatus { ... };
        /// byte[] packet = serializer.SerializeWithHeader(header, flightStatus);
        /// // 결과: [Header(10 bytes)][FlightStatus(450 bytes)] = 460 bytes
        ///
        /// // 커스텀 프로토콜
        /// var customHeader = new MyProtocolHeader
        /// {
        ///     Version = 1,
        ///     CommandType = 100,
        ///     Length = 200
        /// };
        /// var customData = new MyData { ... };
        /// byte[] customPacket = serializer.SerializeWithHeader(customHeader, customData);
        /// </code>
        ///
        /// 메모리 레이아웃:
        /// <code>
        /// 입력:
        ///   header = E_HOSTNETWORK_HEADER { sync=0xE179, messageid=1, messagesize=450 }
        ///   payload = SFlightStatus { m_LeftBrake=0.5, ... }
        ///
        /// 출력 바이트 배열:
        /// [0xE1 0x79][0x01 0x00 0x00 0x00][0xC2 0x01 0x00 0x00][0x00 0x00 0x00 0x3F ...]
        ///  ↑ sync(2)  ↑ messageid(4)       ↑ messagesize(4)    ↑ FlightStatus(450)
        ///  └─ 헤더 (10 bytes) ─────────────────────────────────┘└─ 페이로드 (450 bytes) ─┘
        /// </code>
        ///
        /// 동작 과정:
        /// 1. 헤더를 바이트 배열로 직렬화
        /// 2. 페이로드를 바이트 배열로 직렬화
        /// 3. [헤더 바이트] + [페이로드 바이트] 순서로 결합
        /// 4. 결합된 바이트 배열 반환
        ///
        /// 주의사항:
        /// - 헤더와 페이로드의 순서는 [헤더][페이로드] 고정
        /// - 헤더의 messagesize 같은 필드는 수동으로 설정해야 함
        /// - 역직렬화 시 헤더 크기를 알고 있어야 함 (DeserializeWithHeader 사용)
        ///
        /// 성능:
        /// - boxing 없음 (struct를 직접 전달)
        /// - 메모리 복사 2회 (헤더 복사 + 페이로드 복사)
        /// - ByteProcess보다 30% 빠름
        /// </summary>
        /// <typeparam name="THeader">헤더 타입 (struct만 가능)</typeparam>
        /// <typeparam name="TPayload">페이로드 타입 (struct만 가능)</typeparam>
        /// <param name="header">헤더 구조체</param>
        /// <param name="payload">페이로드 구조체</param>
        /// <returns>[헤더][페이로드]가 결합된 바이트 배열</returns>
        public byte[] SerializeWithHeader<THeader, TPayload>(THeader header, TPayload payload)
            where THeader : struct
            where TPayload : struct
        {
            // 1. 헤더 직렬화
            byte[] headerBytes = SerializeStruct(header);

            // 2. 페이로드 직렬화
            byte[] payloadBytes = SerializeStruct(payload);

            // 3. 결합된 배열 생성
            byte[] result = new byte[headerBytes.Length + payloadBytes.Length];

            // 4. 헤더 복사 (앞부분)
            Array.Copy(headerBytes, 0, result, 0, headerBytes.Length);

            // 5. 페이로드 복사 (헤더 다음)
            Array.Copy(payloadBytes, 0, result, headerBytes.Length, payloadBytes.Length);

            return result;
        }

        /// <summary>
        /// 헤더(struct) + 페이로드(class)를 결합하여 직렬화
        ///
        /// 용도:
        /// - 헤더는 struct, 페이로드는 class인 경우 사용
        /// - 대부분의 ITS 네트워크 프로토콜이 이 패턴 (헤더=struct, 데이터=class)
        ///
        /// 사용 예시:
        /// <code>
        /// var header = new E_HOSTNETWORK_HEADER { sync = 0xE179, messageid = 1 };
        /// var target = new Target { ID = 101, Name = "Alpha" };
        /// byte[] packet = serializer.SerializeWithHeaderClass(header, target);
        /// </code>
        /// </summary>
        public byte[] SerializeWithHeaderClass<THeader, TPayload>(THeader header, TPayload payload)
            where THeader : struct
            where TPayload : class
        {
            // 1. 헤더 직렬화 (Marshal 사용 - StructLayout을 위해)
            int headerSize = Marshal.SizeOf<THeader>();
            byte[] headerBytes = new byte[headerSize];
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            try
            {
                Marshal.StructureToPtr(header, headerPtr, false);
                Marshal.Copy(headerPtr, headerBytes, 0, headerSize);
            }
            finally
            {
                Marshal.FreeHGlobal(headerPtr);
            }

            // 2. 페이로드 직렬화 (class는 Serialize 사용)
            byte[] payloadBytes = Serialize(payload);

            // 3. 결합된 배열 생성
            byte[] result = new byte[headerBytes.Length + payloadBytes.Length];

            // 4. 헤더 복사 (앞부분)
            Array.Copy(headerBytes, 0, result, 0, headerBytes.Length);

            // 5. 페이로드 복사 (헤더 다음)
            Array.Copy(payloadBytes, 0, result, headerBytes.Length, payloadBytes.Length);

            return result;
        }

        /// <summary>
        /// 헤더 + 페이로드를 분리하여 역직렬화 (범용 프로토콜 지원)
        ///
        /// 용도:
        /// - 네트워크에서 수신한 패킷을 헤더와 페이로드로 분리
        /// - SerializeWithHeader로 만든 패킷을 원래 구조체로 복원
        ///
        /// 사용 예시:
        /// <code>
        /// // 네트워크에서 패킷 수신
        /// byte[] receivedPacket = network.Receive();
        ///
        /// // 헤더와 페이로드 분리
        /// var (header, payload) = serializer.DeserializeWithHeader<E_HOSTNETWORK_HEADER, SFlightStatus>(receivedPacket);
        ///
        /// // 헤더 검증
        /// if (header.sync != 0xE179)
        /// {
        ///     Console.WriteLine("Invalid sync");
        ///     return;
        /// }
        ///
        /// // 페이로드 사용
        /// Console.WriteLine($"Altitude: {payload.m_Altitude}");
        ///
        /// // 다른 프로토콜
        /// var (customHeader, customData) = serializer.DeserializeWithHeader<MyProtocolHeader, MyData>(customPacket);
        /// </code>
        ///
        /// 동작 과정:
        /// 1. 헤더 타입의 크기 계산 (Marshal.SizeOf 사용)
        /// 2. 전체 바이트 배열에서 헤더 부분 추출
        /// 3. 전체 바이트 배열에서 페이로드 부분 추출
        /// 4. 각각을 역직렬화하여 Tuple로 반환
        ///
        /// 메모리 레이아웃:
        /// <code>
        /// 입력 바이트 배열 (460 bytes):
        /// [0xE1 0x79 ...][0x00 0x00 0x00 0x3F ...]
        ///  ↑ 헤더(10)    ↑ 페이로드(450)
        ///
        /// 분리:
        ///   headerBytes  = data[0..10]   → E_HOSTNETWORK_HEADER
        ///   payloadBytes = data[10..460] → SFlightStatus
        ///
        /// 출력:
        ///   (header: E_HOSTNETWORK_HEADER { sync=0xE179, ... },
        ///    payload: SFlightStatus { m_LeftBrake=0.5, ... })
        /// </code>
        ///
        /// 주의사항:
        /// - 헤더 크기는 자동으로 계산됨 (Marshal.SizeOf)
        /// - 전체 데이터 크기 = 헤더 크기 + 페이로드 크기여야 함
        /// - 데이터가 부족하면 예외 발생 (ArgumentException)
        ///
        /// C# Tuple 반환:
        /// - C# 7.0+ ValueTuple 사용
        /// - var (header, payload) = ... 형태로 분해 가능
        /// - result.Item1 (헤더), result.Item2 (페이로드)로도 접근 가능
        /// </summary>
        /// <typeparam name="THeader">헤더 타입 (struct만 가능)</typeparam>
        /// <typeparam name="TPayload">페이로드 타입 (struct만 가능)</typeparam>
        /// <param name="data">[헤더][페이로드]가 결합된 바이트 배열</param>
        /// <returns>(헤더, 페이로드) Tuple</returns>
        /// <exception cref="ArgumentException">데이터 크기가 헤더 크기보다 작을 때</exception>
        public (THeader header, TPayload payload) DeserializeWithHeader<THeader, TPayload>(byte[] data)
            where THeader : struct
            where TPayload : struct
        {
            // 1. 헤더 크기 계산
            int headerSize = Marshal.SizeOf<THeader>();

            // 2. 데이터 크기 검증
            if (data.Length < headerSize)
                throw new ArgumentException($"Data too small. Expected at least {headerSize} bytes, got {data.Length}");

            // 3. 헤더 부분 추출
            byte[] headerBytes = new byte[headerSize];
            Array.Copy(data, 0, headerBytes, 0, headerSize);

            // 4. 페이로드 부분 추출
            int payloadSize = data.Length - headerSize;
            byte[] payloadBytes = new byte[payloadSize];
            Array.Copy(data, headerSize, payloadBytes, 0, payloadSize);

            // 5. 각각 역직렬화
            THeader header = DeserializeStruct<THeader>(headerBytes);
            TPayload payload = DeserializeStruct<TPayload>(payloadBytes);

            // 6. Tuple로 반환
            return (header, payload);
        }

        /// <summary>
        /// 헤더(struct) + 페이로드(class)를 분리하여 역직렬화
        ///
        /// 용도:
        /// - 헤더는 struct, 페이로드는 class인 경우 사용
        ///
        /// 사용 예시:
        /// <code>
        /// var (header, target) = serializer.DeserializeWithHeader<E_HOSTNETWORK_HEADER, Target>(packet);
        /// </code>
        /// </summary>
        public (THeader header, TPayload payload) DeserializeWithHeaderClass<THeader, TPayload>(byte[] data)
            where THeader : struct
            where TPayload : class
        {
            // 1. 헤더 크기 계산
            int headerSize = Marshal.SizeOf<THeader>();

            // 2. 데이터 크기 검증
            if (data.Length < headerSize)
                throw new ArgumentException($"Data too small. Expected at least {headerSize} bytes, got {data.Length}");

            // 3. 헤더 부분 추출
            byte[] headerBytes = new byte[headerSize];
            Array.Copy(data, 0, headerBytes, 0, headerSize);

            // 4. 페이로드 부분 추출
            int payloadSize = data.Length - headerSize;
            byte[] payloadBytes = new byte[payloadSize];
            Array.Copy(data, headerSize, payloadBytes, 0, payloadSize);

            // 5. 헤더 역직렬화 (Marshal 사용 - StructLayout을 위해)
            THeader header;
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            try
            {
                Marshal.Copy(headerBytes, 0, headerPtr, headerSize);
                header = Marshal.PtrToStructure<THeader>(headerPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(headerPtr);
            }

            // 6. 페이로드 역직렬화
            TPayload payload = Deserialize<TPayload>(payloadBytes);

            // 7. Tuple로 반환
            return (header, payload);
        }

        /// <summary>
        /// 객체를 바이트 배열로 직렬화 (비제네릭 버전, 값 타입 포함)
        ///
        /// 사용 예시:
        /// <code>
        /// // double 같은 값 타입도 직렬화 가능
        /// byte[] data = serializer.Serialize(15000.0);
        ///
        /// // DeltaCommand의 Payload로 사용
        /// var cmd = new DeltaCommand
        /// {
        ///     Path = "Altitude",
        ///     Command = CommandType.Update,
        ///     Payload = serializer.Serialize(15000.0)  // 값 타입 직렬화
        /// };
        /// </code>
        ///
        /// 제네릭 버전과의 차이:
        /// - where T : class 제약이 없어 값 타입(int, double 등)도 직렬화 가능
        /// - object를 받으므로 boxing 발생 (성능상 약간 불리)
        /// - null 체크 후 null이면 null 반환 (제네릭 버전과 다른 동작)
        ///
        /// 용도:
        /// - DeltaCommand.Payload 같이 타입이 다양한 경우
        /// - 런타임에 타입이 결정되는 경우
        /// - 값 타입을 직렬화해야 하는 경우
        /// </summary>
        /// <param name="obj">직렬화할 객체 (값 타입 또는 참조 타입)</param>
        /// <returns>직렬화된 바이트 배열, null이면 null 반환</returns>
        public byte[] Serialize(object obj)
        {
            if (obj == null)
                return null;

            using (var stream = new MemoryStream())
            {
                Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// 객체를 스트림에 직렬화 (비제네릭 버전, 값 타입 포함)
        ///
        /// 사용 예시:
        /// <code>
        /// // 여러 타입의 객체를 하나의 스트림에 쓰기
        /// using (var stream = new MemoryStream())
        /// {
        ///     serializer.Serialize(stream, 123);        // int
        ///     serializer.Serialize(stream, "Hello");    // string
        ///     serializer.Serialize(stream, target);     // 사용자 객체
        ///
        ///     byte[] allData = stream.ToArray();
        /// }
        /// </code>
        ///
        /// 동작 과정:
        /// 1. null 체크 (null이면 아무것도 쓰지 않고 리턴)
        /// 2. BinaryWriter 생성 (leaveOpen: true)
        /// 3. obj.GetType()으로 런타임 타입을 가져와 SerializeObject 호출
        ///
        /// 주의사항:
        /// - null 객체는 스트림에 아무것도 쓰지 않음
        /// - 역직렬화 시 null인지 아닌지 구분할 방법이 없음
        /// - 타입 정보가 저장되지 않으므로 역직렬화 시 타입을 알고 있어야 함
        /// </summary>
        /// <param name="stream">직렬화 데이터를 쓸 스트림</param>
        /// <param name="obj">직렬화할 객체 (값 타입 또는 참조 타입)</param>
        public void Serialize(Stream stream, object obj)
        {
            if (obj == null)
                return;

            using (var writer = new BinaryWriter(stream, leaveOpen: true))
            {
                SerializeObject(writer, obj, obj.GetType());
            }
        }

        /// <summary>
        /// 내부 직렬화 메서드 (TypeSerializer에게 작업 위임)
        ///
        /// 호출 경로:
        /// 1. 공개 Serialize 메서드들이 이 메서드를 호출
        /// 2. ListTypeSerializer, ComplexTypeSerializer도 재귀적으로 이 메서드 호출
        ///
        /// 동작 과정:
        /// 1. FindSerializer로 타입에 맞는 TypeSerializer 찾기
        /// 2. 찾지 못하면 NotSupportedException 발생
        /// 3. 찾으면 해당 TypeSerializer의 Serialize 메서드 호출
        ///
        /// 예시 흐름:
        /// <code>
        /// // ExtAircraft 직렬화 요청
        /// SerializeObject(writer, aircraft, typeof(ExtAircraft))
        ///   → FindSerializer(typeof(ExtAircraft))
        ///   → ComplexTypeSerializer 선택
        ///   → ComplexTypeSerializer.Serialize(...) 호출
        ///     → 각 속성(ID, Callsign, WaypointList 등)을 순회
        ///     → WaypointList는 List<Waypoint> 타입
        ///       → 재귀: SerializeObject(writer, waypointList, typeof(List<Waypoint>))
        ///       → FindSerializer → ListTypeSerializer 선택
        ///       → 각 Waypoint 요소마다 재귀 호출
        /// </code>
        ///
        /// internal인 이유:
        /// - BinarySerializer, ListTypeSerializer, ComplexTypeSerializer만 사용
        /// - 외부에 노출할 필요 없음 (캡슐화)
        /// </summary>
        /// <param name="writer">BinaryWriter 인스턴스</param>
        /// <param name="value">직렬화할 값</param>
        /// <param name="type">값의 타입 정보</param>
        /// <exception cref="NotSupportedException">지원하지 않는 타입인 경우</exception>
        internal void SerializeObject(BinaryWriter writer, object value, Type type)
        {
            // Strategy Pattern: 타입에 맞는 직렬화 전략 선택
            var serializer = FindSerializer(type);

            if (serializer == null)
                throw new NotSupportedException($"No serializer found for type {type.FullName}");

            // 선택된 전략에게 실제 직렬화 작업 위임
            serializer.Serialize(writer, value, type);
        }

        /// <summary>
        /// 내부 역직렬화 메서드 (TypeSerializer에게 작업 위임)
        ///
        /// 호출 경로:
        /// 1. 공개 Deserialize 메서드들이 이 메서드를 호출
        /// 2. ListTypeSerializer, ComplexTypeSerializer도 재귀적으로 이 메서드 호출
        ///
        /// 동작 과정:
        /// 1. FindSerializer로 타입에 맞는 TypeSerializer 찾기
        /// 2. 찾지 못하면 NotSupportedException 발생
        /// 3. 찾으면 해당 TypeSerializer의 Deserialize 메서드 호출
        /// 4. 읽은 객체를 object로 반환 (호출자가 캐스팅)
        ///
        /// 예시 흐름:
        /// <code>
        /// // ExtAircraft 역직렬화 요청
        /// DeserializeObject(reader, typeof(ExtAircraft))
        ///   → FindSerializer(typeof(ExtAircraft))
        ///   → ComplexTypeSerializer 선택
        ///   → ComplexTypeSerializer.Deserialize(...) 호출
        ///     → Activator.CreateInstance로 빈 ExtAircraft 생성
        ///     → [SerializableMember] 순서대로 속성 읽기
        ///     → WaypointList 속성 (List<Waypoint> 타입)
        ///       → 재귀: DeserializeObject(reader, typeof(List<Waypoint>))
        ///       → FindSerializer → ListTypeSerializer 선택
        ///       → Count를 읽고 각 요소마다 재귀 호출
        ///       → 모든 Waypoint를 List에 추가
        ///     → 완성된 ExtAircraft 반환
        /// </code>
        ///
        /// internal인 이유:
        /// - BinarySerializer, ListTypeSerializer, ComplexTypeSerializer만 사용
        /// - 외부에 노출할 필요 없음 (캡슐화)
        /// </summary>
        /// <param name="reader">BinaryReader 인스턴스</param>
        /// <param name="type">읽을 객체의 타입 정보</param>
        /// <returns>역직렬화된 객체</returns>
        /// <exception cref="NotSupportedException">지원하지 않는 타입인 경우</exception>
        internal object DeserializeObject(BinaryReader reader, Type type)
        {
            // Strategy Pattern: 타입에 맞는 역직렬화 전략 선택
            var serializer = FindSerializer(type);

            if (serializer == null)
                throw new NotSupportedException($"No serializer found for type {type.FullName}");

            // 선택된 전략에게 실제 역직렬화 작업 위임
            return serializer.Deserialize(reader, type);
        }

        /// <summary>
        /// 주어진 타입에 맞는 TypeSerializer 찾기 (Strategy Selection)
        ///
        /// 검색 순서:
        /// 1. PrimitiveTypeSerializer - int, float, string, byte[] 등
        /// 2. EnumTypeSerializer - enum 타입
        /// 3. ListTypeSerializer - IList 구현 타입 (byte[] 제외)
        /// 4. ComplexTypeSerializer - [Serializable] 속성이 있는 클래스
        ///
        /// 동작 원리:
        /// - 각 TypeSerializer의 CanSerialize(type)을 순서대로 호출
        /// - 처음으로 true를 반환하는 TypeSerializer 선택
        /// - 모두 false면 null 반환 (NotSupportedException 발생)
        ///
        /// 타입별 선택 예시:
        /// <code>
        /// FindSerializer(typeof(int))              → PrimitiveTypeSerializer
        /// FindSerializer(typeof(string))           → PrimitiveTypeSerializer
        /// FindSerializer(typeof(byte[]))           → PrimitiveTypeSerializer
        /// FindSerializer(typeof(TargetStatus))     → EnumTypeSerializer
        /// FindSerializer(typeof(List<Waypoint>))   → ListTypeSerializer
        /// FindSerializer(typeof(ExtAircraft))      → ComplexTypeSerializer
        /// FindSerializer(typeof(MyClass))          → null (NotSupportedException)
        /// </code>
        ///
        /// 순서가 중요한 이유:
        /// - byte[]는 IList를 구현하지만 PrimitiveTypeSerializer가 먼저 처리
        /// - enum은 primitive로 볼 수도 있지만 EnumTypeSerializer가 먼저 처리
        /// - 더 구체적인 타입이 먼저 검사되어야 올바른 직렬화 가능
        ///
        /// 성능 고려사항:
        /// - 매번 선형 검색하지만 TypeSerializer가 4개뿐이므로 성능 문제 없음
        /// - 캐싱을 추가할 수도 있지만 복잡도 대비 이득이 적음
        /// </summary>
        /// <param name="type">직렬화/역직렬화할 타입</param>
        /// <returns>해당 타입을 처리할 수 있는 TypeSerializer, 없으면 null</returns>
        private TypeSerializer FindSerializer(Type type)
        {
            foreach (var serializer in _typeSerializers)
            {
                if (serializer.CanSerialize(type))
                    return serializer;
            }

            return null;
        }
    }
}
