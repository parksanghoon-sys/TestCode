
public class Subscriber
{
    /// <summary>
    /// 강한 참조로 인한 메모리 누수
    /// </summary>
    public void HandleEvent(object sender, EventArgs e)
    {
        Console.WriteLine("Evnet received.");
    }
}
