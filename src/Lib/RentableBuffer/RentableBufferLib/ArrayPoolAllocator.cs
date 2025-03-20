using System.Buffers;

namespace RentableBuffer;
/// <summary>
/// 배열 풀을 관리하는 제네릭 정적 클래스입니다.
/// 크기에 따라 두 가지 풀(작은 배열용, 큰 배열용)을 사용하여 메모리 할당을 최적화합니다.
/// </summary>
/// <typeparam name="T">배열에 저장될 요소의 타입</typeparam>
/// <remarks>
/// 이 클래스는 다음과 같은 특징을 가집니다:
/// <list type="bullet">
/// <item>작은 배열(≤1024)과 큰 배열(>1024)에 대해 서로 다른 풀을 사용</item>
/// <item>빈 배열에 대해 공유된 단일 인스턴스 제공</item>
/// <item>배열의 대여(Rent)와 반환(Return) 기능 지원</item>
/// </list>
/// </remarks>
public static class ArrayPoolAllocator<T>
{
    /// <summary>
    /// 비어 있는 배열을 나타내는 정적 읽기 전용 필드입니다.
    /// Array.Empty{T}()를 사용하여 타입 안전한 빈 배열을 제공합니다.
    /// 이 배열은 공유되며 불변이므로 메모리를 효율적으로 사용할 수 있습니다.
    /// </summary>

    public static readonly T[] Empty = Array.Empty<T>();
    /// <summary>
    /// 작은 배열 풀에서 관리할 수 있는 최대 배열 길이입니다.
    /// 이 크기를 초과하는 배열은 공유 풀(LargePool)에서 관리됩니다.
    /// </summary>
    private const int SmallPoolMaxLength = 1024;
    /// <summary>
    /// 작은 배열 풀의 각 버킷당 최대 배열 개수입니다.
    /// 메모리 사용량을 제한하면서도 효율적인 재사용을 가능하게 합니다.
    /// </summary>
    private const int SmallPoolMaxArraysPerBucket = 50;
    /// <summary>
    /// 작은 크기의 배열을 관리하는 전용 풀입니다.
    /// 제한된 크기와 버킷당 배열 수를 가지며, 자주 사용되는 작은 배열의 재사용을 최적화합니다.
    /// </summary>
    private static readonly ArrayPool<T> SmallPool = ArrayPool<T>.Create(maxArrayLength: SmallPoolMaxLength, maxArraysPerBucket: SmallPoolMaxArraysPerBucket);
    /// <summary>
    /// 큰 크기의 배열(>1024)을 관리하는 전역 공유 풀입니다.
    /// .NET의 기본 ArrayPool을 사용하여 큰 배열의 할당을 관리합니다.
    /// </summary>
    private static readonly ArrayPool<T> LargePool = ArrayPool<T>.Shared;
    /// <summary>
    /// 지정된 길이의 배열을 풀에서 대여합니다.
    /// </summary>
    /// <param name="length">필요한 배열의 길이</param>
    /// <returns>
    /// - length가 0인 경우: Empty 배열 반환
    /// - length가 1024 이하인 경우: SmallPool에서 배열 대여
    /// - length가 1024 초과인 경우: LargePool에서 배열 대여
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">length가 0보다 작은 경우 발생</exception>
    public static T[] Rent(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive");
        // 길이가 0인 경우 Empty 배열 반환.
        return length == 0 ? Empty : length <= SmallPoolMaxLength ? SmallPool.Rent(length) : LargePool.Rent(length);
    }
    /// <summary>
    /// 대여한 배열을 해당하는 풀로 반환합니다.
    /// </summary>
    /// <param name="array">반환할 배열</param>
    /// <param name="clearArray">
    /// true인 경우 반환 전에 배열의 모든 요소를 기본값으로 초기화합니다.
    /// 민감한 데이터를 다룰 때 이 옵션을 사용하세요.
    /// </param>
    /// <exception cref="ArgumentNullException">array가 null인 경우 발생</exception>
    /// <remarks>
    /// - Empty 배열이나 길이가 0인 배열은 풀로 반환되지 않습니다.
    /// - 배열은 원래 대여된 풀(SmallPool 또는 LargePool)로 반환됩니다.
    /// </remarks>
    public static void Return(T[] array, bool clearArray)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        // Empty 배열은 반환하지 않음
        if (array.Length == 0 || ReferenceEquals(array, Empty))
            return;
        var pool = array.Length <= SmallPoolMaxLength ? SmallPool : LargePool;
        pool.Return(array, clearArray);
    }
}
