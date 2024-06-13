
/// <summary>
/// 약한 참조의 이벤트
/// 메모리 누수 없음
/// </summary>
/// <typeparam name="TEvnetArgs"></typeparam>
public class WeakReferencePublisher
{
    public WeakEvent<EventArgs> Event { get; } = new WeakEvent<EventArgs>();

    public void RaiseEvent()
    {
        Event.RaiseEvent(this, EventArgs.Empty);
    }
}
