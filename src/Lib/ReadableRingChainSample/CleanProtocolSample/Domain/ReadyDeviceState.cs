namespace CleanProtocolSample.Domain;


/// <summary>
/// 장비 상태.
/// </summary>
internal sealed record ReadyDeviceState(string DeviceId, bool Handshaked, bool Authenticated, bool Ready, string? Token, string? Data)
{
    public static ReadyDeviceState Create(string deviceId) =>
        new(deviceId, false, false, false, null, null);
}
