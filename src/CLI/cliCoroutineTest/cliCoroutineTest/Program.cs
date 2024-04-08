using System.Collections;

class Program
{
    public IEnumerator Coroutine()
    {
        int i = 0;
        Console.WriteLine($"Corutine {++i}");
        yield return null;
        Console.WriteLine($"Corutine {++i}");
        yield return null;
        Console.WriteLine($"Corutine {++i}");
        yield return null;
    }
    static void Main(string[] args)
    {        
        Program program = new Program();
        var coroutine = program.Coroutine();
        Console.WriteLine("Main 1");
        coroutine.MoveNext();
        Console.WriteLine("Main 2");
        coroutine.MoveNext();
        Console.WriteLine("Main 3");
        coroutine.MoveNext();
    }
}