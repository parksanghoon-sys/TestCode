using System;
using System.Threading;

class Program
{
    static int total = 0;

    static void Main(string[] args)
    {
        // 스레드 생성
        Thread[] threads = new Thread[10];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(AddToTotal);
            threads[i].Start(i + 1);
        }

        // 모든 스레드가 종료될 때까지 대기
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Console.WriteLine("최종 결과: " + total);
    }

    static void AddToTotal(object data)
    {
        int valueToAdd = (int)data;

        // total에 valueToAdd를 더하는 작업을 락프리로 수행
        int original, newValue;
        do
        {
            original = total;
            newValue = original + valueToAdd;
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} :  Current Thread Id \t {newValue}");
        }
        while (Interlocked.CompareExchange(ref total, newValue, original) != original);
    }
}
