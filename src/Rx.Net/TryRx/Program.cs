using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

internal class Program
{
    private static void Main(string[] args)
    {

        IList<string> stringList = new List<string>
        {
            "Test1",
            "Test2",
            "Test3"
        };
        IList<int> intList = new List<int>
        {
            1,2,3,4,5,6,7,8,9,10,11,12,
        };
        string filePath = @"D:\WPF_Test_UI\src\CLI\cliTryRx";
        #region Timer Rx
        //IObservable<long> ticks = Observable.Timer(
        //                dueTime: TimeSpan.Zero,
        //                period: TimeSpan.FromSeconds(1));
        //ticks.Subscribe(
        //        tick => Console.WriteLine($"Tick {tick}"));  
        #endregion

        #region 다이머당 돌아가는 Rx
        //IObservable<long> interval = Observable.Interval(TimeSpan.FromMilliseconds(150));
        //interval.Sample(TimeSpan.FromSeconds(1)).Subscribe(Console.WriteLine);

        //MyObserver myObserver = new MyObserver();
        //myObserver.Run(); 
        #endregion


        //var numbers = new MySequenceOfNumbers();
        //numbers.Subscribe(
        //    number => Console.WriteLine($"{Environment.CurrentManagedThreadId} Received value: {number}"),
        //    () => Console.WriteLine("Sequence terminated"));


        #region 구독하면 Instance 가 증가하는 Rx
        //RxFsEvents rxFsEvents = new RxFsEvents(filePath);
        //rxFsEvents.Subscribe((fs) =>
        //{
        //    Console.WriteLine($"{Environment.CurrentManagedThreadId} {fs.ChangeType}");
        //});
        //rxFsEvents.Subscribe((fs) =>
        //{
        //    Console.WriteLine($"{Environment.CurrentManagedThreadId} {fs.ChangeType}");
        //}); 
        #endregion

        #region  구독하면 Instance 가 증가 하지 않는 Rx
        //RxFsEventsMultiSubscriber rxFsEventsMultiSubscriber = new(filePath);

        //rxFsEventsMultiSubscriber.Subscribe((fs) =>
        //{
        //    Console.WriteLine($"{Environment.CurrentManagedThreadId} {fs.ChangeType}");
        //});
        //rxFsEventsMultiSubscriber.Subscribe((fs) =>
        //{
        //    Console.WriteLine($"{Environment.CurrentManagedThreadId} {fs.ChangeType}");
        //}); 
        #endregion
        //IObservable<int> range = Observable.Range(10, 15);
        //range.Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));

        // Not the best way to do it!
        //IObservable<int> Range(int start, int count) =>
        //    Observable.Create<int>(observer =>
        //    {
        //        for (int i = 0; i < count; ++i)
        //        {
        //            observer.OnNext(start + i);
        //        }

        //        return Disposable.Empty;
        //    });
        //Range(0, 10).Subscribe((s) =>
        //{
        //    Console.WriteLine(s);
        //});

        #region 이벤트 Repliction 등록
        //FileSystemWatcher watcher = new(@"c:\incoming");
        //IObservable<EventPattern<FileSystemEventArgs>> changeEvents = Observable
        //    .FromEventPattern<FileSystemEventArgs>(watcher, nameof(watcher.Changed));

        //IObservable<EventPattern<FileSystemEventArgs>> changeEvents2 = Observable
        //        .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
        //            h => watcher.Changed += h,
        //            h => watcher.Changed -= h); 
        #endregion
        #region Task Rx
        //Task<string> t = Task.Run(() =>
        //{
        //    Console.WriteLine($"{Environment.CurrentManagedThreadId} Task running...");
        //    return "Test";
        //});
        ////IObservable<string> source = t.ToObservable();
        //IObservable<string> source = Observable.FromAsync(async () =>
        //{
        //    Console.WriteLine($"{Environment.CurrentManagedThreadId} Task running...");
        //    await Task.Delay(50);
        //    return "Test";
        //});
        //source.Subscribe(
        //    Console.WriteLine,
        //    () => Console.WriteLine($"{Environment.CurrentManagedThreadId} completed"));
        //source.Subscribe(
        //    Console.WriteLine,
        //    () => Console.WriteLine($"{Environment.CurrentManagedThreadId} completed"));
        #endregion

        //Subject<int> s = new();
        //s.Subscribe(x => Console.WriteLine($"{Environment.CurrentManagedThreadId} Sub1: {x}"));
        //s.Subscribe(x => Console.WriteLine($"{Environment.CurrentManagedThreadId} Sub2: {x}"));

        //s.OnNext(1);
        //s.OnNext(2);
        //s.OnNext(3);

        IObservable<int> xs = Observable.Range(0, 10); // The numbers 0-9
        //IObservable<int> evenNumbers = xs.Where(i => i % 2 == 0);

        IObservable<int> evenNumbers =
                from i in xs
                where i % 2 == 0
                select i;

        evenNumbers.Dump("Where");

        Console.WriteLine($"{Environment.CurrentManagedThreadId} : Console Thread Id");
        Console.ReadLine();
    }
    public static IObservable<int> Range(int start, int count)
    {
        int max = start + count;
        return Observable.Generate(
            start,
            value => value < max,
            value => value + 1,
            value => value);
    }

    static void StartAction()
    {
        var start = Observable.Start(() =>
        {
            Console.Write("Working away");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.Write(".");
            }
        });

        start.Subscribe(
            unit => Console.WriteLine("Unit published"),
            () => Console.WriteLine("Action completed"));
    }

    static void StartFunc()
    {
        var start = Observable.Start(() =>
        {
            Console.Write("Working away");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.Write(".");
            }
            return "Published value";
        });

        start.Subscribe(
            Console.WriteLine,
            () => Console.WriteLine("Action completed"));
    }
}
public static class SampleExtensions
{
    public static void Dump<T>(this IObservable<T> source, string name)
    {
        source.Subscribe(
            value => Console.WriteLine($"{name}-->{value}"),
            ex => Console.WriteLine($"{name} failed-->{ex.Message}"),
            () => Console.WriteLine($"{name} completed"));
    }
}

