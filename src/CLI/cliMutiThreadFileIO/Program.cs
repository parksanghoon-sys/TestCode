using System.Collections.Concurrent;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var filePaths = new List<string>
{
    @"D:\Temp\A.txt",
    @"D:\Temp\B.txt",
    @"D:\Temp\C.txt",
    @"D:\Temp\D.txt",
    @"D:\Temp\E.txt",
};
        var texts = new List<string>
{
    "우리나라",
    "대한민국",
    "금수강산",
    "수신제가",
    "치국평천",
};

        var queue = new AppendTextQueue();
        using var cts = new CancellationTokenSource();
        _ = queue.Dequeue(cts.Token);

        var taskCount = 10;
        var repeatCount = 100;
        var startIndex = 0;
        var itemCount = 5;

        var random = new Random();

        var tasks = new List<Task>();
        for (int i = 0; i < taskCount; i++)
        {
            var t = new Task(async () =>
            {
                for (int i = 0; i < repeatCount; i++)
                {
                    await Task.Delay(random.Next(10, 50)).ConfigureAwait(false);
                    var fileIndex = random.Next(startIndex, itemCount);
                    var textIndex = random.Next(startIndex, itemCount);
                    await queue.Enqueue(filePaths[fileIndex], texts[textIndex]).ConfigureAwait(false);
                }
            });
            tasks.Add(t);
            t.Start();
        }
        await Task.WhenAll([.. tasks]);
        await Task.Delay(10000).ConfigureAwait(false);
        cts.Cancel();

        Console.WriteLine("Bye!");
    }
}

public class AppendTextQueue
{
    private CancellationTokenSource? _cts = null;
    private readonly ConcurrentQueue<(string filePath, string appendedText)> _queue;

    public AppendTextQueue()
    {
        _queue = new ConcurrentQueue<(string filePath, string appendedText)>();
    }

    public async Task Enqueue(string filePath, string appendedText)
    {
        await Task.Run(() =>
        {
            _queue.Enqueue((filePath, appendedText));

            try
            {
                if (_cts is not null)
                {
                    _cts!.Cancel();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }).ConfigureAwait(false);
    }

    public async Task Dequeue(CancellationToken token)
    {
        await Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var item))
                {
                     await File.AppendAllTextAsync(item.filePath, item.appendedText);
                }
                else
                {
                    //_cts = new CancellationTokenSource();
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, _cts.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        _cts = null;
                    }
                }
            }
        }, token).ConfigureAwait(false);
    }
}