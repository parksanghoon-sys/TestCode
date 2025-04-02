
using MessageBusLib;
using MessageBusLib.Pub;

namespace MessageBusTest
{
    #region Domain Models

    /// <summary>
    /// 주식 정보 데이터 모델
    /// </summary>
    [Serializable]
    public class StockUpdate
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }

        public StockUpdate(string symbol, decimal price, decimal change)
        {
            Symbol = symbol;
            Price = price;
            Change = change;
            ChangePercent = price > 0 ? Math.Round(change / (price - change) * 100, 2) : 0;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            string changeStr = Change >= 0
                ? $"+{Change:F2} (+{ChangePercent:F2}%)"
                : $"{Change:F2} ({ChangePercent:F2}%)";

            return $"{Symbol}: {Price:C} {changeStr} @ {Timestamp:HH:mm:ss.fff}";
        }
    }

    /// <summary>
    /// 분석 결과 모델
    /// </summary>
    [Serializable]
    public class AnalysisResult
    {
        public string Symbol { get; set; }
        public string Analysis { get; set; }
        public DateTime Timestamp { get; set; }

        public AnalysisResult(string symbol, string analysis)
        {
            Symbol = symbol;
            Analysis = analysis;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 알림 메시지 모델
    /// </summary>
    [Serializable]
    public class AlertMessage
    {
        public string Symbol { get; set; }
        public AlertType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public AlertMessage(string symbol, AlertType type, string message)
        {
            Symbol = symbol;
            Type = type;
            Message = message;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Type}] {Symbol}: {Message}";
        }
    }

    /// <summary>
    /// 알림 타입 열거형
    /// </summary>
    public enum AlertType
    {
        Info,
        Warning,
        Critical
    }

    #endregion

    #region Application Services

    /// <summary>
    /// 주식 데이터 발행자 서비스
    /// </summary>
    public class StockPublisherService : IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly CancellationTokenSource _cts;
        private readonly Task _publishingTask;
        private readonly Random _random = new Random();
        private readonly Dictionary<string, decimal> _stockPrices;

        /// <summary>
        /// 구독자 수 이벤트
        /// </summary>
        public event EventHandler<int> SubscriberCountChanged;

        public StockPublisherService(IMessageBus messageBus)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _cts = new CancellationTokenSource();

            // 초기 주식 가격
            _stockPrices = new Dictionary<string, decimal>
            {
                { "AAPL", 150m },
                { "MSFT", 280m },
                { "GOOGL", 120m },
                { "AMZN", 130m },
                { "TSLA", 250m }
            };

            // 발행 태스크 시작
            _publishingTask = Task.Run(PublishStockData);

            // 구독자 수 모니터링
            messageBus.Subscribe("system.subscribe", OnSubscriberChanged);
            messageBus.Subscribe("system.unsubscribe", OnSubscriberChanged);
        }

        private async Task PublishStockData()
        {
            Console.WriteLine("주식 데이터 발행 서비스 시작");
            int subscriberCount = 0;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 각 주식에 대해 업데이트 생성 및 발행
                    foreach (var stock in _stockPrices.Keys)
                    {
                        if (_cts.Token.IsCancellationRequested)
                            break;

                        // 가격 변동 (-2% ~ +2%)
                        decimal currentPrice = _stockPrices[stock];
                        decimal change = currentPrice * ((decimal)_random.NextDouble() * 0.04m - 0.02m);
                        change = Math.Round(change, 2);
                        decimal newPrice = Math.Max(0.01m, Math.Round(currentPrice + change, 2));
                        _stockPrices[stock] = newPrice;

                        // 주식 업데이트 생성
                        var update = new StockUpdate(stock, newPrice, change);

                        // 특정 주식 토픽으로 발행
                        _messageBus.Publish($"stock.{stock}", update);

                        // 모든 주식 토픽으로도 발행
                        _messageBus.Publish("stock.all", update);

                        Console.WriteLine($"발행: {update} (구독자: {subscriberCount})");

                        // 가격 변동이 크면 알림 발행
                        PublishAlertIfNeeded(update);

                        await Task.Delay(500, _cts.Token);
                    }

                    await Task.Delay(1000, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"주식 데이터 발행 오류: {ex.Message}");
                    await Task.Delay(1000, _cts.Token);
                }
            }

            Console.WriteLine("주식 데이터 발행 서비스 종료");
        }

        private void PublishAlertIfNeeded(StockUpdate update)
        {
            // 가격 변동이 크면 알림 발행
            if (Math.Abs(update.ChangePercent) >= 1.5m)
            {
                AlertType alertType = update.ChangePercent >= 0 ? AlertType.Info : AlertType.Warning;

                if (Math.Abs(update.ChangePercent) >= 2.0m)
                {
                    alertType = update.ChangePercent >= 0 ? AlertType.Warning : AlertType.Critical;
                }

                string message = update.ChangePercent >= 0
                    ? $"큰 상승: {update.ChangePercent:F2}% (${update.Price:F2})"
                    : $"큰 하락: {update.ChangePercent:F2}% (${update.Price:F2})";

                var alert = new AlertMessage(update.Symbol, alertType, message);

                _messageBus.Publish("alert", alert);
            }
        }

        private void OnSubscriberChanged(object sender, IMessageReceivedEventArgs args)
        {
            var message = args.Message;
            int count = message.GetData<int>();

            SubscriberCountChanged?.Invoke(this, count);
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _publishingTask.Wait(1000);
            }
            catch { }

            _messageBus.Unsubscribe("system.subscribe");
            _messageBus.Unsubscribe("system.unsubscribe");
            _cts.Dispose();

            Console.WriteLine("주식 데이터 발행 서비스 종료됨");
        }
    }

    /// <summary>
    /// 주식 데이터 구독자 서비스
    /// </summary>
    public class StockSubscriberService : IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly HashSet<string> _subscribedStocks = new HashSet<string>();
        private int _subscriberCount = 0;

        public StockSubscriberService(IMessageBus messageBus)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

            // 알림 구독
            _messageBus.Subscribe("alert", OnAlertReceived);
        }

        /// <summary>
        /// 특정 주식 구독
        /// </summary>
        public void SubscribeToStock(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentNullException(nameof(symbol));

            if (!_subscribedStocks.Contains(symbol))
            {
                _messageBus.Subscribe($"stock.{symbol}", OnStockUpdate);
                _subscribedStocks.Add(symbol);
                _subscriberCount++;

                // 구독자 수 변경 알림
                _messageBus.Publish("system.subscribe", _subscriberCount);

                Console.WriteLine($"'{symbol}' 주식 데이터 구독 시작");
            }
        }

        /// <summary>
        /// 특정 주식 구독 해제
        /// </summary>
        public void UnsubscribeFromStock(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentNullException(nameof(symbol));

            if (_subscribedStocks.Contains(symbol))
            {
                _messageBus.Unsubscribe($"stock.{symbol}");
                _subscribedStocks.Remove(symbol);
                _subscriberCount--;

                // 구독자 수 변경 알림
                _messageBus.Publish("system.unsubscribe", _subscriberCount);

                Console.WriteLine($"'{symbol}' 주식 데이터 구독 해제");
            }
        }

        /// <summary>
        /// 모든 주식 구독
        /// </summary>
        public void SubscribeToAllStocks()
        {
            _messageBus.Subscribe("stock.all", OnAllStocksUpdate);
            Console.WriteLine("모든 주식 데이터 구독 시작");
        }

        private void OnStockUpdate(object sender, IMessageReceivedEventArgs args)
        {
            var update = args.Message.GetData<StockUpdate>();

            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = update.Change >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"[구독] {update}");
            Console.ForegroundColor = originalColor;

            // 분석 서비스 (RPC)에 분석 요청
            RequestAnalysis(update);
        }

        private void OnAllStocksUpdate(object sender, IMessageReceivedEventArgs args)
        {
            // 개별 구독으로 처리하므로 여기서는 아무것도 하지 않음
        }

        private void OnAlertReceived(object sender, IMessageReceivedEventArgs args)
        {
            var alert = args.Message.GetData<AlertMessage>();

            // 구독 중인 주식에 대한 알림만 처리
            if (_subscribedStocks.Contains(alert.Symbol))
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                switch (alert.Type)
                {
                    case AlertType.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case AlertType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case AlertType.Critical:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                Console.WriteLine($"[알림] {alert}");
                Console.ForegroundColor = originalColor;
            }
        }

        private async void RequestAnalysis(StockUpdate update)
        {
            try
            {
                // RPC 클라이언트 생성
                using (var rpcClient = new MessagingFactory().CreateRpcClient(_messageBus))
                {
                    // 분석 요청
                    var result = await rpcClient.CallAsync<AnalysisResult>("analysis.request", update);

                    if (result != null)
                    {
                        Console.WriteLine($"[분석] {result.Symbol}: {result.Analysis}");
                    }
                }
            }
            catch (TimeoutException)
            {
                // 분석 서비스가 응답하지 않는 경우 무시
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"분석 요청 오류: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // 모든 구독 해제
            _messageBus.Unsubscribe("alert");
            _messageBus.Unsubscribe("stock.all");

            foreach (var symbol in _subscribedStocks.ToArray())
            {
                UnsubscribeFromStock(symbol);
            }

            Console.WriteLine("주식 데이터 구독자 서비스 종료됨");
        }
    }

    /// <summary>
    /// 주식 분석 서비스 (RPC 서버)
    /// </summary>
    public class StockAnalysisService : IDisposable
    {
        private readonly IRpcServer _rpcServer;
        private readonly Random _random = new Random();

        private readonly Dictionary<string, List<string>> _analysisTemplates = new Dictionary<string, List<string>>
        {
            { "AAPL", new List<string> {
                "기술적 지표는 단기 상승세를 보여줍니다.",
                "최근 실적 발표 후 투자자 신뢰도가 상승 중입니다.",
                "신제품 출시로 인한 실적 개선이 예상됩니다.",
                "현재 모멘텀은 강세를 유지하고 있습니다."
            }},
            { "MSFT", new List<string> {
                "클라우드 사업 성장이, 주가 상승을 이끌 것으로 예상됩니다.",
                "기업용 소프트웨어 부문에서 안정적인 성장세가 지속되고 있습니다.",
                "AI 관련 투자가 장기적으로 유리한 위치를 확보할 것으로 보입니다.",
                "전반적인 시장 상황을, 상대적으로 잘 견디고 있습니다."
            }},
            { "GOOGL", new List<string> {
                "광고 매출이 전분기 대비 개선되고 있습니다.",
                "클라우드 부문의 성장이 가속화되고 있습니다.",
                "규제 위험에도 불구하고 핵심 사업은 안정적입니다.",
                "다양한 사업 포트폴리오가 리스크를 분산시키고 있습니다."
            }},
            { "AMZN", new List<string> {
                "전자상거래 사업의 마진이 개선 중입니다.",
                "AWS의 성장이 전체 실적을 견인하고 있습니다.",
                "물류 효율화로 인한 비용 절감이 기대됩니다.",
                "구독 서비스 가입자 증가세가 지속되고 있습니다."
            }},
            { "TSLA", new List<string> {
                "생산량 증가로 규모의 경제가 실현되고 있습니다.",
                "중국 시장에서의 실적이 중요한 변수로 작용 중입니다.",
                "배터리 기술 발전이 장기적인 경쟁력을 강화할 것으로 보입니다.",
                "자율주행 기술의 발전 속도가 주가에 영향을 미칠 수 있습니다."
            }}
        };

        public StockAnalysisService(IMessageBus messageBus)
        {
            // RPC 서버 생성
            _rpcServer = new MessagingFactory().CreateRpcServer(
                messageBus,
                "analysis.request",
                HandleAnalysisRequest);

            Console.WriteLine("주식 분석 서비스 시작");
        }

        private async Task<object> HandleAnalysisRequest(object request)
        {
            // 분석 요청 처리 (임의 지연으로 처리 시간 시뮬레이션)
            await Task.Delay(_random.Next(300, 700));

            if (request is StockUpdate update)
            {
                string analysis;

                // 저장된 분석 템플릿에서 랜덤하게 선택
                if (_analysisTemplates.TryGetValue(update.Symbol, out var templates))
                {
                    analysis = templates[_random.Next(templates.Count)];
                }
                else
                {
                    analysis = "해당 종목에 대한 충분한 데이터가 없습니다.";
                }

                // 가격 변동에 따른 추가 분석
                if (update.ChangePercent >= 1.5m)
                {
                    analysis += " 단기 과매수 구간에 진입할 수 있습니다.";
                }
                else if (update.ChangePercent <= -1.5m)
                {
                    analysis += " 기술적 지지선 근처에서 반등을 기대할 수 있습니다.";
                }

                return new AnalysisResult(update.Symbol, analysis);
            }

            throw new ArgumentException("지원되지 않는 요청 형식");
        }

        public void Dispose()
        {
            _rpcServer.Dispose();
            Console.WriteLine("주식 분석 서비스 종료됨");
        }
    }

    #endregion

    /// <summary>
    /// 주식 거래 메시징 애플리케이션
    /// </summary>
    public class StockMessagingApplication
    {
        private readonly IMessageBus _messageBus;
        private StockPublisherService _publisherService;
        private StockSubscriberService _subscriberService;
        private StockAnalysisService _analysisService;

        public StockMessagingApplication()
        {
            // 메시징 팩토리를 통한 메시지 버스 생성
            _messageBus = new MessagingFactory().CreateMessageBus("stock-market");
        }

        public void Start()
        {
            Console.WriteLine("주식 메시징 애플리케이션 시작 중...");

            // 분석 서비스 시작
            _analysisService = new StockAnalysisService(_messageBus);

            // 구독자 서비스 시작
            _subscriberService = new StockSubscriberService(_messageBus);

            // 구독 설정
            _subscriberService.SubscribeToStock("AAPL");
            _subscriberService.SubscribeToStock("TSLA");
            _subscriberService.SubscribeToAllStocks();

            // 발행자 서비스 시작
            _publisherService = new StockPublisherService(_messageBus);

            Console.WriteLine("애플리케이션이 실행 중입니다. 종료하려면 아무 키나 누르세요.");
            Console.WriteLine("현재 구독 중: AAPL, TSLA");
            Console.WriteLine();

            // 키 입력 대기
            Console.ReadKey(true);
        }

        public void Stop()
        {
            Console.WriteLine("애플리케이션 종료 중...");

            // 서비스 종료
            _publisherService?.Dispose();
            _subscriberService?.Dispose();
            _analysisService?.Dispose();
            _messageBus?.Dispose();

            Console.WriteLine("애플리케이션이 종료되었습니다.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("LightweightMessenger 테스트 애플리케이션");

            var app = new StockMessagingApplication();

            try
            {
                app.Start();
            }
            finally
            {
                app.Stop();
            }
        }
    }
}