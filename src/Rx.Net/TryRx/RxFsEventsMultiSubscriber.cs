
// Example code only

public class RxFsEventsMultiSubscriber : IObservable<FileSystemEventArgs>
{
    private readonly object sync = new();
    private readonly List<Subscription> _subscriptions = new();
    private readonly FileSystemWatcher watcher;
    public RxFsEventsMultiSubscriber(string folder)
    {
        this.watcher = new FileSystemWatcher(folder);
        watcher.Created += SendEventToObservers;
        watcher.Changed += SendEventToObservers;
        watcher.Renamed += SendEventToObservers;
        watcher.Deleted += SendEventToObservers;

        watcher.Error += SendErrorToObservers;
    }
    public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
    {
        Subscription sub = new Subscription(this, observer);
        lock (this.sync)
        {
            _subscriptions.Add(sub);
            if (this._subscriptions.Count == 1)
            {
                watcher.EnableRaisingEvents = true;
            }
        }
        return sub;
    }
    private void Unsubscribe(Subscription sub)
    {
        lock (this.sync)
        {
            this._subscriptions.Remove(sub);

            if (this._subscriptions.Count == 0)
            {
                watcher.EnableRaisingEvents = false;
            }
        }
    }
    private void SendErrorToObservers(object sender, ErrorEventArgs e)
    {
        Exception x = e.GetException();
        lock (this.sync)
        {
            foreach (var subscription in this._subscriptions)
            {
                subscription.Observer.OnError(x);
            }

            this._subscriptions.Clear();
        }
    }

    private void SendEventToObservers(object sender, FileSystemEventArgs e)
    {
        lock (this.sync)
        {
            foreach (var subscription in this._subscriptions)
            {
                subscription.Observer.OnNext(e);
            }
        }
    }


    private class Subscription : IDisposable
    {
        private RxFsEventsMultiSubscriber? parent;

        public Subscription(
            RxFsEventsMultiSubscriber rxFsEventsMultiSubscriber,
            IObserver<FileSystemEventArgs> observer)
        {
            this.parent = rxFsEventsMultiSubscriber;
            this.Observer = observer;
        }

        public IObserver<FileSystemEventArgs> Observer { get; }

        public void Dispose()
        {
            this.parent?.Unsubscribe(this);
            this.parent = null;
        }
    }
}

