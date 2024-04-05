public class RxFsEvents : IObservable<FileSystemEventArgs>
{
    private readonly string folder;
    public RxFsEvents(string folder)
    {
        this.folder = folder;
    }
    public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
    {
        FileSystemWatcher watcher = new FileSystemWatcher(folder);

        object sync = new();

        bool onErrorAlreadyCalled = false;

        void SendToObserver(object _ , FileSystemEventArgs e )
        {
            lock(sync)
            {
                if( onErrorAlreadyCalled == false ) 
                {
                    observer.OnNext(e);
                }
            }
        }
        watcher.Created += SendToObserver;
        watcher.Changed += SendToObserver;
        watcher.Renamed += SendToObserver;
        watcher.Deleted += SendToObserver;

        watcher.Error += (_, e) =>
        {
            lock (sync)
            {
                // The FileSystemWatcher might report multiple errors, but
                // we're only allowed to report one to IObservable<T>.
                if (!onErrorAlreadyCalled)
                {
                    observer.OnError(e.GetException());
                    onErrorAlreadyCalled = true;
                    watcher.Dispose();
                }
            }
        };
        watcher.EnableRaisingEvents = true;

        return watcher;
    }
}
