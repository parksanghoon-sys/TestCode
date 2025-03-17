using chainResultPattern.Processors;
using chainResultPattern.Requests.Handlers;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // 실제 애플리케이션에서는 의존성 주입을 사용
        // 이것은 간단한 예시입니다
        var logger = new ConsoleLogger();
        var serviceProvider = ConfigureServices(logger);

        // RequestProcessor 및 RequestChainProcessor 모두 생성
        var processor = new RequestProcessor(serviceProvider, logger);
        var chainProcessor = new RequestChainProcessor(serviceProvider, logger);

        // 취소 토큰 생성 (필요한 경우 요청 취소를 위해)
        using var cts = new CancellationTokenSource();

        // 1. RequestProcessor를 사용한 개별 요청 처리
        logger.Log("\n==== RequestProcessor를 사용한 요청 처리 시작 ====");

        // FirstRequest 처리 후 성공 시 SecondRequest 처리하는 체인
        var firstResult = await processor.ProcessRequestsAsync<FirstRequest, FirstResponse>(
            new FirstRequest { Parameter = "테스트 데이터" },
            result => {
                // 첫 번째 요청이 성공하면 두 번째 요청 생성
                logger.Log($"첫 번째 요청 성공, 결과: {result.Data.Result}");
                return new SecondRequest { Parameter = 100 } as IRequest<object>;
            },
            cts.Token);

        logger.Log($"RequestProcessor 연쇄 처리 결과: {(firstResult ? "성공" : "실패")}");

        // 2. RequestChainProcessor를 사용한 요청 체인 처리
        // 요청 체인 생성
        var requests = new List<object>
        {
            new FirstRequest { Parameter = "체인 데이터" },
            new SecondRequest { Parameter = 42 },
            new ThirdRequest { Parameter = Guid.NewGuid() }
        };

        // 시퀀셜 방식으로 요청 체인 실행
        logger.Log("\n==== RequestChainProcessor로 시퀀셜 요청 처리 시작 ====");
        var sequentialResult = await chainProcessor.ExecuteRequestChainAsync(requests, cts.Token);
        logger.Log($"시퀀셜 요청 처리 결과: {(sequentialResult ? "성공" : "실패")}");

        // 병렬 방식으로 요청 실행
        logger.Log("\n==== RequestChainProcessor로 병렬 요청 처리 시작 ====");
        var parallelResult = await chainProcessor.ExecuteRequestsInParallelAsync(requests, 2, cts.Token);
        logger.Log($"병렬 요청 처리 결과: {(parallelResult ? "성공" : "실패")}");

        logger.Log("\n처리 완료.");
    }
    private static IServiceProvider ConfigureServices(ILogger logger)
    {
        // 실제 애플리케이션에서는 DI 컨테이너 설정
        // 이것은 목 구현입니다
        return new MockServiceProvider(logger);
    }
}

// 목 서비스 프로바이더 구현
public class MockServiceProvider : IServiceProvider
{
    private readonly ILogger _logger;
    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public MockServiceProvider(ILogger logger)
    {
        _logger = logger;

        // 핸들러 등록
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        _services[typeof(IRequestHandler<FirstRequest, FirstResponse>)] = new FirstRequestHandler(_logger);
        _services[typeof(IRequestHandler<SecondRequest, SecondResponse>)] = new SecondRequestHandler(_logger);
        _services[typeof(IRequestHandler<ThirdRequest, ThirdResponse>)] = new ThirdRequestHandler(_logger);
    }

    public object GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var service))
        {
            return service;
        }

        return null;
    }
}
