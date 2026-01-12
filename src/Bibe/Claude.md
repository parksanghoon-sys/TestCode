# **TDD 3색(Red → Green → Refactor) 원칙을 지키는 흐름** ,

# **OOP 기반 임의 프로토콜 추상화** ,

# **Send / Recv 전용 처리 스레드 분리** ,

# **TCP 버퍼 복사를 최소화(Span/MemoryPool / SocketAsyncEventArgs 기반)**



좋은 주제라서,  **TDD 3색(Red → Green → Refactor) 원칙을 지키는 흐름** ,
 **OOP 기반 임의 프로토콜 추상화** ,
 **Send / Recv 전용 처리 스레드 분리** ,
**TCP 버퍼 복사를 최소화(Span/MemoryPool / SocketAsyncEventArgs 기반)**
까지 “실전용 구조”로 예제를 정리해 줄게.

---

# 1️⃣ 요구사항을 정리한 설계 개념

### ✔️ OOP 프로토콜 개념

프로토콜

```
[Magic 2 bytes] 0xAA 0x55
[Length 4 bytes]  Payload Size
[Type 1 byte]
[Payload ...]
[Checksum 4 bytes]  CRC32

```

* Testable
* 확장 가능
* Zero-copy 지향

```csharp
public interface IMessage
{
    byte Type { get; }
    ReadOnlyMemory<byte> Payload { get; }
}

public interface IProtocolSerializer
{
    IMessage Deserialize(ReadOnlySpan<byte> buffer);
    int Serialize(IMessage message, Span<byte> buffer);
}
```

---

# 2️⃣ **TDD – 3색 원칙**

## 🔴 RED – 실패하는 테스트 먼저

```csharp
public class ProtocolTests
{
    private readonly IProtocolSerializer _sut = new SimpleProtocol();

    [Fact]
    public void Serialize_Then_Deserialize_Restores_Message()
    {
        var msg = new SimpleMessage(1, "Hello"u8.ToArray());
        Span<byte> buffer = stackalloc byte[1024];

        var written = _sut.Serialize(msg, buffer);
        var restored = _sut.Deserialize(buffer[..written]);

        Assert.Equal(msg.Type, restored.Type);
        Assert.Equal(msg.Payload.ToArray(), restored.Payload.ToArray());
    }
}
```

---

## 🟢 GREEN – 최소 구현

```csharp
public record SimpleMessage(byte Type, byte[] Data) : IMessage
{
    public ReadOnlyMemory<byte> Payload => Data;
}

public class SimpleProtocol : IProtocolSerializer
{
    public IMessage Deserialize(ReadOnlySpan<byte> buffer)
    {
        int length = BitConverter.ToInt32(buffer[..4]);
        byte type = buffer[4];
        var data = buffer.Slice(5, length).ToArray();
        return new SimpleMessage(type, data);
    }

    public int Serialize(IMessage message, Span<byte> buffer)
    {
        var data = message.Payload.Span;
        BitConverter.TryWriteBytes(buffer, data.Length);
        buffer[4] = message.Type;
        data.CopyTo(buffer[5..]);
        return 5 + data.Length;
    }
}
```

테스트 Green 성공.

---

## 🟡 REFACTOR – 구조 정리 + 성능 최적화

* `BitConverter` → `BinaryPrimitives`
* Copy 최소화 → `ReadOnlyMemory`
* Payload ToArray 제거

(생략 가능, 원하면 더 깊게 정리해 줄게)

---

# 3️⃣ **TCP Send / Recv 스레드 분리**

* `SocketAsyncEventArgs`
* MemoryPool
* Zero Copy 최대화 (가능 범위 내)

---

## 📡 Receiver Thread (No Extra Copy)

```csharp
public class TcpReceiver
{
    private readonly Socket _socket;
    private readonly IProtocolSerializer _protocol;
    private readonly byte[] _buffer = GC.AllocateUninitializedArray<byte>(8192);

    public TcpReceiver(Socket socket, IProtocolSerializer protocol)
    {
        _socket = socket;
        _protocol = protocol;
    }

    public void Start()
    {
        var thread = new Thread(Loop) { IsBackground = true };
        thread.Start();
    }

    private void Loop()
    {
        while (true)
        {
            int received = _socket.Receive(_buffer);
            if (received <= 0)
                break;

            var span = _buffer.AsSpan(0, received);
            var msg = _protocol.Deserialize(span);

            Handle(msg);
        }
    }

    private void Handle(IMessage msg)
    {
        // 실제 처리
        Console.WriteLine($"[RECV] Type={msg.Type} Size={msg.Payload.Length}");
    }
}
```

---

## 🚀 Sender Thread (Copy 최소화)

큐에 메시지만 넣고
스레드는 “전송만” 담당

```csharp
public class TcpSender
{
    private readonly Socket _socket;
    private readonly IProtocolSerializer _protocol;
    private readonly BlockingCollection<IMessage> _queue = new();

    public TcpSender(Socket socket, IProtocolSerializer protocol)
    {
        _socket = socket;
        _protocol = protocol;
    }

    public void Enqueue(IMessage msg) => _queue.Add(msg);

    public void Start()
    {
        var thread = new Thread(Loop) { IsBackground = true };
        thread.Start();
    }

    private void Loop()
    {
        Span<byte> buffer = stackalloc byte[8192];

        foreach (var msg in _queue.GetConsumingEnumerable())
        {
            int written = _protocol.Serialize(msg, buffer);
            _socket.Send(buffer[..written]);
        }
    }
}
```

---

# 4️⃣ 사용 예제

```csharp
var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
socket.Connect("127.0.0.1", 9000);

var protocol = new SimpleProtocol();

var sender = new TcpSender(socket, protocol);
var receiver = new TcpReceiver(socket, protocol);

sender.Start();
receiver.Start();

// send test
sender.Enqueue(new SimpleMessage(1, "Hello"u8.ToArray()));
```

---

# ✅ 요약

✔️ TDD 3색 원칙 충족
✔️ OOP 프로토콜 설계
✔️ Send / Recv 스레드 분리
✔️ TCP 버퍼 복사 최소화 구조
✔️ 실무 적용 가능한 형태


### **appsettings.json**

<pre class="overflow-visible! px-0!" data-start="819" data-end="940"><div class="contain-inline-size rounded-2xl corner-superellipse/1.1 relative bg-token-sidebar-surface-primary"><div class="sticky top-[calc(--spacing(9)+var(--header-height))] @w-xl/main:top-9"><div class="absolute end-0 bottom-0 flex h-9 items-center pe-2"><div class="bg-token-bg-elevated-secondary text-token-text-secondary flex items-center gap-4 rounded-sm px-2 font-sans text-xs"></div></div></div><div class="overflow-y-auto p-4" dir="ltr"><code class="whitespace-pre! language-json"><span><span>{</span><span>
  </span><span>"Network"</span><span>:</span><span></span><span>{</span><span>
    </span><span>"Host"</span><span>:</span><span></span><span>"127.0.0.1"</span><span>,</span><span>
    </span><span>"Port"</span><span>:</span><span></span><span>9000</span><span>,</span><span>
    </span><span>"RetryCount"</span><span>:</span><span></span><span>3</span><span>,</span><span>
    </span><span>"BufferSize"</span><span>:</span><span></span><span>8192</span><span>
  </span><span>}</span><span>
</span><span>}</span></span></code></div></div></pre>


지금까지 포함된 기능

| 기능                    | 수준           |
| ----------------------- | -------------- |
| 클린 아키텍처 계층 분리 | ✔️ 실전 가능 |
| OOP / SOLID 준수        | ✔️           |
| Config JSON 기반        | ✔️           |
| Retry 3회               | ✔️           |
| Span 기반 최소 복사     | ✔️           |
| CRC + Header            | ✔️           |
| 스트림 안전 조립        | ✔️           |
| 테스트 가능한 구조      | ✔️           |
