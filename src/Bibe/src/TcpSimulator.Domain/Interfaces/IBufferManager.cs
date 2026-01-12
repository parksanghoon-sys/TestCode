namespace TcpSimulator.Domain.Interfaces;

public interface IBufferManager
{
    RentedBuffer<byte> Rent(int minimumSize);
    void Return(RentedBuffer<byte> buffer);
}

public readonly struct RentedBuffer<T> : IDisposable
{
    private readonly T[]? _array;
    private readonly System.Buffers.ArrayPool<T> _pool;
    private readonly int _length;

    public Memory<T> Memory { get; }
    public Span<T> Span => Memory.Span;
    public int Length => _length;

    public RentedBuffer(T[] array, int length, System.Buffers.ArrayPool<T> pool)
    {
        _array = array;
        _length = length;
        _pool = pool;
        Memory = array.AsMemory(0, length);
    }

    public void Dispose()
    {
        if (_array != null)
        {
            _pool.Return(_array, clearArray: true);
        }
    }
}
