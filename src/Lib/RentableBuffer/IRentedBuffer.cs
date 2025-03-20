namespace RentableBuffer;
/// <summary>
/// ArrayPool에 할당 받아서 처리하는 Buffer Interface
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRentedBuffer<T> : IDisposable
{
    /// <summary>
    /// 버퍼가 읽기 전용인지 여부를 반환합니다.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// 현재 읽기 위치를 반환합니다.
    /// </summary>
    int ReaderIndex { get; }
    /// <summary>
    /// 현재 쓰기 위치를 반환합니다.
    /// </summary>
    int WriterIndex { get; }

    /// <summary>
    /// 버퍼가 끝에 도달했는지 여부를 반환합니다.
    /// </summary>
    bool IsEndOfBuffer { get; }
    /// <summary>
    /// 버퍼가 쓰기 가능한지 여부를 반환합니다.
    /// </summary>
    bool IsWritable { get; }
    /// <summary>
    /// 읽을 수 있는 메모리가 남아 있는지 확인합니다.
    /// </summary>
    bool IsReadable { get; }
    /// <summary>
    /// 버퍼가 해제되었는지 여부를 반환합니다.
    /// </summary>
    bool IsDisposed { get; }
    /// <summary>
    /// 현재 버퍼의 쓰기 가능한 공간을 반환합니다.
    /// </summary>
    int WritableBytes { get; }
    /// <summary>
    /// 남아있는 메모리의 크기를 반환합니다.
    /// </summary>
    int ReadableBytes { get; }
    /// <summary>
    /// 전체 데이터 크기를 반환합니다.
    /// </summary>
    int MaxCapacity { get; }

    /// <summary>
    /// 현재 사용가능한 읽기 전용 메모리 영역을 반환합니다.
    /// </summary>
    Memory<T> Memory { get; }

    /// <summary>
    /// 요청된 크기만큼 메모리 오프셋을 이동합니다.
    /// </summary>
    /// <param name="bufSize">이동할 크기</param>
    void Advance(int bufSize);

    /// <summary>
    /// 지정된 위치의 메모리를 반환합니다.
    /// </summary>
    /// <param name="range">범위</param>
    /// <returns>메모리 영역</returns>
    Memory<T> ReadMemory(Range range);

    /// <summary>
    /// 메모리를 초기 상태로 되돌립니다.
    /// </summary>
    void Reset();

    /// <summary>
    /// 지정된 위치에 데이터를 씁니다.
    /// </summary>
    /// <param name="data">쓸 데이터</param>
    /// <param name="destinationIndex">쓰기 시작할 위치</param>
    /// <param name="isFlush">자동으로 Advance를 수행 할지 여부</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    void Write(ReadOnlyMemory<T> data, int destinationIndex = 0, bool isFlush = false);

    /// <summary>
    /// 현재 오프셋 위치에 데이터를 씁니다.
    /// </summary>
    /// <param name="data">쓸 데이터</param>
    /// <param name="isFlush">자동으로 Advance를 수행 할지 여부</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    void WriteAtOffset(ReadOnlyMemory<T> data, bool isFlush = false);
    /// <summary>
    /// 특정 영역에 대하여 Array View를 얻습니다.
    /// </summary>
    /// <param name="range">범위</param>
    /// <returns></returns>
    ArraySegment<T> this[Range range] { get; }
    /// <summary>
    /// 배열 접근자
    /// </summary>
    /// <param name="index">인덱스</param>
    /// <returns></returns>
    T this[Index index] { get; set; }
}
