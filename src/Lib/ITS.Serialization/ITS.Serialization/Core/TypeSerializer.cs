using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// 타입별 직렬화 전략 추상 클래스 (Strategy Pattern)
    ///
    /// 역할:
    /// - 각 타입별로 특화된 직렬화 로직을 구현하기 위한 추상 베이스 클래스
    /// - BinarySerializer가 타입에 맞는 TypeSerializer를 선택하여 사용
    ///
    /// 구현 클래스:
    /// 1. PrimitiveTypeSerializer - 원시 타입 (int, float, string, byte[] 등)
    /// 2. EnumTypeSerializer - 열거형 타입
    /// 3. ListTypeSerializer - IList 컬렉션 타입
    /// 4. ComplexTypeSerializer - 사용자 정의 클래스/구조체
    ///
    /// 사용 패턴:
    /// <code>
    /// TypeSerializer serializer = FindSerializer(type);
    /// if (serializer.CanSerialize(type))
    /// {
    ///     serializer.Serialize(writer, value, type);
    /// }
    /// </code>
    /// </summary>
    public abstract class TypeSerializer
    {
        /// <summary>
        /// 해당 TypeSerializer가 주어진 타입을 직렬화할 수 있는지 확인
        ///
        /// 각 구현 클래스가 자신이 처리할 수 있는 타입을 판별:
        /// - PrimitiveTypeSerializer: type.IsPrimitive || type == typeof(string) || ...
        /// - EnumTypeSerializer: type.IsEnum
        /// - ListTypeSerializer: typeof(IList).IsAssignableFrom(type)
        /// - ComplexTypeSerializer: type.IsClass || type.IsValueType
        /// </summary>
        /// <param name="type">확인할 타입</param>
        /// <returns>처리 가능하면 true, 아니면 false</returns>
        public abstract bool CanSerialize(Type type);

        /// <summary>
        /// 객체를 바이너리로 직렬화
        ///
        /// 각 구현 클래스가 자신의 방식으로 직렬화:
        /// - PrimitiveTypeSerializer: BinaryWriter의 WriteXXX 메서드 직접 호출
        /// - EnumTypeSerializer: underlying 타입으로 변환 후 직렬화
        /// - ListTypeSerializer: Count 쓰고 각 요소를 재귀 직렬화
        /// - ComplexTypeSerializer: [SerializableMember] 순서대로 재귀 직렬화
        /// </summary>
        /// <param name="writer">BinaryWriter 인스턴스</param>
        /// <param name="value">직렬화할 값</param>
        /// <param name="type">값의 타입 정보</param>
        public abstract void Serialize(BinaryWriter writer, object value, Type type);

        /// <summary>
        /// 바이너리에서 객체로 역직렬화
        ///
        /// 각 구현 클래스가 자신의 방식으로 역직렬화:
        /// - PrimitiveTypeSerializer: BinaryReader의 ReadXXX 메서드 직접 호출
        /// - EnumTypeSerializer: underlying 타입 읽고 Enum.ToObject로 변환
        /// - ListTypeSerializer: Count 읽고 각 요소를 재귀 역직렬화
        /// - ComplexTypeSerializer: Activator로 인스턴스 생성 후 멤버별로 재귀 역직렬화
        /// </summary>
        /// <param name="reader">BinaryReader 인스턴스</param>
        /// <param name="type">읽을 객체의 타입 정보</param>
        /// <returns>역직렬화된 객체</returns>
        public abstract object Deserialize(BinaryReader reader, Type type);
    }

    /// <summary>
    /// Primitive 타입 직렬화기 (기본 데이터 타입 처리)
    ///
    /// 지원 타입:
    /// - IsPrimitive: byte, short, int, long, float, double, bool, char 등
    /// - string: UTF-8 인코딩된 문자열 (길이 prefix 포함)
    /// - byte[]: 바이트 배열 (길이 prefix 포함)
    /// - decimal: 128비트 고정소수점 (4개의 int32로 저장)
    ///
    /// 직렬화 형식:
    /// - 숫자: Little-Endian 바이트 순서로 저장
    /// - string: [length: int32][bytes: UTF-8]
    /// - byte[]: [length: int32][bytes]
    /// - bool: 1 byte (0 or 1)
    /// - decimal: 4개의 int32 (Decimal.GetBits 사용)
    ///
    /// 특징:
    /// - 가장 빠른 직렬화 (리플렉션 없이 직접 쓰기/읽기)
    /// - 고정 크기 타입은 항상 동일한 바이트 수 사용
    /// - 가변 크기 타입(string, byte[])은 길이 정보 포함
    /// </summary>
    public class PrimitiveTypeSerializer : TypeSerializer
    {
        /// <summary>
        /// 주어진 타입이 원시 타입인지 확인
        ///
        /// 확인 조건:
        /// 1. type.IsPrimitive - CLR 원시 타입 (byte, int, float, bool 등)
        /// 2. type == typeof(string) - 문자열
        /// 3. type == typeof(decimal) - 고정소수점 숫자
        /// 4. type == typeof(byte[]) - 바이트 배열 (특수 처리)
        ///
        /// byte[] 특수 처리 이유:
        /// - byte[]는 IList를 구현하지만 ListTypeSerializer로 처리하면 비효율적
        /// - BinaryWriter.WriteBytes를 사용하여 한 번에 쓰는 것이 훨씬 빠름
        /// - 따라서 PrimitiveTypeSerializer가 먼저 처리하도록 함
        /// </summary>
        /// <param name="type">확인할 타입</param>
        /// <returns>원시 타입이면 true</returns>
        public override bool CanSerialize(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(byte[]);
        }

        /// <summary>
        /// 원시 타입 값을 바이너리로 직렬화
        ///
        /// 타입별 직렬화 방식:
        /// - byte: 1 byte
        /// - short: 2 bytes (Little-Endian)
        /// - int: 4 bytes (Little-Endian)
        /// - long: 8 bytes (Little-Endian)
        /// - float: 4 bytes (IEEE 754 single precision)
        /// - double: 8 bytes (IEEE 754 double precision)
        /// - bool: 1 byte (0 = false, 1 = true)
        /// - string: [length: 4 bytes][UTF-8 bytes] (null이면 length = -1)
        /// - byte[]: [length: 4 bytes][bytes] (null이면 length = -1)
        /// - decimal: 16 bytes (4개의 int32)
        ///
        /// 예시:
        /// <code>
        /// // int 123 직렬화
        /// writer.WriteInt32(123)  → [7B 00 00 00]
        ///
        /// // string "AB" 직렬화
        /// writer.WriteString("AB")  → [02 00 00 00 41 42]
        ///                              ^^^^^^^^^^^^^ ^^^^
        ///                              length=2     "AB"
        ///
        /// // byte[] {1, 2, 3} 직렬화
        /// writer.WriteBytes([1,2,3])  → [03 00 00 00 01 02 03]
        /// </code>
        /// </summary>
        /// <param name="writer">BinaryWriter 인스턴스</param>
        /// <param name="value">직렬화할 원시 타입 값</param>
        /// <param name="type">값의 타입</param>
        /// <exception cref="NotSupportedException">지원하지 않는 원시 타입인 경우</exception>
        public override void Serialize(BinaryWriter writer, object value, Type type)
        {
            if (type == typeof(byte))
                writer.WriteByte((byte)value);
            else if (type == typeof(short))
                writer.WriteInt16((short)value);
            else if (type == typeof(int))
                writer.WriteInt32((int)value);
            else if (type == typeof(long))
                writer.WriteInt64((long)value);
            else if (type == typeof(float))
                writer.WriteFloat((float)value);
            else if (type == typeof(double))
                writer.WriteDouble((double)value);
            else if (type == typeof(bool))
                writer.WriteBool((bool)value);
            else if (type == typeof(string))
                writer.WriteString((string)value);
            else if (type == typeof(byte[]))
                writer.WriteBytes((byte[])value);
            else if (type == typeof(decimal))
            {
                var bits = decimal.GetBits((decimal)value);
                foreach (var bit in bits)
                    writer.WriteInt32(bit);
            }
            else
                throw new NotSupportedException($"Type {type.Name} is not supported");
        }

        /// <summary>
        /// 바이너리에서 원시 타입 값으로 역직렬화
        ///
        /// Serialize의 역순으로 읽기:
        /// - 각 타입별로 정확히 Serialize에서 쓴 바이트 수만큼 읽음
        /// - 바이트 순서는 Little-Endian (BitConverter 사용)
        /// - string/byte[]는 길이를 먼저 읽고, 그만큼 데이터 읽음
        /// - decimal은 4개의 int32를 읽어 new decimal(bits)로 복원
        ///
        /// 주의사항:
        /// - Serialize와 Deserialize의 순서가 정확히 일치해야 함
        /// - 잘못된 데이터 읽기 시 EndOfStreamException 발생 가능
        /// - null string/byte[]는 length = -1로 표현됨
        /// </summary>
        /// <param name="reader">BinaryReader 인스턴스</param>
        /// <param name="type">읽을 값의 타입</param>
        /// <returns>역직렬화된 원시 타입 값</returns>
        /// <exception cref="NotSupportedException">지원하지 않는 원시 타입인 경우</exception>
        public override object Deserialize(BinaryReader reader, Type type)
        {
            if (type == typeof(byte))
                return reader.ReadByte();
            else if (type == typeof(short))
                return reader.ReadInt16();
            else if (type == typeof(int))
                return reader.ReadInt32();
            else if (type == typeof(long))
                return reader.ReadInt64();
            else if (type == typeof(float))
                return reader.ReadFloat();
            else if (type == typeof(double))
                return reader.ReadDouble();
            else if (type == typeof(bool))
                return reader.ReadBool();
            else if (type == typeof(string))
                return reader.ReadString();
            else if (type == typeof(byte[]))
                return reader.ReadBytes();
            else if (type == typeof(decimal))
            {
                var bits = new int[4];
                for (int i = 0; i < 4; i++)
                    bits[i] = reader.ReadInt32();
                return new decimal(bits);
            }
            else
                throw new NotSupportedException($"Type {type.Name} is not supported");
        }
    }

    /// <summary>
    /// Enum 타입 직렬화기 (열거형 처리)
    ///
    /// 동작 원리:
    /// - enum은 내부적으로 정수형(int, byte, short, long)으로 저장됨
    /// - Enum.GetUnderlyingType으로 실제 저장 타입을 가져옴
    /// - 해당 정수형으로 변환하여 직렬화
    ///
    /// 예시:
    /// <code>
    /// public enum TargetStatus : byte  // underlying type = byte
    /// {
    ///     Unknown = 0,
    ///     Tracking = 1,
    ///     Lost = 2
    /// }
    ///
    /// // TargetStatus.Tracking 직렬화
    /// underlyingType = typeof(byte)
    /// value = 1 (byte)
    /// → writer.WriteByte(1)  → [01]
    /// </code>
    ///
    /// 장점:
    /// - enum의 underlying type에 맞게 최소 바이트만 사용
    /// - byte enum은 1byte, int enum은 4bytes
    /// - 버전 호환성: enum 값이 추가/변경되어도 숫자만 맞으면 역직렬화 가능
    /// </summary>
    public class EnumTypeSerializer : TypeSerializer
    {
        public override bool CanSerialize(Type type)
        {
            return type.IsEnum;
        }

        public override void Serialize(BinaryWriter writer, object value, Type type)
        {
            var underlyingType = Enum.GetUnderlyingType(type);
            var enumValue = Convert.ChangeType(value, underlyingType);

            if (underlyingType == typeof(int))
                writer.WriteInt32((int)enumValue);
            else if (underlyingType == typeof(byte))
                writer.WriteByte((byte)enumValue);
            else if (underlyingType == typeof(short))
                writer.WriteInt16((short)enumValue);
            else if (underlyingType == typeof(long))
                writer.WriteInt64((long)enumValue);
            else
                throw new NotSupportedException($"Enum underlying type {underlyingType.Name} is not supported");
        }

        public override object Deserialize(BinaryReader reader, Type type)
        {
            var underlyingType = Enum.GetUnderlyingType(type);

            if (underlyingType == typeof(int))
                return Enum.ToObject(type, reader.ReadInt32());
            else if (underlyingType == typeof(byte))
                return Enum.ToObject(type, reader.ReadByte());
            else if (underlyingType == typeof(short))
                return Enum.ToObject(type, reader.ReadInt16());
            else if (underlyingType == typeof(long))
                return Enum.ToObject(type, reader.ReadInt64());
            else
                throw new NotSupportedException($"Enum underlying type {underlyingType.Name} is not supported");
        }
    }

    /// <summary>
    /// 리스트 타입 직렬화기 (IList 컬렉션 처리)
    ///
    /// 지원 타입:
    /// - List<T> - 제네릭 리스트
    /// - T[] - 배열 (byte[] 제외, PrimitiveTypeSerializer에서 처리)
    /// - ArrayList 등 IList를 구현하는 모든 타입
    ///
    /// 직렬화 형식:
    /// [count: int32][element1][element2]...[elementN]
    /// - count가 -1이면 null 리스트
    /// - 각 요소는 BinarySerializer.SerializeObject로 재귀 직렬화
    ///
    /// 예시:
    /// <code>
    /// List<Waypoint> waypoints = [wp1, wp2, wp3];
    ///
    /// 직렬화:
    /// [03 00 00 00]  ← count = 3
    /// [wp1 바이너리]  ← 재귀: serializer.SerializeObject(wp1)
    /// [wp2 바이너리]
    /// [wp3 바이너리]
    /// </code>
    ///
    /// 재귀 직렬화:
    /// - 각 요소가 복잡한 객체여도 BinarySerializer가 알아서 처리
    /// - 중첩 리스트도 가능: List<List<int>>
    /// </summary>
    public class ListTypeSerializer : TypeSerializer
    {
        /// <summary>
        /// BinarySerializer 참조 (재귀 직렬화용)
        /// - 리스트의 각 요소를 직렬화할 때 사용
        /// - 요소 타입에 맞는 TypeSerializer를 자동으로 선택
        /// </summary>
        private readonly BinarySerializer _serializer;

        /// <summary>
        /// ListTypeSerializer 생성자
        /// </summary>
        /// <param name="serializer">재귀 직렬화에 사용할 BinarySerializer</param>
        public ListTypeSerializer(BinarySerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// IList 타입인지 확인 (byte[] 제외)
        ///
        /// byte[] 제외 이유:
        /// - byte[]도 IList를 구현하지만 BinaryWriter.WriteBytes가 더 효율적
        /// - PrimitiveTypeSerializer가 먼저 처리하도록 false 반환
        /// </summary>
        /// <param name="type">확인할 타입</param>
        /// <returns>IList 구현 타입이면 true (byte[] 제외)</returns>
        public override bool CanSerialize(Type type)
        {
            // byte[]는 PrimitiveTypeSerializer에서 처리 (BinaryWriter.WriteBytes/ReadBytes 사용)
            if (type == typeof(byte[]))
                return false;

            return typeof(IList).IsAssignableFrom(type);
        }

        /// <summary>
        /// 리스트를 바이너리로 직렬화
        ///
        /// 동작 과정:
        /// 1. null 체크 → null이면 count = -1 쓰고 종료
        /// 2. count 쓰기
        /// 3. 요소 타입 결정 (제네릭이면 GetGenericArguments, 아니면 object)
        /// 4. 각 요소를 재귀 직렬화
        ///
        /// 요소 타입 결정 예시:
        /// - List<Waypoint> → elementType = typeof(Waypoint)
        /// - ArrayList → elementType = typeof(object)
        /// - int[] → elementType = typeof(int)
        /// </summary>
        /// <param name="writer">BinaryWriter 인스턴스</param>
        /// <param name="value">직렬화할 리스트</param>
        /// <param name="type">리스트 타입</param>
        public override void Serialize(BinaryWriter writer, object value, Type type)
        {
            var list = (IList)value;

            if (list == null)
            {
                writer.WriteInt32(-1);
                return;
            }

            writer.WriteInt32(list.Count);

            var elementType = type.IsGenericType
                ? type.GetGenericArguments()[0]
                : typeof(object);

            foreach (var item in list)
            {
                _serializer.SerializeObject(writer, item, elementType);
            }
        }

        public override object Deserialize(BinaryReader reader, Type type)
        {
            var count = reader.ReadInt32();

            if (count == -1)
                return null;

            var list = (IList)Activator.CreateInstance(type);
            var elementType = type.IsGenericType
                ? type.GetGenericArguments()[0]
                : typeof(object);

            for (int i = 0; i < count; i++)
            {
                var item = _serializer.DeserializeObject(reader, elementType);
                list.Add(item);
            }

            return list;
        }
    }

    /// <summary>
    /// 복잡한 객체 직렬화기 (Reflection 기반, 사용자 정의 클래스/구조체)
    ///
    /// 지원 타입:
    /// - [Serializable] 속성이 있는 클래스
    /// - [SerializableMember(order)] 속성이 있는 속성/필드만 직렬화
    ///
    /// 직렬화 형식:
    /// [null_marker: 1 byte][member1][member2]...[memberN]
    /// - null_marker: 0 = null, 1 = not null
    /// - 멤버는 [SerializableMember(order)] 순서대로 직렬화
    /// - 각 멤버는 BinarySerializer.SerializeObject로 재귀 직렬화
    ///
    /// 예시:
    /// <code>
    /// [ITS.Serialization.Core.Serializable]
    /// public class Target
    /// {
    ///     [SerializableMember(1)]  // 첫 번째
    ///     public int ID { get; set; }
    ///
    ///     [SerializableMember(2)]  // 두 번째
    ///     public string Name { get; set; }
    ///
    ///     public string Ignored { get; set; }  // 속성 없음 → 무시
    /// }
    ///
    /// // Target { ID=101, Name="Alpha" } 직렬화
    /// [01]                   ← null_marker = 1 (not null)
    /// [65 00 00 00]         ← ID = 101 (int)
    /// [05 00 00 00 41...]   ← Name = "Alpha" (string)
    /// </code>
    ///
    /// Order가 중요한 이유:
    /// - 버전 호환성: 새 속성을 끝에 추가해도 기존 데이터 읽기 가능
    /// - 명시적 순서: 리플렉션 순서는 보장되지 않으므로 명시 필요
    /// </summary>
    public class ComplexTypeSerializer : TypeSerializer
    {
        /// <summary>
        /// BinarySerializer 참조 (재귀 직렬화용)
        /// - 각 멤버(속성/필드)를 직렬화할 때 사용
        /// - 멤버 타입에 맞는 TypeSerializer를 자동으로 선택
        /// </summary>
        private readonly BinarySerializer _serializer;

        /// <summary>
        /// ComplexTypeSerializer 생성자
        /// </summary>
        /// <param name="serializer">재귀 직렬화에 사용할 BinarySerializer</param>
        public ComplexTypeSerializer(BinarySerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// 클래스 또는 구조체 타입인지 확인
        ///
        /// 주의:
        /// - 모든 클래스와 구조체를 허용하지만 실제로는 [Serializable] 속성 필요
        /// - [SerializableMember] 속성이 하나도 없으면 아무것도 직렬화 안 됨
        /// - 가장 마지막에 호출되므로 다른 Serializer가 처리 못한 것만 여기로 옴
        /// </summary>
        /// <param name="type">확인할 타입</param>
        /// <returns>클래스 또는 구조체면 true</returns>
        public override bool CanSerialize(Type type)
        {
            return type.IsClass || type.IsValueType;
        }

        /// <summary>
        /// 복잡한 객체를 바이너리로 직렬화
        ///
        /// 동작 과정:
        /// 1. null 체크 → null이면 marker=0 쓰고 종료
        /// 2. null이 아니면 marker=1 쓰기
        /// 3. GetSerializableMembers로 직렬화할 멤버 목록 가져오기
        ///    - [SerializableMember] 속성이 있는 속성/필드만 선택
        ///    - Order 순서대로 정렬
        /// 4. 각 멤버의 값을 가져와 재귀 직렬화
        ///
        /// 재귀 직렬화 예시:
        /// <code>
        /// ExtAircraft
        ///   → ID (int) → PrimitiveTypeSerializer
        ///   → Callsign (string) → PrimitiveTypeSerializer
        ///   → WaypointList (List<Waypoint>) → ListTypeSerializer
        ///     → Waypoint[0] → ComplexTypeSerializer (재귀)
        ///       → ID (int) → PrimitiveTypeSerializer
        ///       → Name (string) → PrimitiveTypeSerializer
        ///       → ...
        /// </code>
        /// </summary>
        /// <param name="writer">BinaryWriter 인스턴스</param>
        /// <param name="value">직렬화할 객체</param>
        /// <param name="type">객체 타입</param>
        public override void Serialize(BinaryWriter writer, object value, Type type)
        {
            if (value == null)
            {
                writer.WriteByte(0); // null marker
                return;
            }

            writer.WriteByte(1); // not null marker

            var members = GetSerializableMembers(type);

            foreach (var member in members)
            {
                var memberValue = GetMemberValue(member, value);
                var memberType = GetMemberType(member);

                _serializer.SerializeObject(writer, memberValue, memberType);
            }
        }

        public override object Deserialize(BinaryReader reader, Type type)
        {
            var nullMarker = reader.ReadByte();

            if (nullMarker == 0)
                return null;

            var instance = Activator.CreateInstance(type);
            var members = GetSerializableMembers(type);

            foreach (var member in members)
            {
                var memberType = GetMemberType(member);
                var memberValue = _serializer.DeserializeObject(reader, memberType);

                SetMemberValue(member, instance, memberValue);
            }

            return instance;
        }

        /// <summary>
        /// [SerializableMember] 속성이 있는 멤버 목록 가져오기 (Reflection)
        ///
        /// 동작:
        /// 1. Public 인스턴스 속성 중 CanRead && CanWrite인 것 선택
        /// 2. [SerializableMember] 속성이 있는 것만 필터링
        /// 3. Order 순서대로 정렬
        /// 4. Public 인스턴스 필드도 동일하게 처리
        /// 5. 속성 + 필드를 합쳐서 반환
        ///
        /// 예시:
        /// <code>
        /// class Target {
        ///     [SerializableMember(2)] public string Name { get; set; }  // 두 번째
        ///     [SerializableMember(1)] public int ID { get; set; }       // 첫 번째
        ///     public double Ignored { get; set; }                       // 무시됨
        /// }
        ///
        /// 결과: [ID, Name]  // Order 순서대로
        /// </code>
        /// </summary>
        /// <param name="type">멤버를 가져올 타입</param>
        /// <returns>[SerializableMember] 속성이 있는 멤버 배열 (Order 순)</returns>
        private MemberInfo[] GetSerializableMembers(Type type)
        {
            var members = new List<MemberInfo>();

            // Properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.GetCustomAttribute<SerializableMemberAttribute>() != null)
                .OrderBy(p => p.GetCustomAttribute<SerializableMemberAttribute>().Order);

            members.AddRange(properties);

            // Fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<SerializableMemberAttribute>() != null)
                .OrderBy(f => f.GetCustomAttribute<SerializableMemberAttribute>().Order);

            members.AddRange(fields);

            return members.ToArray();
        }

        /// <summary>
        /// 멤버(속성 또는 필드)의 값 가져오기
        /// </summary>
        /// <param name="member">속성 또는 필드 정보</param>
        /// <param name="obj">값을 읽을 객체</param>
        /// <returns>멤버 값</returns>
        private object GetMemberValue(MemberInfo member, object obj)
        {
            if (member is PropertyInfo prop)
                return prop.GetValue(obj);
            else if (member is FieldInfo field)
                return field.GetValue(obj);
            else
                throw new NotSupportedException($"Member type {member.GetType().Name} is not supported");
        }

        /// <summary>
        /// 멤버(속성 또는 필드)에 값 설정하기
        /// </summary>
        /// <param name="member">속성 또는 필드 정보</param>
        /// <param name="obj">값을 쓸 객체</param>
        /// <param name="value">설정할 값</param>
        private void SetMemberValue(MemberInfo member, object obj, object value)
        {
            if (member is PropertyInfo prop)
                prop.SetValue(obj, value);
            else if (member is FieldInfo field)
                field.SetValue(obj, value);
        }

        /// <summary>
        /// 멤버(속성 또는 필드)의 타입 가져오기
        /// </summary>
        /// <param name="member">속성 또는 필드 정보</param>
        /// <returns>멤버의 타입</returns>
        private Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo prop)
                return prop.PropertyType;
            else if (member is FieldInfo field)
                return field.FieldType;
            else
                throw new NotSupportedException($"Member type {member.GetType().Name} is not supported");
        }
    }
}
