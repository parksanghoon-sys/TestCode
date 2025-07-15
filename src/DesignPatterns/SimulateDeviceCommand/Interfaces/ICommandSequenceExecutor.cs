using SimulateDeviceCommand.Models;

namespace SimulateDeviceCommand.Interfaces;

// Chain of Responsibility Pattern - 명령 시퀀스 실행
public interface ICommandSequenceExecutor
{
    event Action<CommandProgress> ProgressChanged;
    Task<bool> ExecuteSequenceAsync(IEnumerable<IDeviceCommand> commands, CancellationToken cancellationToken);
}
