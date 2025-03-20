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
public static class ArrayPoolAllocator
{

}
