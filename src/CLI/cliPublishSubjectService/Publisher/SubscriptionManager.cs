using System.Collections.Concurrent;
using System.ComponentModel;

namespace Publisher
{
    public class SubscriptionManager
    {
        private readonly ConcurrentDictionary<object, ISubscriptionService> _subscriptions = new();

        public SubscriptionService<T> CreateSubscription<T>(T target) where T : class,INotifyPropertyChanged
        {
            return (SubscriptionService<T>)_subscriptions.GetOrAdd(target, _ => new SubscriptionService<T>(target));
        }
        public void RemoveSubscription<T>(T target) where T : class, INotifyPropertyChanged
        {
            if (_subscriptions.TryRemove(target, out var service))
            {
                service.Dispose();
            }
        }
    }
}
