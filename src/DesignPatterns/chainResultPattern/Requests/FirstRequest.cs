
// 샘플 요청 구현
public class FirstRequest : IRequest<FirstResponse>
{
    public string RequestName => "첫 번째 요청";
    public Guid RequestId { get; } = Guid.NewGuid();
    public string Parameter { get; set; }
}
