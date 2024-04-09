using System.Collections.Concurrent;

namespace wpf비동기연속스트림
{
    public class ChatStream
    {
        private CancellationTokenSource _cancellation;
        private readonly SemaphoreSlim _semaphore;
        private ConcurrentQueue<string> _chatCollection;

        public ChatStream()
        {
            _cancellation = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(0);
            _chatCollection = new ConcurrentQueue<string>();
        }
        public async IAsyncEnumerable<string> GetChatAsync()
        {
            while(_cancellation.IsCancellationRequested == false)
            {
                await _semaphore.WaitAsync();
                string chat;
                if(_chatCollection.TryDequeue(out chat))
                {
                    yield return chat;
                }
            }
        }
        public void Send(string message)
        {
            _chatCollection.Enqueue(message);
            _semaphore.Release(1);
        }
        public void Stop()
        {
            _cancellation.Cancel();
        }
    }
}