using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DeadlockExample
{
    class Program
    {
        // 공유 자원을 나타내는 클래스들
        public class Resource
        {
            public int Id { get; }
            public string Name { get; }

            public Resource(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString()
            {
                return $"Resource {Id}: {Name}";
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== 교착상태 예제 시작 ===");

            // 1. 교착상태 발생 예제
            Console.WriteLine("\n[1] 교착상태 발생 데모:");
            //CreateDeadlock();

            // 2. 교착상태 예방 예제
            Console.WriteLine("\n[2] 교착상태 예방 데모:");
            PreventDeadlock();

            Console.WriteLine("\n=== 예제 종료 ===");
            Console.ReadKey();
        }

        /// <summary>
        /// 교착상태를 발생시키는 예제
        /// </summary>
        static void CreateDeadlock()
        {
            // 두 개의 자원 생성
            var resourceA = new Resource(1, "Database Connection");
            var resourceB = new Resource(2, "File Handle");

            Console.WriteLine("두 개의 스레드가 서로 다른 순서로 두 자원을 요청하여 교착상태 발생");

            // 교착상태 조건 1: 상호 배제 (Mutual Exclusion)
            // 각 자원은 한 번에 하나의 스레드만 접근 가능하도록 락 사용
            object lockA = new object();
            object lockB = new object();

            // 타임아웃을 설정하여 교착상태 발생 시 감지
            var deadlockDetected = new ManualResetEventSlim(false);
            var timeout = TimeSpan.FromSeconds(3);
            var stopwatch = Stopwatch.StartNew();

            // 스레드 1 시작
            var thread1 = Task.Run(() =>
            {
                Console.WriteLine("스레드 1: 시작됨");

                try
                {
                    // 교착상태 조건 2: 점유와 대기 (Hold and Wait)
                    // 자원 A를 점유한 상태에서 자원 B를 대기
                    Console.WriteLine("스레드 1: 자원 A 획득 시도");
                    lock (lockA)
                    {
                        Console.WriteLine("스레드 1: 자원 A 획득 완료");
                        // 잠시 대기하여 스레드 2가 자원 B를 획득할 시간을 줌
                        Thread.Sleep(100);

                        // 교착상태 조건 3: 비선점 (No Preemption)
                        // 자원 B를 획득하려고 시도하나, 이미 스레드 2가 점유 중이라면 대기
                        Console.WriteLine("스레드 1: 자원 B 획득 시도");

                        if (Monitor.TryEnter(lockB, timeout))
                        {
                            try
                            {
                                Console.WriteLine("스레드 1: 자원 B 획득 완료");
                                Console.WriteLine("스레드 1: 두 자원으로 작업 수행 중...");
                                Thread.Sleep(100);
                            }
                            finally
                            {
                                Monitor.Exit(lockB);
                                Console.WriteLine("스레드 1: 자원 B 해제");
                            }
                        }
                        else
                        {
                            Console.WriteLine("스레드 1: 자원 B 획득 타임아웃 - 교착상태 감지!");
                            deadlockDetected.Set();
                        }
                    }
                    Console.WriteLine("스레드 1: 자원 A 해제");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"스레드 1 오류: {ex.Message}");
                }
            });

            // 스레드 2 시작
            var thread2 = Task.Run(() =>
            {
                Console.WriteLine("스레드 2: 시작됨");

                try
                {
                    // 교착상태 조건 4: 순환 대기 (Circular Wait)
                    // 스레드 1이 A를 점유하고 B를 기다리는 동안
                    // 스레드 2는 B를 점유하고 A를 기다림 (순환 형성)
                    Console.WriteLine("스레드 2: 자원 B 획득 시도");
                    lock (lockB)
                    {
                        Console.WriteLine("스레드 2: 자원 B 획득 완료");
                        // 잠시 대기하여 교착상태가 발생할 수 있는 조건 형성
                        Thread.Sleep(100);

                        Console.WriteLine("스레드 2: 자원 A 획득 시도");
                        if (Monitor.TryEnter(lockA, timeout))
                        {
                            try
                            {
                                Console.WriteLine("스레드 2: 자원 A 획득 완료");
                                Console.WriteLine("스레드 2: 두 자원으로 작업 수행 중...");
                                Thread.Sleep(100);
                            }
                            finally
                            {
                                Monitor.Exit(lockA);
                                Console.WriteLine("스레드 2: 자원 A 해제");
                            }
                        }
                        else
                        {
                            Console.WriteLine("스레드 2: 자원 A 획득 타임아웃 - 교착상태 감지!");
                            deadlockDetected.Set();
                        }
                    }
                    Console.WriteLine("스레드 2: 자원 B 해제");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"스레드 2 오류: {ex.Message}");
                }
            });

            // 교착상태 감지 또는 작업 완료 대기
            var allCompleted = Task.WhenAll(thread1, thread2).Wait(5000);

            if (deadlockDetected.IsSet || !allCompleted)
            {
                Console.WriteLine($"\n교착상태 감지됨! 경과 시간: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine("교착상태 발생 조건:");
                Console.WriteLine("1. 상호 배제: 각 자원(lockA, lockB)은 한 번에 하나의 스레드만 접근 가능");
                Console.WriteLine("2. 점유와 대기: 각 스레드는 하나의 자원을 점유한 상태에서 다른 자원을 대기");
                Console.WriteLine("3. 비선점: 이미 할당된 자원은 사용이 끝날 때까지 강제로 빼앗을 수 없음");
                Console.WriteLine("4. 순환 대기: 스레드 1은 A->B 순서로, 스레드 2는 B->A 순서로 자원을 요청하여 순환 형성");
            }
            else
            {
                Console.WriteLine("모든 스레드가 정상적으로 완료됨 (교착상태 없음)");
            }
        }

        /// <summary>
        /// 교착상태 예방 기법을 보여주는 예제
        /// </summary>
        static void PreventDeadlock()
        {
            Console.WriteLine("=== 교착상태 예방 기법 ===");

            // 예방 기법 1: 상호 배제 조건 제거 - 자원을 공유 가능하게 설계
            PreventMutualExclusion();

            // 예방 기법 2: 점유와 대기 조건 제거 - 필요한 모든 자원을 한번에 요청
            PreventHoldAndWait();

            // 예방 기법 3: 비선점 조건 제거 - 자원을 선점 가능하게 만들기
            PreventNoPreemption();

            // 예방 기법 4: 순환 대기 조건 제거 - 자원에 순서 부여하기
            PreventCircularWait();
        }

        /// <summary>
        /// 교착상태 예방 기법 1: 상호 배제 조건 제거
        /// </summary>
        static void PreventMutualExclusion()
        {
            Console.WriteLine("\n[예방 기법 1] 상호 배제 조건 제거:");
            Console.WriteLine("- 자원을 여러 스레드가 동시에 사용할 수 있도록 설계");
            Console.WriteLine("- 예: 읽기 전용 데이터는 여러 스레드가 동시에 접근 가능");

            // 상호 배제를 피하는 예제: ReadOnly 컬렉션 사용
            var sharedReadOnlyData = new List<string> { "데이터1", "데이터2", "데이터3" }.AsReadOnly();

            // 여러 스레드가 동시에 읽기 작업 수행
            var tasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    foreach (var item in sharedReadOnlyData)
                    {
                        Console.WriteLine($"스레드 {threadId}: 데이터 '{item}' 읽기 완료");
                        Thread.Sleep(10); // 작업 시뮬레이션
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("모든 스레드가 읽기 전용 데이터에 동시 접근 완료 (교착상태 없음)");
        }

        /// <summary>
        /// 교착상태 예방 기법 2: 점유와 대기 조건 제거
        /// </summary>
        static void PreventHoldAndWait()
        {
            Console.WriteLine("\n[예방 기법 2] 점유와 대기 조건 제거:");
            Console.WriteLine("- 필요한 모든 자원을 한번에 획득하거나, 아예 획득하지 않음");

            var resourceA = new Resource(1, "데이터베이스 연결");
            var resourceB = new Resource(2, "파일 핸들");

            object lockA = new object();
            object lockB = new object();

            // 모든 필요한 자원에 대한 락을 관리하는 클래스
            var resourceManager = new ResourceManager();
            resourceManager.AddResource(lockA, "LockA");
            resourceManager.AddResource(lockB, "LockB");

            // 두 스레드 시작
            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    Console.WriteLine($"스레드 {threadId}: 모든 자원 한번에 획득 시도");

                    // 모든 자원을 한번에 획득하거나 기다림
                    using (resourceManager.AcquireAll())
                    {
                        Console.WriteLine($"스레드 {threadId}: 모든 자원 획득 성공");
                        Console.WriteLine($"스레드 {threadId}: 자원 A와 B로 작업 수행 중...");
                        Thread.Sleep(100); // 작업 시뮬레이션
                    }

                    Console.WriteLine($"스레드 {threadId}: 모든 자원 해제 완료");
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("모든 스레드가 작업 완료 (교착상태 없음)");
        }

        /// <summary>
        /// 교착상태 예방 기법 3: 비선점 조건 제거
        /// </summary>
        static void PreventNoPreemption()
        {
            Console.WriteLine("\n[예방 기법 3] 비선점 조건 제거:");
            Console.WriteLine("- 자원을 점유한 스레드가 다른 자원을 기다려야 할 경우, 이미 점유한 자원을 해제");

            // 자원을 나타내는 객체
            var resourceA = new Resource(1, "네트워크 연결");
            var resourceB = new Resource(2, "메모리 버퍼");

            // 타임스탬프 기반 백오프 전략을 사용하는 잠금 관리자
            var preemptiveLockManager = new PreemptiveLockManager();

            // 두 스레드 시작
            var tasks = new List<Task>();
            for (int i = 0; i < 2; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // 첫 번째 자원 획득 시도
                        Resource firstResource = threadId == 0 ? resourceA : resourceB;
                        Resource secondResource = threadId == 0 ? resourceB : resourceA;

                        Console.WriteLine($"스레드 {threadId}: {firstResource.Name} 획득 시도");
                        if (preemptiveLockManager.TryAcquire(firstResource.Id, threadId))
                        {
                            Console.WriteLine($"스레드 {threadId}: {firstResource.Name} 획득 성공");
                            Thread.Sleep(50); // 작업 시뮬레이션

                            // 두 번째 자원 획득 시도
                            Console.WriteLine($"스레드 {threadId}: {secondResource.Name} 획득 시도");
                            if (preemptiveLockManager.TryAcquire(secondResource.Id, threadId))
                            {
                                Console.WriteLine($"스레드 {threadId}: {secondResource.Name} 획득 성공");
                                Console.WriteLine($"스레드 {threadId}: 두 자원으로 작업 수행 중...");
                                Thread.Sleep(100); // 작업 시뮬레이션

                                // 두 번째 자원 해제
                                preemptiveLockManager.Release(secondResource.Id, threadId);
                                Console.WriteLine($"스레드 {threadId}: {secondResource.Name} 해제");
                            }
                            else
                            {
                                // 두 번째 자원을 획득할 수 없는 경우 첫 번째 자원을 해제 (선점 해제)
                                Console.WriteLine($"스레드 {threadId}: {secondResource.Name} 획득 실패, {firstResource.Name} 해제 후 재시도");
                                preemptiveLockManager.Release(firstResource.Id, threadId);

                                // 백오프 후 다시 시도
                                Thread.Sleep(threadId * 30 + 10); // 백오프 시간

                                // 재귀적으로 다시 시도 (실제 코드에서는 무한 재귀를 피하기 위해 카운터 필요)
                                // 여기서는 예시를 단순화하기 위해 생략
                            }
                        }
                        else
                        {
                            Console.WriteLine($"스레드 {threadId}: {firstResource.Name} 획득 실패, 나중에 재시도");
                            // 백오프 후 다시 시도
                            Thread.Sleep(threadId * 30 + 20);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"스레드 {threadId} 오류: {ex.Message}");
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("모든 스레드가 작업 완료 (교착상태 없음)");
        }

        /// <summary>
        /// 교착상태 예방 기법 4: 순환 대기 조건 제거
        /// </summary>
        static void PreventCircularWait()
        {
            Console.WriteLine("\n[예방 기법 4] 순환 대기 조건 제거:");
            Console.WriteLine("- 모든 자원에 전역 순서를 부여하고, 항상 오름차순으로만 자원 요청");

            // 자원 정의 (ID 순서로 정렬됨)
            var resourceA = new Resource(1, "데이터베이스 연결");
            var resourceB = new Resource(2, "파일 핸들");
            var resourceC = new Resource(3, "네트워크 소켓");

            // 자원 ID에 해당하는 락 객체들
            var locks = new Dictionary<int, object>
            {
                { resourceA.Id, new object() },
                { resourceB.Id, new object() },
                { resourceC.Id, new object() }
            };

            // 여러 스레드 시작
            var tasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // 필요한 자원 목록 (각 스레드마다 다른 순서로 필요함)
                        List<Resource> neededResources = new List<Resource>();

                        // 스레드 0은 A, B가 필요
                        // 스레드 1은 B, C가 필요
                        // 스레드 2는 A, C가 필요
                        switch (threadId)
                        {
                            case 0:
                                neededResources.Add(resourceA);
                                neededResources.Add(resourceB);
                                break;
                            case 1:
                                neededResources.Add(resourceB);
                                neededResources.Add(resourceC);
                                break;
                            case 2:
                                neededResources.Add(resourceA);
                                neededResources.Add(resourceC);
                                break;
                        }

                        Console.WriteLine($"스레드 {threadId}: 필요한 자원: {string.Join(", ", neededResources)}");

                        // 핵심: 자원 ID 기준으로 오름차순 정렬하여 항상 같은 순서로 획득
                        neededResources.Sort((x, y) => x.Id.CompareTo(y.Id));
                        Console.WriteLine($"스레드 {threadId}: 자원 획득 순서: {string.Join(", ", neededResources)}");

                        // 정렬된 순서대로 자원 획득
                        foreach (var resource in neededResources)
                        {
                            Console.WriteLine($"스레드 {threadId}: {resource.Name} 획득 시도");
                            Monitor.Enter(locks[resource.Id]);
                            Console.WriteLine($"스레드 {threadId}: {resource.Name} 획득 성공");
                            Thread.Sleep(50); // 작업 시뮬레이션
                        }

                        Console.WriteLine($"스레드 {threadId}: 모든 필요 자원으로 작업 수행 중...");
                        Thread.Sleep(100); // 작업 시뮬레이션

                        // 역순으로 자원 해제 (LIFO 원칙)
                        for (int j = neededResources.Count - 1; j >= 0; j--)
                        {
                            var resource = neededResources[j];
                            Monitor.Exit(locks[resource.Id]);
                            Console.WriteLine($"스레드 {threadId}: {resource.Name} 해제");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"스레드 {threadId} 오류: {ex.Message}");
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("모든 스레드가 순서대로 자원 획득 후 작업 완료 (교착상태 없음)");
        }

        // ======== 도우미 클래스들 ========

        /// <summary>
        /// 자원 관리자 클래스 - 모든 자원을 한번에 획득 (예방 기법 2)
        /// </summary>
        public class ResourceManager
        {
            private readonly Dictionary<object, string> _resources = new Dictionary<object, string>();
            private readonly object _globalLock = new object();

            public void AddResource(object resource, string name)
            {
                lock (_globalLock)
                {
                    _resources[resource] = name;
                }
            }

            public IDisposable AcquireAll()
            {
                // 모든 자원을 획득할 때까지 대기
                bool acquiredAll = false;
                List<object> acquiredResources = new List<object>();

                while (!acquiredAll)
                {
                    lock (_globalLock)
                    {
                        // 모든 자원에 대해 락 획득 시도
                        bool canAcquireAll = true;
                        acquiredResources.Clear();

                        foreach (var resource in _resources.Keys)
                        {
                            if (Monitor.TryEnter(resource))
                            {
                                acquiredResources.Add(resource);
                            }
                            else
                            {
                                canAcquireAll = false;
                                // 획득한 자원 해제
                                foreach (var acquired in acquiredResources)
                                {
                                    Monitor.Exit(acquired);
                                }
                                acquiredResources.Clear();
                                break;
                            }
                        }

                        if (canAcquireAll)
                        {
                            acquiredAll = true;
                        }
                    }

                    if (!acquiredAll)
                    {
                        // 잠시 대기 후 재시도
                        Thread.Sleep(10);
                    }
                }

                // 모든 자원을 해제하는 IDisposable 반환
                return new ResourceDisposer(acquiredResources);
            }

            // 자원 해제를 위한 도우미 클래스
            private class ResourceDisposer : IDisposable
            {
                private readonly List<object> _resources;

                public ResourceDisposer(List<object> resources)
                {
                    _resources = resources;
                }

                public void Dispose()
                {
                    foreach (var resource in _resources)
                    {
                        Monitor.Exit(resource);
                    }
                }
            }
        }

        /// <summary>
        /// 선점 가능한 락 관리자 (예방 기법 3)
        /// </summary>
        public class PreemptiveLockManager
        {
            private readonly Dictionary<int, int> _resourceOwners = new Dictionary<int, int>();
            private readonly Dictionary<int, long> _resourceTimestamps = new Dictionary<int, long>();
            private readonly object _lock = new object();

            public bool TryAcquire(int resourceId, int threadId)
            {
                lock (_lock)
                {
                    // 자원이 이미 점유되어 있는지 확인
                    if (_resourceOwners.TryGetValue(resourceId, out int ownerId))
                    {
                        // 이미 소유하고 있으면 true 반환
                        if (ownerId == threadId)
                        {
                            return true;
                        }

                        // 이미 다른 스레드가 소유 중이면 false 반환
                        return false;
                    }

                    // 자원을 획득하고 타임스탬프 기록
                    _resourceOwners[resourceId] = threadId;
                    _resourceTimestamps[resourceId] = DateTime.Now.Ticks;
                    return true;
                }
            }

            public void Release(int resourceId, int threadId)
            {
                lock (_lock)
                {
                    // 자원의 소유자인 경우에만 해제
                    if (_resourceOwners.TryGetValue(resourceId, out int ownerId) && ownerId == threadId)
                    {
                        _resourceOwners.Remove(resourceId);
                        _resourceTimestamps.Remove(resourceId);
                    }
                }
            }
        }
    }
}