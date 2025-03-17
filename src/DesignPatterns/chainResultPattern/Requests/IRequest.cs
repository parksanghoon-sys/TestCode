
// 기본 요청 인터페이스
public interface IRequest<TResponse>
{
    string RequestName { get; }
    Guid RequestId { get; }
}
