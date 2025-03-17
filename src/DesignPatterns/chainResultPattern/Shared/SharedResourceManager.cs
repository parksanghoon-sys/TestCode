using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chainResultPattern.Shared;

// 공유 자원을 관리하는 클래스
public class SharedResourceManager
{
    private static readonly Lazy<SharedResourceManager> _instance = new Lazy<SharedResourceManager>(() => new SharedResourceManager());
    public static SharedResourceManager Instance => _instance.Value;

    // 공유 자원에 대한 락
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    // 공유 데이터 저장소
    private readonly ConcurrentDictionary<string, object> _sharedData = new ConcurrentDictionary<string, object>();

    private ImmutableHashSet<string> _writingResources = ImmutableHashSet<string>.Empty;
    private readonly ReaderWriterLockSlim _resourceSetLock = new ReaderWriterLockSlim();

    private SharedResourceManager() { }

    // 특정 리소스에 대한 세마포어 가져오기 (없으면 생성)
    public SemaphoreSlim GetResourceLock(string resourceKey)
    {
        return _resourceLocks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
    }

    // 공유 데이터 읽기
    public bool TryGetSharedData<T>(string key, out T value)
    {
        if (_sharedData.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    // 공유 데이터 쓰기
    public void SetSharedData<T>(string key, T value)
    {
        _sharedData[key] = value;
    }

    // 리소스 접근 시도 (읽기)
    public async Task<bool> TryAccessResourceForReadingAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        // 현재 쓰기 작업이 진행 중인지 확인
        _resourceSetLock.EnterReadLock();
        try
        {
            if (_writingResources.Contains(resourceKey))
            {
                return false; // 쓰기 작업 중이면 읽기 불가
            }
        }
        finally
        {
            _resourceSetLock.ExitReadLock();
        }

        // 리소스 락 획득 시도
        var resourceLock = GetResourceLock(resourceKey);
        if (await resourceLock.WaitAsync(0, cancellationToken))
        {
            try
            {
                // 락을 얻었으므로 리소스에 접근 가능
                return true;
            }
            finally
            {
                resourceLock.Release();
            }
        }

        return false; // 락을 얻지 못함
    }

    // 리소스 독점 접근 (쓰기)
    public async Task<IDisposable> AccessResourceForWritingAsync(string resourceKey, CancellationToken cancellationToken = default)
    {
        // 쓰기 목록에 리소스 추가
        _resourceSetLock.EnterWriteLock();
        try
        {
            _writingResources = _writingResources.Add(resourceKey);
        }
        finally
        {
            _resourceSetLock.ExitWriteLock();
        }

        // 리소스 락 획득
        var resourceLock = GetResourceLock(resourceKey);
        await resourceLock.WaitAsync(cancellationToken);

        // 리소스 사용 후 해제를 위한 disposable 반환
        return new ResourceAccessDisposer(this, resourceKey, resourceLock);
    }

    // 리소스 해제를 관리하는 내부 클래스
    private class ResourceAccessDisposer : IDisposable
    {
        private readonly SharedResourceManager _manager;
        private readonly string _resourceKey;
        private readonly SemaphoreSlim _resourceLock;
        private bool _disposed = false;

        public ResourceAccessDisposer(SharedResourceManager manager, string resourceKey, SemaphoreSlim resourceLock)
        {
            _manager = manager;
            _resourceKey = resourceKey;
            _resourceLock = resourceLock;
        }

        public void Dispose()
        {
            if (_disposed) return;

            // 쓰기 목록에서 리소스 제거
            _manager._resourceSetLock.EnterWriteLock();
            try
            {
                _manager._writingResources = _manager._writingResources.Remove(_resourceKey);
            }
            finally
            {
                _manager._resourceSetLock.ExitWriteLock();
            }

            // 리소스 락 해제
            _resourceLock.Release();
            _disposed = true;
        }
    }
}



