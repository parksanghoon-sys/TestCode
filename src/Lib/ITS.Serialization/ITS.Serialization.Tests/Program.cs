using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ITS.Serialization.Core;
using ITS.Serialization.Protocol;
using ITS.Serialization.Tests.Models;

namespace ITS.Serialization.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("ITS.Serialization 테스트 프로그램");
            Console.WriteLine("Custom Binary Serialization Library Test");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            // Test 1: 기본 객체 직렬화
            Test1_BasicSerialization();

            // Test 2: 복잡한 중첩 객체 직렬화
            Test2_ComplexSerialization();

            // Test 3: Delta Command 프로토콜
            Test3_DeltaCommand();

            // Test 4: Network Message 프로토콜
            Test4_NetworkMessage();

            // Test 5: Batch Command 프로토콜
            Test5_BatchCommand();

            // Test 6: 성능 비교 (바이너리 vs 텍스트)
            Test6_PerformanceComparison();

            // Test 7: 헤더 + 페이로드 직렬화 (범용 프로토콜)
            Test7_SerializeWithHeader();

            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("모든 테스트 완료!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void Test1_BasicSerialization()
        {
            Console.WriteLine("[Test 1] 기본 객체 직렬화 테스트");
            Console.WriteLine("-".PadRight(80, '-'));

            var target = new Target
            {
                ID = 101,
                Name = "Target-Alpha",
                Status = TargetStatus.Tracking,
                Latitude = 37.566535,
                Longitude = 126.977969,
                Altitude = 500.0
            };

            Console.WriteLine($"원본 객체: {target}");

            var serializer = new BinarySerializer();
            var data = serializer.Serialize(target);

            Console.WriteLine($"직렬화 크기: {data.Length} bytes");
            Console.WriteLine($"바이트 데이터: {BitConverter.ToString(data)}");

            var restored = serializer.Deserialize<Target>(data);
            Console.WriteLine($"역직렬화: {restored}");

            Console.WriteLine($"검증: ID={restored.ID}, Name={restored.Name}, Status={restored.Status}");
            Console.WriteLine();
        }

        static void Test2_ComplexSerialization()
        {
            Console.WriteLine("[Test 2] 복잡한 중첩 객체 직렬화 테스트");
            Console.WriteLine("-".PadRight(80, '-'));

            var aircraft = new ExtAircraft
            {
                ID = 201,
                Callsign = "KAL123",
                Latitude = 37.5,
                Longitude = 127.0,
                Altitude = 10000.0,
                Speed = 450.0,
                Heading = 90.0
            };

            aircraft.WaypointList.Add(new Waypoint
            {
                ID = 1,
                Name = "WPT-A",
                Latitude = 37.6,
                Longitude = 127.1,
                Altitude = 11000.0
            });

            aircraft.WaypointList.Add(new Waypoint
            {
                ID = 2,
                Name = "WPT-B",
                Latitude = 37.7,
                Longitude = 127.2,
                Altitude = 12000.0
            });

            Console.WriteLine($"원본 객체: {aircraft}");
            foreach (var wp in aircraft.WaypointList)
            {
                Console.WriteLine($"  - {wp}");
            }

            var serializer = new BinarySerializer();
            var data = serializer.Serialize(aircraft);

            Console.WriteLine($"직렬화 크기: {data.Length} bytes");

            var restored = serializer.Deserialize<ExtAircraft>(data);
            Console.WriteLine($"역직렬화: {restored}");
            foreach (var wp in restored.WaypointList)
            {
                Console.WriteLine($"  - {wp}");
            }

            Console.WriteLine();
        }

        static void Test3_DeltaCommand()
        {
            Console.WriteLine("[Test 3] Delta Command 프로토콜 테스트");
            Console.WriteLine("-".PadRight(80, '-'));

            var serializer = new BinarySerializer();

            // Scenario 1: TargetList.Remove(2)
            Console.WriteLine("Scenario 1: TargetList.Remove(2)");
            var removeCmd = new DeltaCommand
            {
                Path = "TargetList",
                Command = CommandType.Remove,
                Index = 2,
                ID = -1,
                Payload = null
            };

            var removeData = serializer.Serialize(removeCmd);
            Console.WriteLine($"  Command: {removeCmd}");
            Console.WriteLine($"  Size: {removeData.Length} bytes");

            // Scenario 2: ExtAircraftList.Add(aircraft)
            Console.WriteLine();
            Console.WriteLine("Scenario 2: ExtAircraftList.Add(aircraft)");

            var newAircraft = new ExtAircraft
            {
                ID = 999,
                Callsign = "NEW001",
                Latitude = 35.0,
                Longitude = 128.0,
                Altitude = 5000.0,
                Speed = 300.0,
                Heading = 180.0
            };

            var aircraftPayload = serializer.Serialize(newAircraft);

            var addCmd = new DeltaCommand
            {
                Path = "ExtAircraftList",
                Command = CommandType.Add,
                Index = -1,
                ID = newAircraft.ID,
                Payload = aircraftPayload
            };

            var addData = serializer.Serialize(addCmd);
            Console.WriteLine($"  Command: {addCmd}");
            Console.WriteLine($"  Size: {addData.Length} bytes");

            // Scenario 3: ExtAircraftList[0].Altitude = 15000
            Console.WriteLine();
            Console.WriteLine("Scenario 3: ExtAircraftList[0].Altitude = 15000");

            var altitudePayload = serializer.Serialize(15000.0);

            var updateCmd = new DeltaCommand
            {
                Path = "ExtAircraftList[0].Altitude",
                Command = CommandType.Update,
                Index = -1,
                ID = -1,
                Payload = altitudePayload
            };

            var updateData = serializer.Serialize(updateCmd);
            Console.WriteLine($"  Command: {updateCmd}");
            Console.WriteLine($"  Size: {updateData.Length} bytes");

            Console.WriteLine();
        }

        static void Test4_NetworkMessage()
        {
            Console.WriteLine("[Test 4] Network Message 프로토콜 테스트");
            Console.WriteLine("-".PadRight(80, '-'));

            var serializer = new BinarySerializer();

            // DeltaUpdate 메시지
            var deltaMsg = new NetworkMessage
            {
                Type = MessageType.DeltaUpdate,
                Delta = new DeltaCommand
                {
                    Path = "TargetList",
                    Command = CommandType.Remove,
                    Index = 2
                }
            };

            var deltaData = serializer.Serialize(deltaMsg);
            Console.WriteLine($"DeltaUpdate 메시지: {deltaMsg}");
            Console.WriteLine($"  Size: {deltaData.Length} bytes");

            var restoredDelta = serializer.Deserialize<NetworkMessage>(deltaData);
            Console.WriteLine($"  복원: Type={restoredDelta.Type}, Delta={restoredDelta.Delta}");

            // FullSync 메시지
            var mcrcData = new MCRCData
            {
                DeviceID = 1,
                DeviceName = "MCRC-001",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var fullStatePayload = serializer.Serialize(mcrcData);

            var fullSyncMsg = new NetworkMessage
            {
                Type = MessageType.FullSync,
                FullState = fullStatePayload
            };

            var fullSyncData = serializer.Serialize(fullSyncMsg);
            Console.WriteLine();
            Console.WriteLine($"FullSync 메시지: {fullSyncMsg}");
            Console.WriteLine($"  Size: {fullSyncData.Length} bytes");

            var restoredSync = serializer.Deserialize<NetworkMessage>(fullSyncData);
            Console.WriteLine($"  복원: Type={restoredSync.Type}, FullState.Length={restoredSync.FullState.Length}");

            Console.WriteLine();
        }

        static void Test5_BatchCommand()
        {
            Console.WriteLine("[Test 5] Batch Command 프로토콜 테스트");
            Console.WriteLine("-".PadRight(80, '-'));

            var serializer = new BinarySerializer();

            var batch = new DeltaBatch
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // 여러 명령어를 배치로 묶기
            batch.Commands.Add(new DeltaCommand
            {
                Path = "TargetList",
                Command = CommandType.Remove,
                Index = 1
            });

            batch.Commands.Add(new DeltaCommand
            {
                Path = "ExtAircraftList[0].Altitude",
                Command = CommandType.Update,
                Payload = serializer.Serialize(12000.0)
            });

            batch.Commands.Add(new DeltaCommand
            {
                Path = "ExtAircraftList[0].Speed",
                Command = CommandType.Update,
                Payload = serializer.Serialize(480.0)
            });

            Console.WriteLine($"배치 커맨드: {batch}");
            foreach (var cmd in batch.Commands)
            {
                Console.WriteLine($"  - {cmd}");
            }

            var batchData = serializer.Serialize(batch);
            Console.WriteLine($"직렬화 크기: {batchData.Length} bytes");

            var restoredBatch = serializer.Deserialize<DeltaBatch>(batchData);
            Console.WriteLine($"역직렬화: {restoredBatch}");
            foreach (var cmd in restoredBatch.Commands)
            {
                Console.WriteLine($"  - {cmd}");
            }

            // NetworkMessage로 감싸기
            var batchMsg = new NetworkMessage
            {
                Type = MessageType.Batch,
                Batch = batch
            };

            var msgData = serializer.Serialize(batchMsg);
            Console.WriteLine();
            Console.WriteLine($"NetworkMessage(Batch) 크기: {msgData.Length} bytes");

            Console.WriteLine();
        }

        static void Test6_PerformanceComparison()
        {
            Console.WriteLine("[Test 6] 성능 비교 (바이너리 vs 텍스트)");
            Console.WriteLine("-".PadRight(80, '-'));

            var mcrcData = new MCRCData
            {
                DeviceID = 1,
                DeviceName = "MCRC-Simulator-001",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // ExtAircraft 10개 추가
            for (int i = 0; i < 10; i++)
            {
                var aircraft = new ExtAircraft
                {
                    ID = 1000 + i,
                    Callsign = $"KAL{1000 + i}",
                    Latitude = 37.5 + i * 0.1,
                    Longitude = 127.0 + i * 0.1,
                    Altitude = 10000.0 + i * 1000,
                    Speed = 400.0 + i * 10,
                    Heading = i * 36.0
                };

                // 각 항공기에 waypoint 3개씩
                for (int j = 0; j < 3; j++)
                {
                    aircraft.WaypointList.Add(new Waypoint
                    {
                        ID = j + 1,
                        Name = $"WPT-{i}-{j}",
                        Latitude = 37.5 + i * 0.1 + j * 0.05,
                        Longitude = 127.0 + i * 0.1 + j * 0.05,
                        Altitude = 10000.0 + j * 500
                    });
                }

                mcrcData.ExtAircraftList.Add(aircraft);
            }

            // Target 20개 추가
            for (int i = 0; i < 20; i++)
            {
                mcrcData.TargetList.Add(new Target
                {
                    ID = 2000 + i,
                    Name = $"Target-{i:D3}",
                    Status = (TargetStatus)(i % 4),
                    Latitude = 36.0 + i * 0.05,
                    Longitude = 128.0 + i * 0.05,
                    Altitude = 500.0 + i * 100
                });
            }

            Console.WriteLine($"테스트 데이터: {mcrcData}");
            Console.WriteLine($"  - ExtAircraft: {mcrcData.ExtAircraftList.Count}개 (각각 Waypoint 3개)");
            Console.WriteLine($"  - Target: {mcrcData.TargetList.Count}개");
            Console.WriteLine();

            // 바이너리 직렬화
            var serializer = new BinarySerializer();
            var binaryData = serializer.Serialize(mcrcData);

            Console.WriteLine($"바이너리 직렬화 크기: {binaryData.Length} bytes");

            // 텍스트 직렬화 (리플렉션 방식 시뮬레이션)
            var textData = SimulateReflectionSerialization(mcrcData);
            Console.WriteLine($"텍스트 직렬화 크기: {textData.Length} bytes (UTF-8)");

            var ratio = (double)textData.Length / binaryData.Length;
            Console.WriteLine($"크기 비율: {ratio:F2}x (텍스트가 바이너리보다 {ratio:F2}배 큼)");

            var reduction = (1.0 - 1.0 / ratio) * 100;
            Console.WriteLine($"크기 절감: {reduction:F1}% 감소");

            Console.WriteLine();
        }

        static byte[] SimulateReflectionSerialization(MCRCData data)
        {
            // ITS 시스템의 리플렉션 기반 텍스트 직렬화 시뮬레이션
            var sb = new StringBuilder();

            sb.AppendLine($"DeviceID={data.DeviceID}");
            sb.AppendLine($"DeviceName={data.DeviceName}");
            sb.AppendLine($"Timestamp={data.Timestamp}");

            sb.AppendLine($"ExtAircraftList.Count={data.ExtAircraftList.Count}");
            for (int i = 0; i < data.ExtAircraftList.Count; i++)
            {
                var aircraft = data.ExtAircraftList[i];
                sb.AppendLine($"ExtAircraftList[{i}].ID={aircraft.ID}");
                sb.AppendLine($"ExtAircraftList[{i}].Callsign={aircraft.Callsign}");
                sb.AppendLine($"ExtAircraftList[{i}].Latitude={aircraft.Latitude}");
                sb.AppendLine($"ExtAircraftList[{i}].Longitude={aircraft.Longitude}");
                sb.AppendLine($"ExtAircraftList[{i}].Altitude={aircraft.Altitude}");
                sb.AppendLine($"ExtAircraftList[{i}].Speed={aircraft.Speed}");
                sb.AppendLine($"ExtAircraftList[{i}].Heading={aircraft.Heading}");
                sb.AppendLine($"ExtAircraftList[{i}].WaypointList.Count={aircraft.WaypointList.Count}");

                for (int j = 0; j < aircraft.WaypointList.Count; j++)
                {
                    var wp = aircraft.WaypointList[j];
                    sb.AppendLine($"ExtAircraftList[{i}].WaypointList[{j}].ID={wp.ID}");
                    sb.AppendLine($"ExtAircraftList[{i}].WaypointList[{j}].Name={wp.Name}");
                    sb.AppendLine($"ExtAircraftList[{i}].WaypointList[{j}].Latitude={wp.Latitude}");
                    sb.AppendLine($"ExtAircraftList[{i}].WaypointList[{j}].Longitude={wp.Longitude}");
                    sb.AppendLine($"ExtAircraftList[{i}].WaypointList[{j}].Altitude={wp.Altitude}");
                }
            }

            sb.AppendLine($"TargetList.Count={data.TargetList.Count}");
            for (int i = 0; i < data.TargetList.Count; i++)
            {
                var target = data.TargetList[i];
                sb.AppendLine($"TargetList[{i}].ID={target.ID}");
                sb.AppendLine($"TargetList[{i}].Name={target.Name}");
                sb.AppendLine($"TargetList[{i}].Status={target.Status}");
                sb.AppendLine($"TargetList[{i}].Latitude={target.Latitude}");
                sb.AppendLine($"TargetList[{i}].Longitude={target.Longitude}");
                sb.AppendLine($"TargetList[{i}].Altitude={target.Altitude}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        static void Test7_SerializeWithHeader()
        {
            Console.WriteLine("[Test 7] 헤더 + 페이로드 직렬화 테스트 (범용 프로토콜)");
            Console.WriteLine("-".PadRight(80, '-'));

            var serializer = new BinarySerializer();

            // ===== Scenario 1: E_HOSTNETWORK_HEADER + FlightStatus =====
            Console.WriteLine("Scenario 1: ITS 네트워크 프로토콜 (E_HOSTNETWORK_HEADER + Target)");
            Console.WriteLine();

            // 페이로드 생성
            var target = new Target
            {
                ID = 101,
                Name = "Target-Alpha",
                Status = TargetStatus.Tracking,
                Latitude = 37.566535,
                Longitude = 126.977969,
                Altitude = 500.0
            };

            // 페이로드 직렬화하여 크기 계산
            byte[] targetPayload = serializer.Serialize(target);

            // 헤더 생성 (messagesize를 페이로드 크기로 설정)
            var header = new NetworkHeader
            {
                sync = 0xE179,
                messageid = 1,
                messagesize = targetPayload.Length
            };

            Console.WriteLine($"  헤더: sync=0x{header.sync:X4}, messageid={header.messageid}, messagesize={header.messagesize}");
            Console.WriteLine($"  페이로드: {target}");
            Console.WriteLine();

            // ★ 헤더 + 페이로드 결합 직렬화 (class는 SerializeWithHeaderClass 사용)
            byte[] packet = serializer.SerializeWithHeaderClass(header, target);

            Console.WriteLine($"  패킷 크기: {packet.Length} bytes");
            Console.WriteLine($"    - 헤더: {System.Runtime.InteropServices.Marshal.SizeOf<NetworkHeader>()} bytes");
            Console.WriteLine($"    - 페이로드: {targetPayload.Length} bytes");
            Console.WriteLine($"  패킷 데이터 (처음 40 bytes): {BitConverter.ToString(packet, 0, Math.Min(40, packet.Length))}");
            Console.WriteLine();

            // ★ 헤더 + 페이로드 분리 역직렬화 (class는 DeserializeWithHeaderClass 사용)
            var (receivedHeader, receivedTarget) = serializer.DeserializeWithHeaderClass<NetworkHeader, Target>(packet);

            Console.WriteLine($"  수신 헤더: sync=0x{receivedHeader.sync:X4}, messageid={receivedHeader.messageid}, messagesize={receivedHeader.messagesize}");
            Console.WriteLine($"  수신 페이로드: {receivedTarget}");

            // 헤더 검증
            if (receivedHeader.sync != 0xE179)
            {
                Console.WriteLine("  [ERROR] 헤더 sync 불일치!");
            }
            else
            {
                Console.WriteLine("  [OK] 헤더 검증 성공");
            }

            // 데이터 검증
            bool dataValid = receivedTarget.ID == target.ID &&
                           receivedTarget.Name == target.Name &&
                           receivedTarget.Status == target.Status;
            Console.WriteLine($"  [OK] 데이터 검증: {(dataValid ? "성공" : "실패")}");
            Console.WriteLine();
            Console.WriteLine();

            // ===== Scenario 2: 커스텀 프로토콜 헤더 =====
            Console.WriteLine("Scenario 2: 커스텀 프로토콜 (MyProtocolHeader + ExtAircraft)");
            Console.WriteLine();

            var aircraft = new ExtAircraft
            {
                ID = 201,
                Callsign = "KAL123",
                Latitude = 37.5,
                Longitude = 127.0,
                Altitude = 10000.0,
                Speed = 450.0,
                Heading = 90.0
            };

            aircraft.WaypointList.Add(new Waypoint
            {
                ID = 1,
                Name = "WPT-A",
                Latitude = 37.6,
                Longitude = 127.1,
                Altitude = 11000.0
            });

            // 커스텀 헤더 생성
            byte[] aircraftPayload = serializer.Serialize(aircraft);
            var customHeader = new CustomProtocolHeader
            {
                Version = 1,
                CommandType = 100,
                SequenceNumber = 42,
                PayloadLength = aircraftPayload.Length,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Console.WriteLine($"  커스텀 헤더: Version={customHeader.Version}, CommandType={customHeader.CommandType}, Seq={customHeader.SequenceNumber}");
            Console.WriteLine($"  페이로드: {aircraft}");
            Console.WriteLine();

            // ★ 커스텀 헤더 + 페이로드 결합 직렬화 (class는 SerializeWithHeaderClass 사용)
            byte[] customPacket = serializer.SerializeWithHeaderClass(customHeader, aircraft);

            Console.WriteLine($"  패킷 크기: {customPacket.Length} bytes");
            Console.WriteLine($"    - 헤더: {System.Runtime.InteropServices.Marshal.SizeOf<CustomProtocolHeader>()} bytes");
            Console.WriteLine($"    - 페이로드: {aircraftPayload.Length} bytes");
            Console.WriteLine();

            // ★ 커스텀 헤더 + 페이로드 분리 역직렬화 (class는 DeserializeWithHeaderClass 사용)
            var (receivedCustomHeader, receivedAircraft) = serializer.DeserializeWithHeaderClass<CustomProtocolHeader, ExtAircraft>(customPacket);

            Console.WriteLine($"  수신 헤더: Version={receivedCustomHeader.Version}, CommandType={receivedCustomHeader.CommandType}, Seq={receivedCustomHeader.SequenceNumber}");
            Console.WriteLine($"  수신 페이로드: {receivedAircraft}");
            Console.WriteLine($"    - Waypoints: {receivedAircraft.WaypointList.Count}개");

            bool customValid = receivedCustomHeader.Version == customHeader.Version &&
                             receivedCustomHeader.SequenceNumber == customHeader.SequenceNumber &&
                             receivedAircraft.ID == aircraft.ID;
            Console.WriteLine($"  [OK] 데이터 검증: {(customValid ? "성공" : "실패")}");
            Console.WriteLine();
            Console.WriteLine();

            // ===== Scenario 3: 기존 SendData() vs SerializeWithHeader 비교 =====
            Console.WriteLine("Scenario 3: 기존 방식 vs SerializeWithHeader 비교");
            Console.WriteLine();

            // 기존 방식 시뮬레이션 (Marshal 사용)
            Console.WriteLine("  [기존 방식] Marshal + Array.Copy");
            int headerSize = System.Runtime.InteropServices.Marshal.SizeOf<NetworkHeader>();
            int dataSize = targetPayload.Length;
            byte[] oldStylePacket = new byte[headerSize + dataSize];

            // 데이터 먼저 복사
            Array.Copy(targetPayload, 0, oldStylePacket, headerSize, dataSize);

            // 헤더를 Marshal로 복사
            IntPtr headerPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(headerSize);
            System.Runtime.InteropServices.Marshal.StructureToPtr(header, headerPtr, true);
            System.Runtime.InteropServices.Marshal.Copy(headerPtr, oldStylePacket, 0, headerSize);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(headerPtr);

            Console.WriteLine($"    - 코드 라인: ~10줄");
            Console.WriteLine($"    - Marshal 호출: 4회 (AllocHGlobal, StructureToPtr, Copy, FreeHGlobal)");
            Console.WriteLine($"    - 패킷 크기: {oldStylePacket.Length} bytes");
            Console.WriteLine();

            // SerializeWithHeaderClass 방식
            Console.WriteLine("  [새로운 방식] SerializeWithHeaderClass");
            byte[] newStylePacket = serializer.SerializeWithHeaderClass(header, target);
            Console.WriteLine($"    - 코드 라인: 1줄");
            Console.WriteLine($"    - Marshal 호출: 0회 (BinarySerializer 내부 처리)");
            Console.WriteLine($"    - 패킷 크기: {newStylePacket.Length} bytes");
            Console.WriteLine();

            // 바이트 데이터 비교
            bool identical = oldStylePacket.Length == newStylePacket.Length;
            if (identical)
            {
                for (int i = 0; i < oldStylePacket.Length; i++)
                {
                    if (oldStylePacket[i] != newStylePacket[i])
                    {
                        identical = false;
                        Console.WriteLine($"  [WARNING] 바이트 차이 발견: 위치 {i}, 기존={oldStylePacket[i]:X2}, 신규={newStylePacket[i]:X2}");
                        break;
                    }
                }
            }

            Console.WriteLine($"  [결과] 패킷 동일성: {(identical ? "100% 일치" : "불일치")}");
            Console.WriteLine($"  [장점]");
            Console.WriteLine($"    - 코드 간결성: 90% 감소 (10줄 → 1줄)");
            Console.WriteLine($"    - 타입 안전성: 컴파일 타임 체크");
            Console.WriteLine($"    - 유지보수성: 명확한 API");
            Console.WriteLine($"    - 범용성: 모든 헤더 타입 지원");
            Console.WriteLine();
        }
    }

    // ===== 테스트용 헤더 구조체 =====

    /// <summary>
    /// ITS 네트워크 헤더 (E_HOSTNETWORK_HEADER 시뮬레이션)
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct NetworkHeader
    {
        public ushort sync;         // 2 bytes: 0xE179
        public int messageid;       // 4 bytes: 메시지 ID
        public int messagesize;     // 4 bytes: 페이로드 크기
                                    // 총 10 bytes
    }

    /// <summary>
    /// 커스텀 프로토콜 헤더 (범용성 테스트)
    /// </summary>
    [ITS.Serialization.Core.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct CustomProtocolHeader
    {
        [ITS.Serialization.Core.SerializableMember(1)]
        public byte Version;            // 1 byte: 프로토콜 버전

        [ITS.Serialization.Core.SerializableMember(2)]
        public byte CommandType;        // 1 byte: 커맨드 타입

        [ITS.Serialization.Core.SerializableMember(3)]
        public ushort SequenceNumber;   // 2 bytes: 시퀀스 번호

        [ITS.Serialization.Core.SerializableMember(4)]
        public int PayloadLength;       // 4 bytes: 페이로드 길이

        [ITS.Serialization.Core.SerializableMember(5)]
        public long Timestamp;          // 8 bytes: Unix timestamp (milliseconds)
                                        // 총 16 bytes
    }
}
