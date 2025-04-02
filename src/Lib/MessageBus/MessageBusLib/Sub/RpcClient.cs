using MessageBusLib.Exceptions;
using MessageBusLib.Messages;
using MessageBusLib.Pub;
using MessageBusLib.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MessageBusLib.Sub;

/// <summary>
/// 메시지 버스를 이용한 RPC(원격 프로시저 호출) 클라이언트
/// </summary>
public class RpcClient : IRpcClient
{
    private readonly IMessageBus _messageBus;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<IMessage>> _pendingRequests;
    private readonly string _replyTopic;
    private readonly RpcClientOptions _options;
    private readonly ISerializer _serializer;
    private bool _disposed = false;

    /// <summary>
    /// RPC 클라이언트 초기화
    /// </summary>
    /// <param name="messageBus">사용할 메시지 버스</param>
    /// <param name="options">RPC 클라이언트 옵션</param>
    /// <param name="serializer">직렬화 도구</param>
    public RpcClient(IMessageBus messageBus, RpcClientOptions options = null, ISerializer serializer = null)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _options = options ?? new RpcClientOptions();
        _serializer = serializer ?? new JsonSerializer();
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>();
        _replyTopic = $"rpc_reply_{Process.GetCurrentProcess().Id}_{Guid.NewGuid()}";

        // 응답 토픽 구독
        _messageBus.Subscribe(_replyTopic, HandleReply);
    }

    /// <summary>
    /// RPC 요청 전송 및 응답 대기
    /// </summary>
    /// <typeparam name="TResult">응답 데이터 타입</typeparam>
    /// <param name="serviceTopic">서비스 토픽</param>
    /// <param name="data">요청 데이터</param>
    /// <returns>응답 데이터</returns>
    public async Task<TResult> CallAsync<TResult>(string serviceTopic, object data)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RpcClient));

        if (string.IsNullOrEmpty(serviceTopic))
            throw new ArgumentNullException(nameof(serviceTopic));

        // 요청 ID 생성
        string requestId = Guid.NewGuid().ToString();

        // 응답을 대기하기 위한 TaskCompletionSource 생성
        var tcs = new TaskCompletionSource<IMessage>();
        _pendingRequests[requestId] = tcs;

        try
        {
            // 요청 데이터 직렬화
            byte[] serializedData = data != null ? _serializer.SerializeWithType(data) : null;

            // 요청 타입 정보
            string requestTypeName = data?.GetType().AssemblyQualifiedName;

            // RPC 요청 생성
            var request = new RpcRequest
            {
                Data = serializedData,
                ReplyTopic = _replyTopic,
                RequestTypeName = requestTypeName
            };

            // 메시지 발행
            _messageBus.PublishWithId(requestId, serviceTopic, request);

            // 타임아웃과 함께 응답 대기
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(_options.Timeout));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException($"RPC 요청 {requestId}에 대한 응답 시간 초과");
            }

            // 응답 처리
            var response = await tcs.Task;

            // RPC 응답 확인
            var rpcResponse = response.GetData<RpcResponse>();

            if (rpcResponse != null)
            {
                if (!rpcResponse.Success)
                {
                    throw new RpcException($"RPC 서버 오류: {rpcResponse.ErrorMessage}");
                }

                // 응답 타입이 지정된 경우
                if (!string.IsNullOrEmpty(rpcResponse.ResponseTypeName))
                {
                    // 타입 정보로 역직렬화 시도
                    var result = _serializer.DeserializeWithType(rpcResponse.Data);

                    if (result is TResult typedResult)
                    {
                        return typedResult;
                    }
                }

                // 직접 TResult 타입으로 역직렬화
                return _serializer.Deserialize<TResult>(rpcResponse.Data);
            }
            else
            {
                // 구버전 서버는 RpcResponse를 사용하지 않을 수 있으므로 직접 역직렬화 시도
                return response.GetData<TResult>();
            }
        }
        finally
        {
            // 요청 제거
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// 응답 메시지 처리
    /// </summary>
    private void HandleReply(object sender, IMessageReceivedEventArgs args)
    {
        var message = args.Message;

        // 해당하는 요청 찾기
        if (_pendingRequests.TryGetValue(message.MessageId, out var tcs))
        {
            // 응답 완료 알림
            tcs.TrySetResult(message);
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
                // 응답 토픽 구독 해제
                _messageBus.Unsubscribe(_replyTopic);

                // 대기 중인 모든 요청 취소
                foreach (var request in _pendingRequests)
                {
                    request.Value.TrySetCanceled();
                }
            }

            _disposed = true;
        }
    }

    ~RpcClient()
    {
        Dispose(false);
    }
}
