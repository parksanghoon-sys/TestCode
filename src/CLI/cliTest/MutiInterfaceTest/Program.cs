namespace mutitest;

class Program
{
    private static void Main(string[] args)
    {
        IAactionA actionAA = new ActionAA();

        actionAA.A();
        Console.WriteLine("Hello, World!");
        Console.WriteLine();
    }
}

interface IAactionA
{
    void A();
}
interface IAactionAA : IAactionA
{
    void AA();
}
interface IActionB
{
    void B();
}
internal class ActionA : IAactionA
{
    public void A()
    {
        Console.WriteLine( "ActionA");
    }
}
internal class ActionB : IActionB
{
    public void B()
    {
        Console.WriteLine("ActionB");
    }
}
internal class ActionAA : ActionA, IAactionA
{
    public void AA()
    {
        Console.WriteLine("ActionAA");
    }
    public void A()
    {
        Console.WriteLine("ActionAA");
    }
}
internal class ActionAAA : IAactionAA
{
    public void A()
    {
        throw new NotImplementedException();
    }

    public void AA()
    {
        throw new NotImplementedException();
    }
}

