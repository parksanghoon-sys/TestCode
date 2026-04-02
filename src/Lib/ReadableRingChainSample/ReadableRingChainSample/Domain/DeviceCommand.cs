namespace ReadableRingChainSample.Domain;

public sealed record DeviceCommand(string Code, string Payload);

public sealed record DeviceResponse(string Code, string Payload, bool IsOk);

public sealed record DeviceSessionState(
    string DeviceId,
    bool Handshaked,
    bool Authenticated,
    string? Token,
    string? Data,
    IReadOnlyList<string> Logs)
{
    public static DeviceSessionState Create(string deviceId)
        => new DeviceSessionState(
            DeviceId: deviceId,
            Handshaked: false,
            Authenticated: false,
            Token: null,
            Data: null,
            Logs: Array.Empty<string>());
}