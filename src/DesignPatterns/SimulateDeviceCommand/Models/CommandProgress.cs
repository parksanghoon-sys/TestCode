using SimulateDeviceCommand.Enums;

namespace SimulateDeviceCommand.Models;

public class CommandProgress
{
    public string CommandName { get; set; }
    public CommandState State { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public int RetryCount { get; set; }
    public string Message { get; set; }
}
