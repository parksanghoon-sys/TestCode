using System.Collections;
using static Program;
using System.Collections.Concurrent;
using Microsoft.VisualBasic;

class Program
{
    static int s_TaskSeq = 0;
    static BlockingCollection<Coroutine> queue = new BlockingCollection<Coroutine>();
    public class AsyncAction
    {

        public AsyncAction(Coroutine coroutine, Action action)
        {
            Task.Run(() =>
            {
                action();
                queue.Add(coroutine);
            });
        }
    }
    public class AsyncWrite
    {
        Coroutine coroutine;
        public AsyncWrite(Coroutine coroutine, string fileName)
        {
            this.coroutine = coroutine;
            Write(fileName);            
        }
        private async void Write(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                var buff = new byte[1024 * 1024 * 1000];
                await fs.WriteAsync(buff);
                await Console.Out.WriteLineAsync($"[{DateTime.Now}] task_seq:{s_TaskSeq},\ttid:{Thread.CurrentThread.ManagedThreadId}\ttask finished file writing.");
            }
            queue.Add(coroutine);
        }
    }

    public class Coroutine
    {
        private IEnumerator _enumerator;
        public string _message { get; set; }
        public Coroutine()
        {
            _enumerator = Handler();
        }
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }
        public virtual IEnumerator Handler()
        {
            DateTime start_time = DateTime.Now;
            int taskSeq = ++Program.s_TaskSeq;

            Console.WriteLine($"[{DateTime.Now}] task_seq :{taskSeq}, \ttid:" +
                $"{Thread.CurrentThread.ManagedThreadId} /ttask '{_message}' starts ans suspend.");


            // 사용자 로직
            yield return new AsyncAction(this, () =>
            {
                Console.WriteLine($"[{DateTime.Now}] task_seq:{s_TaskSeq},\ttid:{Thread.CurrentThread.ManagedThreadId}\ttask is sleeping.");
                Thread.Sleep(1000);
                Console.WriteLine($"[{DateTime.Now}] task_seq:{s_TaskSeq},\ttid:{Thread.CurrentThread.ManagedThreadId}\ttask will resume.");
            } );
            Console.WriteLine($"[{DateTime.Now}] task_seq:{s_TaskSeq},\ttid:{Thread.CurrentThread.ManagedThreadId}\ttask starts file writing.");
            yield return new AsyncWrite(this, s_TaskSeq.ToString() + ".txt");

            TimeSpan timeSpan = DateTime.Now - start_time;
            Console.WriteLine($"[{DateTime.Now}] task_seq:{s_TaskSeq},\ttid:{Thread.CurrentThread.ManagedThreadId}\ttask finished. elapsed_time:{timeSpan}.");
            yield break;
        }
    }

    //public IEnumerator Coroutine()
    //{
    //    int i = 0;
    //    Console.WriteLine($"Corutine {++i}");
    //    yield return null;
    //    Console.WriteLine($"Corutine {++i}");
    //    yield return null;
    //    Console.WriteLine($"Corutine {++i}");
    //    yield return null;
    //}
    static void Main(string[] args)
    {
        //Program program = new Program();
        //var coroutine = program.Coroutine();
        //Console.WriteLine("Main 1");
        //coroutine.MoveNext();
        //Console.WriteLine("Main 2");
        //coroutine.MoveNext();
        //Console.WriteLine("Main 3");
        //coroutine.MoveNext();

        for (int i = 0; i < 4; i++)
        {
            Task.Run(() =>
            {
                string message = Console.ReadLine();
                Coroutine coroutine = new Coroutine();
                coroutine._message = message;
                queue.Add(coroutine);
            });
        }
        while (true)
        {
            try
            {
                var coroutine = queue.Take();
                coroutine.MoveNext();
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("That's All");
                throw;
            }
        }
    }
}
