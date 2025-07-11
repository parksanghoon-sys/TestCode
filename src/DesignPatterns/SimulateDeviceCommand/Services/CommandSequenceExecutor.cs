using SimulateDeviceCommand.Enums;
using SimulateDeviceCommand.Interfaces;
using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Services;

// =============================================
// Chain of Responsibility Pattern - 명령 시퀀스 실행기
// =============================================
public class CommandSequenceExecutor : ICommandSequenceExecutor
{
    private readonly IDeviceCommunicator _communicator;

    public event Action<CommandProgress> ProgressChanged;

    public CommandSequenceExecutor(IDeviceCommunicator communicator)
    {
        _communicator = communicator;
    }
    public async Task<bool> ExecuteSequenceAsync(IEnumerable<IDeviceCommand> commands, CancellationToken cancellationToken)
    {
        var commandList = new List<IDeviceCommand>(commands);
        var totalSteps = commandList.Count;
        var currentStep = 0;

        foreach(var command in commandList)
        {
            currentStep++;
            ReportProgress(command.Name, CommandState.Ready, currentStep, totalSteps, 0 , "Executing command...");
            try
            {
                var success = await command.ExecuteAsync(_communicator, cancellationToken);

                if (success)
                {
                    ReportProgress(command.Name, CommandState.Success, currentStep, totalSteps, 0, "명령 완료");
                    Console.WriteLine($"✅ [{currentStep}/{totalSteps}] {command.Name} - 성공");
                    Console.WriteLine();
                }
                else
                {
                    ReportProgress(command.Name, CommandState.Failed, currentStep, totalSteps, 0, "명령 실패");
                    Console.WriteLine($"❌ [{currentStep}/{totalSteps}] {command.Name} - 실패");
                    Console.WriteLine("💥 시퀀스 실행을 중단합니다.");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                ReportProgress(command.Name, CommandState.Cancelled, currentStep, totalSteps, 0, "사용자 취소");
                Console.WriteLine($"🚫 [{currentStep}/{totalSteps}] {command.Name} - 취소됨");
                return false;
            }
        }
        Console.WriteLine("🎉 모든 명령이 성공적으로 완료되었습니다!");
        return true;
    }

    private void ReportProgress(string commandName, CommandState state, int currentStep, int totalSteps, int retryCount, string message)
    {
        ProgressChanged?.Invoke(new CommandProgress
        {
            CommandName = commandName,
            State = state,
            CurrentStep = currentStep,
            TotalSteps = totalSteps,
            RetryCount = retryCount,
            Message = message
        });
    }
}
