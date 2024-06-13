
/// <summary>
/// 약한 참조의 이벤트
/// 메모리 누수 없음
/// </summary>
/// <typeparam name="TEvnetArgs"></typeparam>
public class WeakReferenceSubscriber
{
    public void Subscribe(WeakReferencePublisher publisher)
    {
        publisher.Event.AddEventHandler(HandleEvent);
    }    
    public void HandleEvent(object sender, EventArgs e)
    {
        Console.WriteLine("Evnet received.");
    }
}
