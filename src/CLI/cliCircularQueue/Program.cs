using System.Diagnostics;
using System.Linq;

namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CircularQueue<int?> cq = new CircularQueue<int?>(3);

            {
                Debug.Assert(cq.Capacity == 2);
                Debug.Assert(cq.PeekHead() == null);
                Debug.Assert(cq.PeekTail() == null);
                Debug.Assert(cq.Count == 0);                
                Debug.Assert(cq.Dequeue() == null);
            }

            {
                cq.Enqueue(0);
                Debug.Assert(cq.PeekHead() == 0);
                Debug.Assert(cq.PeekTail() == 0);
                Debug.Assert(cq.ToString() == "0");
            }

            {
                cq.Enqueue(1);
                cq.Enqueue(2);
                cq.Enqueue(3);
                cq.Enqueue(4);
                Debug.Assert(cq.PeekHead() == 4);
                Debug.Assert(cq.PeekTail() == 2);
                Debug.Assert(cq.ToString() == "2,3,4");
            }

            {
                int? elem = cq.Dequeue();
                Debug.Assert(elem == 2);
            }

            {
                cq.Enqueue(5);

                int?[] values = new int?[cq.Count];
                cq.CopyTo(values, reverse: true);
                //Debug.Assert(Enumerable.SequenceEqual(values,new List<int> { 5, 4, 3 }));
            }

            {
                cq.Enqueue(6);
                int?[] values = cq.GetElements();
                //Debug.Assert(Enumerable.SequenceEqual(values, [4, 5, 6]));
            }
        }
    }
}