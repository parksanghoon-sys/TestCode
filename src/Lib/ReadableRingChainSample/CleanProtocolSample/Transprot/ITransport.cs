using CleanProtocolSample.Protocol;

namespace CleanProtocolSample.Transprot;
/// <summary>
/// 실제 통신 transport.
/// TCP/UDP/Serial 등으로 확장 가능.
/// </summary>
internal interface ITransport
{
    Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken);
    Task<ProtocolMessage> ReceiveAsync(CancellationToken cancellationToken);
}
