using KeyBoardMouseHook;
using System;
using System.Reactive.Linq;

internal class Program
{
    private static void Main(string[] args)
    {
        IObservable<long> ticks = Observable.Timer(
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromSeconds(1));

        IDisposable tickSubscription = ticks.Subscribe(
            tick => Console.WriteLine($"Tick {tick}"));

        MouseHook.HookStart();

        // 키보드 이벤트 관찰 가능한 시퀀스
        IObservable<int> keyDownObservable = Observable.FromEvent<KeyboardEventCallback, int>(
               handler =>
               {
                   KeyboardEventCallback callback = vkCode =>
                   {
                       handler(vkCode);
                       return true;
                   };
                   return callback;
               },
               h => KeyboardHook.KeyDown += h,
               h => KeyboardHook.KeyDown -= h
           );

        IDisposable keyDownSubscription = keyDownObservable.Subscribe(vkCode =>
        {
            Console.WriteLine($"KEYDOWN: {vkCode}");
        });
        // 마우스 이벤트 관찰 가능한 시퀀스
        IObservable<(MouseEventType type, int x, int y)> mouseEventObservable = Observable.FromEvent<MouseEventCallback, (MouseEventType type, int x, int y)>(
            handler =>
            {
                MouseEventCallback callback = (type, x, y) =>
                {
                    handler((type, x, y));
                    return true;
                };
                return callback;
            },
            h => MouseHook.MouseMove += h,
            h => MouseHook.MouseMove -= h
        );

        IDisposable mouseEventSubscription = mouseEventObservable.Subscribe(mouseEvent =>
        {
            Console.WriteLine($"Mouse Event Type: {mouseEvent.type}, X: {mouseEvent.x}, Y: {mouseEvent.y}");
        });

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        // 구독 해제
        tickSubscription.Dispose();
        keyDownSubscription.Dispose();
        mouseEventSubscription.Dispose();
        MouseHook.HookEnd();
    }
}
