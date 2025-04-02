using MessageBusLib;

/// <summary>
/// 채팅 메시지 모델
/// </summary>
[Serializable]
public class ChatMessage
{
    /// <summary>
    /// 발신자 이름
    /// </summary>
    public string SenderName { get; set; }

    /// <summary>
    /// 메시지 내용
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 전송 시간
    /// </summary>
    public DateTime Timestamp { get; set; }

    public ChatMessage(string senderName, string content)
    {
        SenderName = senderName;
        Content = content;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] {SenderName}: {Content}";
    }
}

/// <summary>
/// 콘솔 메신저 애플리케이션
/// </summary>
public class ConsoleMessenger : IDisposable
{
    private readonly IMessageBus _messageBus;
    private readonly string _userName;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly string _chatTopic = "console.chat2";
    private bool _isRunning = true;

    /// <summary>
    /// 콘솔 메신저 초기화
    /// </summary>
    /// <param name="userName">사용자 이름</param>
    public ConsoleMessenger(string userName)
    {
        _userName = string.IsNullOrWhiteSpace(userName) ? "Anonymous" : userName;

        // 메시지 버스 생성
        var factory = new MessagingFactory();
        _messageBus = factory.CreateMessageBus("console-messenger");

        // 채팅 메시지 구독
        _messageBus.Subscribe(_chatTopic, OnChatMessageReceived);

        // 시스템 메시지 발송 (입장)
        SendSystemMessage($"{_userName}님이 입장했습니다.");
    }

    /// <summary>
    /// 메시지 수신 처리
    /// </summary>
    private void OnChatMessageReceived(object sender, IMessageReceivedEventArgs args)
    {
        var message = args.Message.GetData<ChatMessage>();

        // 자신이 보낸 메시지는 표시하지 않음
        if (message.SenderName == _userName)
            return;

        // 다른 사람의 메시지 표시
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// 채팅 메시지 전송
    /// </summary>
    private void SendChatMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        var message = new ChatMessage(_userName, content);
        _messageBus.Publish(_chatTopic, message);

        // 자신의 메시지 표시 (녹색)
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// 시스템 메시지 전송
    /// </summary>
    private void SendSystemMessage(string content)
    {
        var message = new ChatMessage("System", content);
        _messageBus.Publish(_chatTopic, message);
    }

    /// <summary>
    /// 입력 처리 루프 실행
    /// </summary>
    public void Run()
    {
        Console.WriteLine("콘솔 메신저가 시작되었습니다. 채팅을 입력하세요. 종료하려면 'exit'를 입력하세요.");
        Console.WriteLine($"사용자 이름: {_userName}");
        Console.WriteLine();

        // 입력 처리 루프
        Task.Run(() => InputLoop(), _cts.Token);

        // 프로그램 종료 대기
        Console.WriteLine("아무 키나 누르면 프로그램이 종료됩니다...");
        Console.ReadKey(true);
        _isRunning = false;
    }

    /// <summary>
    /// 입력 처리 루프
    /// </summary>
    private void InputLoop()
    {
        while (_isRunning)
        {
            string input = Console.ReadLine();

            if (input?.ToLower() == "exit")
            {
                // 퇴장 메시지 전송
                SendSystemMessage($"{_userName}님이 퇴장했습니다.");
                _isRunning = false;
                break;
            }

            // 채팅 메시지 전송
            SendChatMessage(input);
        }
    }

    /// <summary>
    /// 자원 해제
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        _messageBus.Dispose();
        _cts.Dispose();
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("콘솔 메신저 애플리케이션");
        Console.Write("사용자 이름을 입력하세요: ");
        string userName = Console.ReadLine();

        using (var messenger = new ConsoleMessenger(userName))
        {
            messenger.Run();
        }

        Console.WriteLine("프로그램이 종료되었습니다.");
    }
}