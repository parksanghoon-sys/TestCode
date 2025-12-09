# ITS.Serialization

Custom binary serialization library for ITS system using OOP design patterns.

## Overview

This library provides a high-performance, attribute-based binary serialization framework designed specifically for the ITS (Integrated Training System) delta update protocol. It replaces the slow reflection-based text serialization with efficient binary serialization while maintaining protocol compatibility.

## Key Features

- **Attribute-based serialization** - Clean API using `[Serializable]` and `[SerializableMember(order)]` attributes
- **OOP design patterns** - Strategy Pattern, Template Method Pattern, Wrapper Pattern
- **High performance** - ~80% size reduction compared to text-based serialization
- **Type-safe** - Compile-time type checking with runtime validation
- **Delta update support** - Full support for ITS delta command protocol (Add, Remove, Update, Set, Reset)
- **Header + Payload serialization** - Convenient methods for protocol header handling
- **Struct and Class support** - Separate methods for value types and reference types
- **netstandard2.0** - Compatible with .NET Framework 4.8+ and modern .NET

## Architecture

```
ITS.Serialization/
├── Core/
│   ├── ISerializer.cs              - Strategy Pattern interface
│   ├── BinarySerializer.cs         - Main serializer (Template Method Pattern)
│   ├── BinaryWriter.cs             - Low-level binary writer wrapper
│   ├── BinaryReader.cs             - Low-level binary reader wrapper
│   ├── TypeSerializer.cs           - Abstract base + concrete serializers
│   │   ├── PrimitiveTypeSerializer - Handles primitives (int, float, string, etc.)
│   │   ├── EnumTypeSerializer      - Handles enums with underlying type support
│   │   ├── ListTypeSerializer      - Handles IList collections recursively
│   │   └── ComplexTypeSerializer   - Handles classes/structs with reflection
│   └── SerializableAttribute.cs    - Attribute system
└── Protocol/
    ├── CommandType.cs              - Delta command enumeration
    ├── DeltaCommand.cs             - Single delta command structure
    ├── DeltaBatch.cs               - Batch command structure
    └── NetworkMessage.cs           - Message wrapper (DeltaUpdate, FullSync, Batch)
```

## Design Patterns

### 1. Strategy Pattern
Different serialization strategies for different types:
- `ISerializer` interface defines the contract
- `TypeSerializer` abstract base class for type-specific serializers
- Multiple concrete serializers handle different type categories

### 2. Template Method Pattern
`BinarySerializer` defines the serialization flow:
1. Find appropriate serializer for type
2. Delegate to type-specific serializer
3. Handle errors and validation

### 3. Wrapper Pattern
`BinaryWriter` and `BinaryReader` wrap `Stream` to provide:
- Type-safe primitive operations
- Length-prefixed strings and byte arrays
- Stream lifetime management (`leaveOpen` parameter)

## Usage Examples

### Basic Object Serialization

```csharp
using ITS.Serialization.Core;

[ITS.Serialization.Core.Serializable]
public class Target
{
    [SerializableMember(1)]
    public int ID { get; set; }

    [SerializableMember(2)]
    public string Name { get; set; }

    [SerializableMember(3)]
    public double Latitude { get; set; }
}

var serializer = new BinarySerializer();
var target = new Target { ID = 101, Name = "Alpha", Latitude = 37.5 };

// Serialize
byte[] data = serializer.Serialize(target);

// Deserialize
var restored = serializer.Deserialize<Target>(data);
```

### Delta Command Protocol

```csharp
using ITS.Serialization.Protocol;

// Remove command: TargetList.Remove(2)
var removeCmd = new DeltaCommand
{
    Path = "TargetList",
    Command = CommandType.Remove,
    Index = 2
};

// Add command: ExtAircraftList.Add(aircraft)
var aircraft = new ExtAircraft { ID = 999, Callsign = "NEW001" };
var addCmd = new DeltaCommand
{
    Path = "ExtAircraftList",
    Command = CommandType.Add,
    ID = aircraft.ID,
    Payload = serializer.Serialize(aircraft)
};

// Update command: ExtAircraftList[0].Altitude = 15000
var updateCmd = new DeltaCommand
{
    Path = "ExtAircraftList[0].Altitude",
    Command = CommandType.Update,
    Payload = serializer.Serialize(15000.0)
};
```

### Network Message Protocol

```csharp
// Delta update message
var deltaMsg = new NetworkMessage
{
    Type = MessageType.DeltaUpdate,
    Delta = new DeltaCommand { /* ... */ }
};

// Full sync message
var fullSyncMsg = new NetworkMessage
{
    Type = MessageType.FullSync,
    FullState = serializer.Serialize(mcrcData)
};

// Batch message
var batchMsg = new NetworkMessage
{
    Type = MessageType.Batch,
    Batch = new DeltaBatch
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Commands = { cmd1, cmd2, cmd3 }
    }
};

byte[] msgData = serializer.Serialize(batchMsg);
```

## Performance

Test results with realistic ITS data (10 aircraft with 3 waypoints each + 20 targets):

- **Binary serialization**: 2,783 bytes
- **Text serialization**: 13,831 bytes
- **Size ratio**: 4.97x smaller
- **Size reduction**: 79.9% less bandwidth

### Protocol Size Examples

- `TargetList.Remove(2)`: 31 bytes
- `ExtAircraftList.Add(aircraft)`: 95 bytes (with full aircraft object)
- `ExtAircraftList[0].Altitude = 15000`: 56 bytes
- `DeltaBatch` with 3 commands: 153 bytes
- Complex `ExtAircraft` with 2 waypoints: 135 bytes

## Building and Testing

```bash
# Build solution
cd lib
dotnet build ITS.Serialization.sln

# Run tests
cd ITS.Serialization.Tests
dotnet run
```

## Test Coverage

The test program (`ITS.Serialization.Tests`) includes:

1. **Test 1**: Basic object serialization (Target with primitives and enum)
2. **Test 2**: Complex nested objects (ExtAircraft with List<Waypoint>)
3. **Test 3**: Delta command protocol (Remove, Add, Update scenarios)
4. **Test 4**: Network message protocol (DeltaUpdate, FullSync)
5. **Test 5**: Batch command protocol (multiple commands in one message)
6. **Test 6**: Performance comparison (binary vs text serialization)

All tests passed successfully with proper serialization/deserialization verification.

## Integration with ITS

To integrate this library into the ITS system:

1. **Reference the library** in your ITS projects
2. **Mark model classes** with `[ITS.Serialization.Core.Serializable]`
3. **Mark properties** with `[SerializableMember(order)]` in the order you want them serialized
4. **Use correct serialization methods**:
   - **ITS Internal (Manager ↔ Manager)**: Use `Serialize()` / `Deserialize()` only (SendData/ParsingPacket handles headers)
   - **External TCP**: Use `SerializeWithHeaderClass()` / `DeserializeWithHeaderClass()` (direct socket communication)
   - **File I/O**: Choose based on format (with or without header)

Example migration:
```csharp
// ✅ ITS Internal: GetObject() - Payload only
public override byte[] GetObject(int index)
{
    return serializer.Serialize(m_datalist[index].m_data);  // No header!
}

// ✅ ITS Internal: Receive() - Payload only
public override int Receive(byte[] Data, int Length)
{
    var obj = serializer.Deserialize<SFlightStatus>(Data);  // No header!
    ProcessReceiveData(obj);
    return Length;
}
```

**⚠️ IMPORTANT**: Read [HEADER_SERIALIZATION_GUIDE.md](HEADER_SERIALIZATION_GUIDE.md) for correct usage!

## Dependencies

- **Target Framework**: netstandard2.0
- **External Dependencies**: None (fully self-contained)
- **Compatible with**: .NET Framework 4.8+, .NET Core 2.0+, .NET 5+

## License

Internal use for ITS project.
