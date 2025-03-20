using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentableBuffer;
public class RentedBuffer<T> : IRentedBuffer<T>, IDisposable
{
    /// <summary>
    /// 비어 있는 배열
    /// </summary>
    public static readonly IRentedBuffer<T> Empty = new RentedBuffer<T>(0);
#if DEBUG
    // 디버깅을 위한 추적 정보
    private readonly string _allocationStack;
    private string? _disposalStack;
#endif

    private T[]? _array;
    private int _offset;
    private int _length;
    private int _disposed;
    private readonly bool _isWritableBuffer;
    private readonly object _writeLock = new();
    private readonly bool _isTakeOwnership;
    /// <inheritdoc/>
    public bool IsReadOnly => !_isWritableBuffer;

    /// <inheritdoc/>
    protected int CurrentOffset
    {
        get
        {
            ThrowIfDisposed();
            return Volatile.Read(ref _offset);
        }
    }
    /// <inheritdoc/>
    protected int CurrentLength
    {
        get
        {
            ThrowIfDisposed();
            return Volatile.Read(ref _length);
        }
    }
    /// <inheritdoc/>
    public int ReadableBytes => WriterIndex - ReaderIndex;
    /// <inheritdoc/>
    public int ReaderIndex => CurrentOffset;
    /// <inheritdoc/>
    public int WriterIndex => CurrentLength;
    /// <inheritdoc/>
    public int WritableBytes
    {
        get
        {
            ThrowIfDisposed();
            if (!_isWritableBuffer)
            {
                return 0; // 읽기 전용 버퍼는 쓰기 가능 공간이 없음
            }

            return Math.Max(0, MaxCapacity - CurrentLength);
        }
    }
    /// <inheritdoc/>
    public bool IsEndOfBuffer => CurrentOffset >= CurrentLength;

    /// <inheritdoc/>
    public bool IsWritable => _isWritableBuffer && !IsDisposed;

    /// <inheritdoc/>
    public bool IsReadable => ReadableBytes > 0;

    /// <inheritdoc/>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <inheritdoc/>
    public Memory<T> Memory
    {
        get
        {
            ThrowIfDisposed();
            return _array == null ? throw new ObjectDisposedException(GetType().Name) : _array.AsMemory();
        }
    }
    /// <inheritdoc/>
    public T this[Index index]
    {
        get
        {
            ThrowIfDisposed();

            if (_array == null)
                throw new ObjectDisposedException(GetType().Name);

            var actualIndex = index.IsFromEnd ? _length - index.Value : index.Value;

            return actualIndex < 0 || actualIndex >= _length ? throw new ArgumentOutOfRangeException(nameof(index)) : _array[actualIndex];
        }
        set
        {
            ThrowIfDisposed();

            if (!_isWritableBuffer)
                throw new InvalidOperationException("Buffer is read-only");

            if (_array == null)
                throw new ObjectDisposedException(GetType().Name);

            var actualIndex = index.IsFromEnd ? _length - index.Value : index.Value;

            if (actualIndex < 0 || actualIndex >= _length)
                throw new ArgumentOutOfRangeException(nameof(index));

            lock (_writeLock)
            {
                _array[actualIndex] = value;
            }
        }
    }
    /// <inheritdoc/>
    public ArraySegment<T> this[Range range]
    {
        get
        {
            ThrowIfDisposed();

            if (_array == null)
                throw new ObjectDisposedException(GetType().Name);

            var (offset, length) = range.GetOffsetAndLength(_length);
            return offset < 0 || length < 0 || offset + length > MaxCapacity
                ? throw new IndexOutOfRangeException("offset or length is out of range.")
                : new ArraySegment<T>(_array, offset, length);
        }
    }
    /// <inheritdoc/>
    public int MaxCapacity { get; }

    private RentedBuffer(int maxCapacity, bool isWritable)
    {
        MaxCapacity = maxCapacity;
        _array = ArrayPoolAllocator<T>.Rent(maxCapacity);
        _offset = 0;
        _length = isWritable ? 0 : maxCapacity;
        _isWritableBuffer = isWritable;

#if DEBUG
        _allocationStack = Environment.StackTrace;
#endif
    }
    /// <summary>
    /// ReadOnlySpan으로부터 RentedBuffer를 초기화합니다.
    /// </summary>
    /// <param name="other">복사할 ReadOnlySequence</param>
    /// <exception cref="ArgumentException">sequence의 길이가 int.MaxValue를 초과하거나 0 이하인 경우</exception>
    public RentedBuffer(ReadOnlySpan<T> other)
        : this(other.Length <= 0
            ? throw new ArgumentException("Length must be positive.", nameof(other))
            : other.Length, false)
    {
        other.CopyTo(_array);
    }
    /// <summary>
    /// 기존 배열을 사용하여 RentedBuffer를 초기화합니다.
    /// </summary>
    /// <param name="array">사용할 배열</param>
    public RentedBuffer(T[] array) : this(array.AsSpan())
    {
    }
    /// <inheritdoc/>
    public RentedBuffer(ReadOnlyMemory<T> memory) : this(memory.Span)
    {
    }

    /// <summary>
    /// ReadOnlySequence로부터 RentedBuffer를 초기화합니다.
    /// </summary>
    /// <param name="sequence">복사할 ReadOnlySequence</param>
    /// <exception cref="ArgumentException">sequence의 길이가 int.MaxValue를 초과하거나 0 이하인 경우</exception>
    public RentedBuffer(ReadOnlySequence<T> sequence)
        : this(
            sequence.Length > int.MaxValue
                ? throw new ArgumentException("Sequence is too large to process", nameof(sequence))
                : sequence.Length <= 0
                    ? throw new ArgumentException("Sequence length must be positive.", nameof(sequence))
                    : (int)sequence.Length,
            false)
    {


        if (sequence.IsSingleSegment)
        {
            sequence.First.Span.CopyTo(_array);
        }
        else
        {
            sequence.CopyTo(_array);
        }
    }
    /// <inheritdoc/>
    public RentedBuffer(int length)
        : this(length <= 0
            ? throw new ArgumentException("Length must be positive.", nameof(length))
            : length, true)
    {
    }
    /// <summary>
    /// Rented Buffer 소멸자
    /// </summary>
    ~RentedBuffer()
    {
        Dispose(false);
    }
    /// <inheritdoc/>
    public Memory<T> ReadMemory(Range range)
    {

        ThrowIfDisposed();

        var segment = this[range];

        return new Memory<T>(segment.Array!, segment.Offset, segment.Count);
    }
    private void CopyDataInternal(ReadOnlyMemory<T> data, int destinationIndex)
    {
        const int PARALLEL_THRESHOLD = 1024 * 1024;   // 1MB
        const int MIN_SLICE_SIZE = 32 * 1024;        // 32KB minimum chunk size
        lock (_writeLock)
        {
            // 병렬 복사를 위한 적절한 청크 크기 계산
            int maxChunks = Math.Max(1, data.Length / MIN_SLICE_SIZE);
            int maxDegreeOfParallelism = Math.Min(
                Environment.ProcessorCount,
                maxChunks
            );

            // 데이터가 작거나 병렬화가 이점이 없는 경우
            if (data.Length < PARALLEL_THRESHOLD || maxDegreeOfParallelism <= 1)
            {
                data.CopyTo(_array.AsMemory(destinationIndex));
            }
            else
            {
                int sliceSize = (int)Math.Min(
                    Math.Max(MIN_SLICE_SIZE, data.Length / maxDegreeOfParallelism),
                    int.MaxValue
                );

                try
                {
                    int chunks = (int)((long)data.Length + sliceSize - 1) / sliceSize;
                    ParallelLoopResult result = Parallel.For(0, chunks,
                        new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                        i =>
                        {
                            long start = (long)i * sliceSize;
                            if (start < data.Length)
                            {
                                int length = (int)Math.Min(sliceSize, data.Length - start);
                                data.Slice((int)start, length)
                                    .CopyTo(_array.AsMemory(destinationIndex + (int)start));
                            }
                        });

                    if (!result.IsCompleted)
                    {
                        throw new InvalidOperationException("Parallel copy operation was not completed successfully.");
                    }
                }
                catch (AggregateException)
                {
                    throw;
                }
            }
            // 버퍼 길이 업데이트 (오버플로우 방지)
            long newLength = (long)destinationIndex + data.Length;
            _length = (int)Math.Min(Math.Max(_length, newLength), int.MaxValue);
        }
    }
    /// <inheritdoc/>
    public void Write(ReadOnlyMemory<T> data, int destinationIndex = 0, bool isFlush = false)
    {
        ThrowIfDisposed();

        if (!_isWritableBuffer)
        {
            throw new InvalidOperationException("This buffer was initialized as read-only buffer.");
        }

        // 정수 오버플로우 방지를 위한 체크
        if (destinationIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationIndex), "Destination index cannot be negative.");
        }

        if (data.Length > MaxCapacity - destinationIndex)  // 오버플로우 없이 체크
        {
            throw new ArgumentOutOfRangeException(nameof(destinationIndex), "Data exceeds buffer capacity.");
        }

        CopyDataInternal(data, destinationIndex);
        if (isFlush)
        {
            Advance(data.Length);
        }
    }

    /// <inheritdoc/>
    public void WriteAtOffset(ReadOnlyMemory<T> data, bool isFlush = false)
    {
        Write(data, CurrentOffset, isFlush);
    }
    /// <inheritdoc/>
    public void Advance(int bufSize)
    {
        ThrowIfDisposed();
        if (bufSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufSize), "Advance size must be positive.");
        }

        const int maxRetires = 100;

        int retryCount = 0;
        while (retryCount < maxRetires)
        {
            var currentOffset = CurrentOffset;

            if (currentOffset >= MaxCapacity)
            {
                throw new InvalidOperationException("Buffer is at the end.");
            }

            // 읽을 수 있는 바이트 수 계산
            var readableBytes = ReadableBytes;
            if (bufSize > readableBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(bufSize),
                    $"Cannot advance beyond readable bytes. Available: {readableBytes}, Requested: {bufSize}");
            }
            // 오버 플로우 체크
            if (bufSize > MaxCapacity - currentOffset)
            {
                throw new InvalidOperationException("Advancing would cause integer overflow.");
            }

            var newOffset = currentOffset + bufSize;
            if (newOffset > MaxCapacity)
            {
                newOffset = MaxCapacity;
            }
            // offset만 CAS로 업데이트
            if (Interlocked.CompareExchange(ref _offset, newOffset, currentOffset) == currentOffset)
            {
                return;
            }
            retryCount++;
            Thread.SpinWait(1 << Math.Min(retryCount, 30)); // 지수 백오프
        }
        throw new InvalidOperationException("Failed to advance buffer after maximum retries.");
    }
    ///<inheritdoc/>
    public void Reset()
    {
        ThrowIfDisposed();
        lock (_writeLock)
        {
            Volatile.Write(ref _offset, 0);
            Volatile.Write(ref _length, _isWritableBuffer ? 0 : MaxCapacity);
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
    /// <summary>
    /// 자원을 해제합니다.
    /// </summary>
    /// <param name="disposing">관리되는 자원을 해제할지 여부</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
#if DEBUG
            _disposalStack = Environment.StackTrace;
#endif
            if (disposing)
            {
                var array = Interlocked.Exchange(ref _array, null);
                if (array != null)
                {
                    if (!_isTakeOwnership)
                    {
                        ArrayPoolAllocator<T>.Return(array, true);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 관리되는 리소스와 비관리되는 리소스를 해제하고 가비지 수집을 억제합니다.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// Rented Buffer를 Memory로 변환합니다.
    /// </summary>
    /// <param name="instance"></param>
    public static implicit operator ReadOnlyMemory<T>(RentedBuffer<T> instance) => instance.Memory;
    /// <summary>
    /// 디버깅 도우미
    /// </summary>
    /// <returns></returns>
    public string GetDebugInfo()
    {
        StringBuilder builder = new();
        builder.AppendLine("Buffer Info:");
        builder.AppendLine($"- Capacity: {MaxCapacity}");
        builder.AppendLine($"- Current Length: {_length}");
        builder.AppendLine($"- Current Offset: {_offset}");
        builder.AppendLine($"- Is Disposed: {IsDisposed}");
#if DEBUG
        builder.AppendLine($"- Allocation Stack: {_allocationStack}");
        builder.AppendLine($"- Disposal Stack: {_disposalStack ?? "Not disposed yet"}");
#endif
        return builder.ToString();
    }
}