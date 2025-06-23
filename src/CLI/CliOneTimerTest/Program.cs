

using System.Diagnostics;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Action action = null;
        action = () =>
        {
            Console.WriteLine("OneShotTiemr Test Success");
        };
        OneActionTimer timer = new OneActionTimer(action, 1000);

        Console.WriteLine("Test 진행중");
        Console.ReadLine();
        timer.Dispose();

    }
    
}
internal class OneActionTimer : IDisposable
{
    private Stopwatch Stopwatch = new Stopwatch();
    private Timer _timer;
    private bool _disposed = false;

    public OneActionTimer(Action action, int timeout)
    {
        Stopwatch.Start();
        LinkedList<Action> list = new LinkedList<Action>();
        Dictionary<int,string> dict = new Dictionary<int,string>();
        _timer = new Timer(delegate
        {
            action.Invoke();
            Stopwatch.Stop();
            Console.WriteLine($"소요 시간 : {Stopwatch.ElapsedMilliseconds}");
        }, null, timeout, 0);
    }

    ~OneActionTimer()
    {
        Dispose(false);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _timer.Dispose();
            _timer = null;
        }
        _disposed = true;
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}