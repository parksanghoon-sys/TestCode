
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
        Test();
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
    private void Test()
    {
        Console.WriteLine("TEST");
    }
}
