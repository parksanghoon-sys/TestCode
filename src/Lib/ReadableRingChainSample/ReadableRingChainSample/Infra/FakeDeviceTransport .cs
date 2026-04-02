using ReadableRingChainSample.Abstractions;
using ReadableRingChainSample.Core;
using ReadableRingChainSample.Domain;
using System.Collections.Concurrent;

namespace ReadableRingChainSample.Infra;

public sealed class FakeDeviceTransport : ICommandTransport<DeviceCommand, DeviceResponse>
{
    private readonly ConcurrentQueue<DeviceResponse> _responses = new();
    public FakeDeviceTransport()
    {
        _responses.Enqueue(new DeviceResponse("HELLO_ACK", "WELCOME", true));
        _responses.Enqueue(new DeviceResponse("AUTH_ACK", "TOKEN:ABC123", true));
        _responses.Enqueue(new DeviceResponse("DATA_ACK", "VALUE=42", true));
    }
    public Task<Result<DeviceResponse>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_responses.Count == 0)
        {
            return Task.FromResult(
                Result<DeviceResponse>.Failure("NO_RESPONSE", "No queued response."));
        }

        _responses.TryDequeue(out var response);
        Console.WriteLine($"[RECV] {response!.Code} / {response.Payload} / ok={response.IsOk}");

        return Task.FromResult(Result.Success(response));
    }

    public Task<Result> SendAsync(DeviceCommand command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[SEND] {command.Code} / {command.Payload}");
        return Task.FromResult(Result.Success());
    }
}
