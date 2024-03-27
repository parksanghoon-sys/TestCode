using KeyBoardMouseHook;
using System.Reactive.Linq;
using System.Reflection.Emit;
using static System.Net.Mime.MediaTypeNames;

internal class Program
{
    private static void Main(string[] args)
    {
        IObservable<long> ticks = Observable.Timer(
        dueTime: TimeSpan.Zero,
        period: TimeSpan.FromSeconds(1));

        ticks.Subscribe(
            tick => Console.WriteLine($"Tick {tick}"));
        MouseHook.HookStart();
        // Observable 클래스를 사용하여 KeyboardHook.KeyDown 이벤트를 관찰 가능한 시퀀스로 변환합니다.
        IObservable<int> keyDownObservable = Observable.FromEvent<KeyboardEventCallback, int>(
            handler => vkCode => { handler(vkCode); return true; },
            h => KeyboardHook.KeyDown += h,
            h => KeyboardHook.KeyDown -= h
        );

        IDisposable subscription = keyDownObservable.Subscribe(vkCode =>
        {
            // 각 키 다운 이벤트가 발생할 때마다 실행됩니다.
            // 예를 들어, AppendText 메서드를 호출하여 해당 키 다운 이벤트를 텍스트 박스에 출력합니다.
            AppendText($"KEYDOWN : {vkCode}");
        });

        // MouseEventCallback 델리게이트를 사용하여 마우스 이벤트를 관찰 가능한 시퀀스로 변환합니다.
        IObservable<(MouseEventType type, int x, int y)> mouseEventObservable = Observable.FromEvent<MouseEventCallback, (MouseEventType type, int x, int y)>(
            handler => (type, x, y) => { handler((type, x, y)); return true; },
            h => MouseHook.MouseMove += h,
            h => MouseHook.MouseMove -= h
        );

        // 변환된 관찰 가능한 시퀀스에 대해 Subscribe를 설정합니다.
        IDisposable subscription2 = mouseEventObservable.Subscribe(mouseEvent =>
        {
            // 각 마우스 이벤트가 발생할 때마다 실행됩니다.
            // 예를 들어, AppendText 메서드를 호출하여 해당 마우스 이벤트를 텍스트 박스에 출력합니다.
            AppendText($"Mouse Event Type: {mouseEvent.type}, X: {mouseEvent.x}, Y: {mouseEvent.y}");
        });
        
        // Dispose() 메서드를 호출하여 구독을 취소할 수 있습니다.
        // subscription.Dispose();

        Console.ReadLine();
    }



    private static void AppendText(string v)
    {
        Console.WriteLine(v);
    }
}