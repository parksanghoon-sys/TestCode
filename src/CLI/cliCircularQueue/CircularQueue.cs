using System.Security.Cryptography;

public class CircularQueue<T>
{
    private T[] _elements;
    private int _front;
    private int _rear;
    private int _max;
    private int _count;
    public int Count => _count;
    public int Size => _max;
    public int Capacity => _max;

    public CircularQueue(int size)
    {
        _elements = new T[size];
        _front = 0;
        _rear = -1;
        _max = size;
        _count = 0;
    }
    public T PeekHead()
    {
        if(_count == 0)
            return default(T);
        return _elements[_rear];
    }
    public T PeekTail()
    {
        if(_count == 0)
            return default(T);
        return _elements[_front];
    }
    public void Enqueue(T item)
    {
        if(_count == _max)
            Dequeue();
        _rear = (_rear +1) % _max;
        _elements[_rear] = item;

        _count ++;    
    }
    public T Dequeue()
    {
        if(_count == 0)
        {
            return default(T);
        }
        else
        {
            T element = _elements[_front];
            _front = (_front +1) % _max;
            _count--;

            return element;
        }
    }
    public T[] GetElements(bool reverse = false)
    {
        T[] values = new T[_count];
        CopyTo(values, reverse);
        return values;        
    }

    public void CopyTo(T[] targetArray, bool reverse = false)
    {
        int targetPose = 0;

        int j = 0;
        for(int i = _front; j < _count;)
        {
            targetArray[targetPose++] = _elements[i];
            i = (i+1) % _max;
            j ++;
        }
        if(reverse)
            Array.Reverse(targetArray);        
    }
    public override string ToString()
    {
        if (_count == 0)
        {
            return string.Empty;
        }

        T[] values = GetElements();
        return string.Join(",", values);
    }
}
 