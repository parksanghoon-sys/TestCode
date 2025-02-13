using System.Collections.Concurrent;
using System.ComponentModel;

namespace Publisher
{
    public class SubscriptionService<T> : ISubscriptionService where T : class, INotifyPropertyChanged
    {
        private readonly WeakReference<T> _targetReference;
        private readonly ConcurrentDictionary<Type, ConcurrentBag<WeakReference<Func<string, object, Task>>>> _subscribers = new();
        private readonly object _lock = new();
        private int _disposed; // 0: false, 1: true
        public SubscriptionService(T target)
        {
            _targetReference = new(target);
            target.PropertyChanged += OnPropertyChanged;
        }
        public void Subscribe<U>(Func<string, object, Task> asyncHandler) where U : T
        {
            var type = typeof(U);
                lock(_lock)
            {
                if (_disposed == 1) return;
                if(_subscribers.ContainsKey(type) == false)
                {
                    _subscribers[type] = new ConcurrentBag<WeakReference<Func<string, object, Task>>>();
                }
                _subscribers[type].Add(new WeakReference<Func<string, object, Task>>(asyncHandler));
            }
        }
        public void Unsubscribe<U>(Func<string, object, Task> asyncHandler) where U : T
        {
            var type = typeof(U);
            lock (_lock)
            {
                if (_subscribers.TryGetValue(type, out var handlers))
                {
                    handlers = new ConcurrentBag<WeakReference<Func<string, object, Task>>>(
                        handlers.Where(wr => !wr.TryGetTarget(out var target) || target != asyncHandler)
                    );
                    _subscribers[type] = handlers;
                }
            }
        }
        private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(_targetReference.TryGetTarget(out var target))
            {
                var type = target.GetType();
                var properties = type.GetProperty(e.PropertyName);
                var newValue = properties?.GetValue(target);
                await NotifySubscribersAsync(type, e.PropertyName, newValue);
            }
        }     
        public async Task NotifySubscribersAsync(Type type, string propertyName, object newValue)
        {
            if(_subscribers.ContainsKey(type) == false) return;
            List<Func<string, object, Task>> validSubscribers = new();

            lock (_lock)
            {
                foreach (var weakRef in _subscribers[type])
                {
                    if (weakRef.TryGetTarget(out var subscriber))
                    {
                        validSubscribers.Add(subscriber);
                    }
                }
            }
            await Task.WhenAll(validSubscribers.Select(subscriber => subscriber(propertyName, newValue)));
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

            if (_targetReference.TryGetTarget(out var target))
            {
                target.PropertyChanged -= OnPropertyChanged;
            }

            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }
}
