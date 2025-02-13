namespace Publisher
{
    // 속성 변경 이벤트 인터페이스
    public interface ISubscriptionService : IDisposable
    {
        Task NotifySubscribersAsync(Type type, string propertyName, object newValue);
    }
}
