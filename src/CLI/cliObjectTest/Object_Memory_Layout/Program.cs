using System.Runtime.InteropServices;

internal class Program
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MyStruct
    {
        public int i;     // 4
        public double d;  // 8
        public byte b;    // 1
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    class MyClass
    {
        [FieldOffset(0)]
        public int i;
        [FieldOffset(4)]
        public double d;
        [FieldOffset(12)]
        public byte b;
        public void Print()
        {
            Console.WriteLine("Print");
        }
    }
    private static void Main(string[] args)
    {
        int size = Marshal.SizeOf(typeof(MyStruct));
        Console.WriteLine(size);

        var s = new MyClass() { i =1, d = 2.0, b = 3 };
        var htype = s.GetType().TypeHandle;
        Type t = Type.GetTypeFromHandle(htype);
        byte[] buff = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(s, ptr, true);
        Marshal.Copy(ptr,buff, 0, size);
        Marshal.FreeHGlobal(ptr);

        string filename = @"D:\Temp\1.txt";
        using (var fs = new FileStream(filename, FileMode.Create))
        {
            using (var wr = new BinaryWriter(fs))
            {
                wr.Write(buff);
            }
        }

        byte[] bytes = File.ReadAllBytes(filename);
        Console.WriteLine("Hello, World!");
    }
}