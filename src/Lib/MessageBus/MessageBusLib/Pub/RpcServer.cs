using MessageBusLib.Serialization;

namespace MessageBusLib.Pub;

/// <summary>
/// 메시지 버스를 이용한 RPC(원격 프로시저 호출) 서버
/// </summary>
public class RpcServer : IRpcServer
{
    private readonly IMessageBus _messageBus;
    private readonly string _serviceTopic;
    private readonly Func<object, Task<object>> _handler;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ISerializer _serializer;
    private bool _disposed = false;

    /// <summary>
    /// 서비스 토픽
    /// </summary>
    public string ServiceTopic => _serviceTopic;

    /// <summary>
    /// RPC 서버 초기화
    /// </summary>
    /// <param name="messageBus">사용할 메시지 버스</param>
    /// <param name="serviceTopic">서비스 토픽</param>
    /// <param name="handler">요청 처리 핸들러</param>
    /// <param name="serializer">직렬화 도구</param>
    public RpcServer(IMessageBus messageBus, string serviceTopic, Func<object, Task<object>> handler, ISerializer serializer = null)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _serviceTopic = serviceTopic ?? throw new ArgumentNullException(nameof(serviceTopic));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _serializer = serializer ?? new JsonSerializer();
        _cancellationTokenSource = new CancellationTokenSource();

        // 서비스 토픽 구독
        _messageBus.Subscribe(_serviceTopic, HandleRequest);
    }

    /// <summary>
    /// 요청 메시지 처리
    /// </summary>
    private async void HandleRequest(object sender, IMessageReceivedEventArgs args)
    {
        if (_disposed)
            return;

        var message = args.Message;

        try
        {
            // RPC 요청 파싱
            var request = message.GetData<RpcRequest>();

            if (request == null || string.IsNullOrEmpty(request.ReplyTopic))
            {
                Console.Error.WriteLine("잘못된 RPC 요청 형식");
                return;
            }

            // 요청 데이터 준비
            object requestData = null;

            if (request.Data != null)
            {
                try
                {
                    // 타입 정보가 있는 경우
                    if (!string.IsNullOrEmpty(request.RequestTypeName))
                    {
                        Type requestType = Type.GetType(request.RequestTypeName);
                        if (requestType != null)
                        {
                            requestData = _serializer.DeserializeWithType(request.Data);
                        }
                    }

                    // 타입 정보가 없거나 실패한 경우 그냥 바이트 배열로 전달
                    if (requestData == null)
                    {
                        requestData = request.Data;
                    }
                }
                catch
                {
                    // 역직렬화 실패 시 바이트 배열 그대로 사용
                    requestData = request.Data;
                }
            }

            // 응답 객체 준비
            RpcResponse response = new RpcResponse();

            try
            {
                // 요청 처리
                object result = await _handler(requestData);

                // 응답 데이터 직렬화
                if (result != null)
                {
                    response.Data = _serializer.SerializeWithType(result);
                    response.ResponseTypeName = result.GetType().AssemblyQualifiedName;
                }
            }
            catch (Exception ex)
            {
                // 요청 처리 중 오류 발생
                response.ErrorMessage = ex.Message;
            }

            // 응답 전송
            _messageBus.PublishWithId(message.MessageId, request.ReplyTopic, response);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"RPC 요청 처리 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 자원 해제
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 서비스 토픽 구독 해제
                _messageBus.Unsubscribe(_serviceTopic);
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }

    ~RpcServer()
    {
        Dispose(false);
    }
}
