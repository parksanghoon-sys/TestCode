using CleanProtocolSample.Domain;
using CleanProtocolSample.Logging;
using CleanProtocolSample.Protocol;
using CleanProtocolSample.Transprot;

/// <summary>
/// 프로그램 시작점.
///
/// 실제 테스트 목적:
/// - protocol engine 실행
/// - workflow 실행
/// - request/response 검증
/// - event 수신 검증
/// - queue 동작 검증
/// - 최근 이력 검증
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        /// cancel token
        using var cts = new CancellationTokenSource();

        // logger 생성
        var logger = new ConsoleLogger();
        // send/ receive queue Management
        var queues = new MessageQueueHub(historySize: 32);
        
        // mock transport object 
        var transport = new FakeTransport(logger);

        // session cooperative object
        var session = new SessionContext(queues, logger, transport);

        // correlation id factory
        var ids = new SequentialCorrelationIdGenerator();

        // protocol engine create
        var client = new ProtocolClient(session, ids);

        // protocol engine start
        var startResult = await client.StartAsync(cts.Token);

        if(startResult.IsFailure)
        {
            logger.Error($"Start Fail : {startResult.ErrorCode}");
            return;
        }
        logger.Info("protocol Started");

        // workflow state create
        var state = ReadyDeviceState.Create("DEVICE-001");

        //work runner create
        var runner =
            CreateWorkflow(client, logger);
    }
    private static WorkFl
}