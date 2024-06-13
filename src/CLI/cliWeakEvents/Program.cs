internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var publisher = new Publisher();
        var subscriber = new Subscriber();

        publisher.Event += subscriber.HandleEvent;
        publisher.RaiseEvnet();
        subscriber = null;

        GC.Collect();
        publisher.RaiseEvnet();

        var weakEventPublisher = new WeakReferencePublisher();
        var weakEventSubscriber = new WeakReferenceSubscriber();
        weakEventSubscriber.Subscribe(weakEventPublisher);
        weakEventPublisher.RaiseEvent();
        weakEventSubscriber = null;
        GC.Collect();
        weakEventPublisher.RaiseEvent();
    }
}
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
public class Subscriber
{
    public void HandleEvent(object sender, EventArgs e)
    {
        Console.WriteLine("Evnet received.");
    }
}
/// <summary>
/// 약한 참조의 이벤트
/// 메모리 누수 없음
/// </summary>
/// <typeparam name="TEvnetArgs"></typeparam>
public class WeakEvent<TEvnetArgs> where TEvnetArgs : EventArgs
{
    public readonly List<WeakReference<EventHandler<TEvnetArgs>>> _eventHandlers = [];
    public void AddEventHandler(EventHandler<TEvnetArgs> handler)
    {
        if (handler == null) return;
        _eventHandlers.Add(new WeakReference<EventHandler<TEvnetArgs>> (handler));
    }
    public void RemoveEventHandler(EventHandler<TEvnetArgs> handler)
    {
        if (handler == null) return;
        var eventHandler = _eventHandlers.FirstOrDefault(wr =>
        {
            wr.TryGetTarget(out var targert);
            return targert == handler;
        });

        if(eventHandler != null)
        {
            _eventHandlers.Remove(eventHandler);
        }
    }
    public void RaiseEvent(object sender, TEvnetArgs e)
    {
        foreach(var handler in _eventHandlers.ToArray())
        {
            if(handler.TryGetTarget(out var targert))
            {
                targert(sender, e);
            }
        }
    }
}
public class WeakReferencePublisher
{
    public WeakEvent<EventArgs> Event { get; } = new WeakEvent<EventArgs>();

    public void RaiseEvent()
    {
        Event.RaiseEvent(this, EventArgs.Empty);
    }
}
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
