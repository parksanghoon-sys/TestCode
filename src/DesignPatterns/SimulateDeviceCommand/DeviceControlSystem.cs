using SimulateDeviceCommand.Interfaces;
using SimulateDeviceCommand.Models;
using SimulateDeviceCommand.Services;
// =============================================
// 사용 예제
// =============================================
public class DeviceControlSystem
{
    private readonly ICommandSequenceExecutor _executor;
    private readonly IList<IDeviceCommand> _commands;
    private CancellationTokenSource _cancellationTokenSource;
    public DeviceControlSystem()
    {
        var communicator = new MockDeviceCommunicator();
        _executor = new CommandSequenceExecutor(communicator);
        _executor.ProgressChanged += OnProgressChanged;

        // 바이너리 프로토콜 명령 시퀀스 설정
        _commands = new List<IDeviceCommand>
        {
            new ConnectCommand(0x01),                              // 장비 ID 1에 연결
            new ConfigureCommand(1000, 16, 0x01),                 // 1000Hz, 16bit, 연속 모드
            new StartMeasurementCommand(0x10, 30),                // 온도측정, 30초
            new GetResultCommand(0x01),                           // 결과 조회
            new DisconnectCommand()                               // 연결 해제
        };
    }
    public void OnCancelButtonClick()
    {
        _cancellationTokenSource?.Cancel();
        Console.WriteLine("🛑 사용자가 시퀀스 실행을 취소했습니다.");
    }

    public async Task OnExecuteButtonClickAsync()
    
    {
        if(_cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested == false)
        {
            Console.WriteLine("시퀀스가 이미 취소되었습니다. 다시 시도하세요.");
            return;
        }
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            Console.WriteLine("🎯 바이너리 프로토콜 장비 제어 시스템 시작");
            Console.WriteLine();
            var success = await _executor.ExecuteSequenceAsync(_commands, _cancellationTokenSource.Token);
            Console.WriteLine();
            if (success)
            {
                Console.WriteLine("✨ 전체 시퀀스가 성공적으로 완료되었습니다!");
            }
            else
            {
                Console.WriteLine("💥 시퀀스 실행 중 오류가 발생했습니다.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🚨 예외 발생: {ex.Message}");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

    }

    private void OnProgressChanged(CommandProgress progress)
    {
        // UI 업데이트는 여기서 처리 (실제로는 Dispatcher 사용)
        if (progress.RetryCount > 0)
        {
            Console.WriteLine($"🔄 재시도 중: {progress.CommandName} ({progress.RetryCount}회)");
        }
    }
}