using CleanProtocolSample.Common;

namespace CleanProtocolSample.Protocol;

internal sealed class RingBuffer<T>
{
    private readonly Lock _sync = new();
    private readonly T[] _buffer;
    private int _index;
    private int _count;
    public RingBuffer(int capacity)
    {
        capacity = capacity.Positive(nameof(capacity));

        _buffer = new T[capacity];
    }
    /// <summary>
    /// 데이터 추가.
    /// 가득 찬 경우 가장 오래된 데이터 overwrite.
    /// </summary>
    public void Add(T item)
    {
        lock(_sync)
        {
            _buffer[_index] = item;
            _index = (_index +1) % _buffer.Length;

            if(_count < _buffer.Length)            
                _count++;
            
        }
    }
    /// <summary>
    /// 현재 데이터 snapshot 반환.
    /// </summary>
    public IReadOnlyList<T> Snapshot()
    {
        lock(_sync)
        {
            var list = new List<T>(_count);

            for(var i = 0; i < _count; i++)
            {
                var index = (_index - _count +i + _buffer.Length) % _buffer.Length;
                list.Add(_buffer[index]);
            }
            return list;
        }
    }
}
