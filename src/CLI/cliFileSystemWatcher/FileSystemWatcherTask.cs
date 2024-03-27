using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliFileSystemWatcher
{
    internal class FileSystemWatcherTask
    {
        private object _lockObj = new();
        private FileSystemWatcher _watcher = new FileSystemWatcher(@".");
        private BlockingCollection<string?> _queue = new ();

        private async void FileCreateHandlerAsync(object _, FileSystemEventArgs e) => await Task.Run(() =>
        {
            var lockTaken = false;
            try
            {
                Monitor.Enter(_lockObj, ref lockTaken);
                if (!_queue.IsCompleted)
                {
                    _queue.Add(e.Name);
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_lockObj);
                }
            }
        });
        public FileSystemWatcherTask()
        {
            _watcher.Created += FileCreateHandlerAsync;
            _watcher.EnableRaisingEvents = true;
            var consumer = Task.Run(async () =>
            {
                foreach (string? name in _queue.GetConsumingEnumerable())
                    await Task.Run(() => Console.WriteLine($"File has been created: \"{name}\"."));

                Debug.Assert(_queue.IsCompleted);
            });
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
            _watcher.Created -= FileCreateHandlerAsync;
            lock (_lockObj)
            {
                _queue.CompleteAdding();
            }

            bool completed = consumer.Wait(100);
            Debug.Assert(completed && TaskStatus.RanToCompletion == consumer.Status);
        }        
        
    }
}
