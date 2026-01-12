using System.Buffers;
using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Infrastructure.Buffers;

public sealed class PooledBufferManager : IBufferManager
{
    private const int SmallPoolMaxLength = 1024;
    private const int SmallPoolMaxArraysPerBucket = 50;

    private static readonly ArrayPool<byte> SmallPool =
        ArrayPool<byte>.Create(SmallPoolMaxLength, SmallPoolMaxArraysPerBucket);
    private static readonly ArrayPool<byte> LargePool = ArrayPool<byte>.Shared;

    public RentedBuffer<byte> Rent(int minimumSize)
    {
        var pool = minimumSize <= SmallPoolMaxLength ? SmallPool : LargePool;
        var array = pool.Rent(minimumSize);
        return new RentedBuffer<byte>(array, minimumSize, pool);
    }

    public void Return(RentedBuffer<byte> buffer)
    {
        buffer.Dispose();
    }
}
