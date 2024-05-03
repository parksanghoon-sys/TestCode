internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

/// <summary>
/// 반공변 제네릭 인터페이스
/// </summary>
public class TestD : ITest
{
    interface IContravariant<in A> { }

    // Extending contravariant interface.
    interface IExtContravariant<in A> : IContravariant<A> { }

    class Sample<A> : IContravariant<A> { }
    public void Test()
    {
        IContravariant<ITest> iobj = new Sample<ITest>();
        IContravariant<TestA> istr = new Sample<TestA>();

        // You can assign iobj to istr because
        // the IContravariant interface is contravariant.
        istr = iobj;    
    }
}
/// <summary>
/// 공변 제네릭 인터페이스
/// </summary>
public class TestC : ITest
{
    public TestA SampleTestA()
    {
        return new TestA();
    }

    public TestB SampleTestB()
    {
        return new TestB();
    }

    public delegate R DCovariant<out R>();

    public void Test()
    {
        DCovariant<ITest> dButton = SampleTestA;
        DCovariant<ITest> dControl = SampleTestB;

        dButton = dControl;
        dControl();
    }
}
/// <summary>
/// 공변 제네릭 인터페이스
/// 자식 객체가 부며 변수로 할당 되는 형변환
/// int -> object X
/// </summary>
public class TestB : ITest
{
    public void Test()
    {
        IConvariant<object> iobj = new Sample<object>();
        IConvariant<string> istr = new Sample<string>();

        iobj = istr;
    }

    private interface IConvariant<out R>
    {

    }
    private interface IExtConvariant<out R> : IConvariant<R>
    {

    }
    class Sample<R> : IConvariant<R> { }
}
/// <summary>
/// 공변성 형변환
/// </summary>
public class TestA : ITest
{
    IEnumerable<string> strings = new List<string>();
    IEnumerable<object> objects;
    private string GetString()
    {
        return "";
    }
    public TestA()
    {
        objects = strings;
    }
    public void Test()
    {
        object del = GetString();
    }
}
interface ITest
{
    public void Test();
}