public class ThirdRequest : IRequest<ThirdResponse>
{
    public string RequestName => "세 번째 요청";
    public Guid RequestId { get; } = Guid.NewGuid();
    public Guid Parameter { get; set; }
}
