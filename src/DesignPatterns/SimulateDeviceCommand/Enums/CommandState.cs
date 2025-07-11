namespace SimulateDeviceCommand.Enums;

public enum CommandState
{
    Ready,
    Sending,
    WaitingResponse,
    Success,
    Failed,
    Retrying,
    Cancelled
}

public enum ResponseType
{
    Success,
    Fail,
    Timeout,
    Error
}