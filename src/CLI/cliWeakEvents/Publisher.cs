
/// <summary>
/// 강한 참조로 인한 메모리 누수
/// </summary>
public class Publisher
{
    public event EventHandler? Event;
    public void RaiseEvnet()
    {
        Event?.Invoke(this, EventArgs.Empty);
    }
}
