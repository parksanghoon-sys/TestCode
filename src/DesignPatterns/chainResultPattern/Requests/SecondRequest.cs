public class SecondRequest : IRequest<SecondResponse>
{
    public string RequestName => "두 번째 요청";
    public Guid RequestId { get; } = Guid.NewGuid();
    public int Parameter { get; set; }
}
