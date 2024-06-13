internal class Program
{
    /// <summary>
    /// Age
    /// </summary>
    private int Age;
    /// <summary>
    /// Bearing
    /// </summary>
    private int Bearing;
    /// <summary>
    /// Main 함수
    /// </summary>
    /// <param name="args"></param>
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
