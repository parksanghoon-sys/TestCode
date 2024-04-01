using System.Formats.Asn1;
using System.Runtime.CompilerServices;

internal class Program
{
    static async Task Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Task task = Task.Run(async () =>
        {
            for(int i = 0; i< 10; i ++)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    await Console.Out.WriteLineAsync("작업 취소");
                    return;
                }
                await Console.Out.WriteLineAsync($"작업 {i +1 }");
                await Task.Delay(1000);
            }
            await Console.Out.WriteLineAsync("작업 완료");
        },cancellationToken);

        Thread.Sleep(5000);
        //cancellationTokenSource.CancelAfter(3000);
        cancellationTokenSource.Cancel();
        try
        {
             await task;
        }
        catch (Exception)
        {
            Console.WriteLine("작업이 취소되었습니다.");
        }
    }
}