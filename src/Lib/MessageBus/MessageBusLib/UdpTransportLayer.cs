using System.Net.Sockets;
using System.Net;
using MessageBusLib.Exceptions;

namespace MessageBusLib;

/// <summary>
/// UDP 기반 전송 계층 구현
/// </summary>
public class UdpTransportLayer : TransportLayerBase
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _multicastEndpoint;
    private readonly UdpTransportOptions _options;
    private Task _receiveTask;

    /// <summary>
    /// UDP 전송 계층 초기화
    /// </summary>
    /// <param name="multicastIp">멀티캐스트 IP 주소</param>
    /// <param name="port">사용할 포트</param>
    public UdpTransportLayer(string multicastIp = "239.0.0.1", int port = 11000)
        : this(new UdpTransportOptions { MulticastIp = multicastIp, Port = port })
    {
    }

    /// <summary>
    /// UDP 전송 계층 초기화 (옵션 지정)
    /// </summary>
    /// <param name="options">UDP 구성 옵션</param>
    public UdpTransportLayer(UdpTransportOptions options)
        : base()
    {
        _options = options ?? new UdpTransportOptions();

        try
        {
            // UDP 클라이언트 설정
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _options.Port));

            // TTL 설정
            _udpClient.Ttl = (short)_options.TimeToLive;

            // 멀티캐스트 그룹 가입
            _multicastEndpoint = new IPEndPoint(IPAddress.Parse(_options.MulticastIp), _options.Port);
            _udpClient.JoinMulticastGroup(IPAddress.Parse(_options.MulticastIp));
        }
        catch (Exception ex)
        {
            _udpClient?.Close();
            throw new MessageBusException("UDP 전송 계층 초기화 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 시작 시 호출되는 메서드
    /// </summary>
    protected override void OnStart()
    {
        // 수신 태스크 시작
        _receiveTask = Task.Run(ReceiveLoopAsync);
    }

    /// <summary>
    /// 중지 시 호출되는 메서드
    /// </summary>
    protected override void OnStop()
    {
        // UDP 클라이언트 닫기 (수신 루프 중단)
        try
        {
            _udpClient.Close();
        }
        catch { }
    }

    /// <summary>
    /// 메시지 전송
    /// </summary>
    public override void SendMessage(byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        if (data.Length > _options.MaxMessageSize)
            throw new ArgumentException($"메시지 크기가 최대 허용 크기({_options.MaxMessageSize} 바이트)를 초과합니다.");

        try
        {
            // 멀티캐스트 그룹으로 메시지 전송
            _udpClient.Send(data, data.Length, _multicastEndpoint);
        }
        catch (SocketException ex)
        {
            throw new MessageBusException("UDP 메시지 전송 중 소켓 오류 발생", ex);
        }
        catch (Exception ex)
        {
            throw new MessageBusException("UDP 메시지 전송 중 오류 발생", ex);
        }
    }

    /// <summary>
    /// 메시지 수신 루프
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        while (IsRunning && !CancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                // 비동기적으로 UDP 패킷 수신
                UdpReceiveResult result = await _udpClient.ReceiveAsync().ConfigureAwait(false);

                // 자신이 보낸 메시지 필터링 (선택적)
                // if (result.RemoteEndPoint.Address.Equals(LocalEndPoint.Address) && 
                //     result.RemoteEndPoint.Port == LocalEndPoint.Port)
                //     continue;

                // 메시지 수신 이벤트 발생
                OnMessageReceived(new TransportMessageReceivedEventArgs(result.Buffer, result.RemoteEndPoint));
            }
            catch (ObjectDisposedException)
            {
                // UDP 클라이언트가 닫힌 경우 (정상 종료)
                break;
            }
            catch (SocketException ex)
            {
                // 소켓 오류 처리
                Console.Error.WriteLine($"UDP 수신 오류: {ex.Message}");

                // 연속적인 오류 방지
                await Task.Delay(100).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 작업 취소 (정상 종료)
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UDP 메시지 수신 중 오류: {ex.Message}");

                // 연속적인 오류 방지
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// 자원 해제
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // 멀티캐스트 그룹 탈퇴
                _udpClient.DropMulticastGroup(IPAddress.Parse(_options.MulticastIp));
            }
            catch { }

            // UDP 클라이언트 닫기
            try
            {
                _udpClient.Close();
            }
            catch { }

            // 수신 태스크 완료 대기
            try
            {
                if (_receiveTask != null && !_receiveTask.IsCompleted)
                {
                    Task.WaitAny(new[] { _receiveTask }, 1000);
                }
            }
            catch { }
        }

        base.Dispose(disposing);
    }
}