using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace LockFreeExample
{
    // 공유 자원을 표현하는 클래스
    public class SharedResource
    {
        // Interlocked 클래스를 사용하여 원자적 연산을 수행할 수 있는 카운터
        private long _counter;

        // 비동기 작업의 결과를 저장하는 ConcurrentQueue (lock-free 컬렉션)
        private readonly ConcurrentQueue<ProcessingResult> _results = new ConcurrentQueue<ProcessingResult>();

        // 마지막 업데이트 시간을 추적하는 필드 (ticks로 저장)
        private long _lastUpdateTimeTicks = DateTime.MinValue.Ticks;

        // 원자적 카운터 값 업데이트
        public long IncrementCounter()
        {
            return Interlocked.Increment(ref _counter);
        }

        // 원자적 값 가져오기
        public long GetCounter()
        {
            return Interlocked.Read(ref _counter);
        }

        // 결과 큐에 결과 추가
        public void AddResult(ProcessingResult result)
        {
            _results.Enqueue(result);
            // DateTime.Ticks를 long 값으로 원자적으로 업데이트
            Interlocked.Exchange(ref _lastUpdateTimeTicks, DateTime.Now.Ticks);
        }

        // 모든 결과를 가져옴
        public IEnumerable<ProcessingResult> GetResults()
        {
            return _results.ToArray();
        }

        // 마지막 업데이트 시간 가져오기
        public DateTime GetLastUpdateTime()
        {
            return new DateTime(Interlocked.Read(ref _lastUpdateTimeTicks));
        }

        // 원자적 비교 및 교환 연산 예제
        public bool TryUpdateCounter(long expectedValue, long newValue)
        {
            return Interlocked.CompareExchange(ref _counter, newValue, expectedValue) == expectedValue;
        }
    }

    // 처리 결과를 나타내는 불변 클래스
    public class ProcessingResult
    {
        public long TaskId { get; }
        public long Value { get; }
        public DateTime ProcessedTime { get; }

        public ProcessingResult(long taskId, long value)
        {
            TaskId = taskId;
            Value = value;
            ProcessedTime = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Task {TaskId}: {Value} processed at {ProcessedTime}";
        }
    }

    // 공유 자원을 사용하는 작업 관리자
    public class LongTaskManager
    {
        private readonly SharedResource _sharedResource;
        private readonly int _taskCount;
        private readonly int _operationsPerTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public LongTaskManager(SharedResource sharedResource, int taskCount, int operationsPerTask)
        {
            _sharedResource = sharedResource;
            _taskCount = taskCount;
            _operationsPerTask = operationsPerTask;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        // 여러 개의 장기 실행 태스크 시작
        public async Task StartLongRunningTasksAsync()
        {
            Console.WriteLine("Starting long-running tasks...");
            var tasks = new List<Task>();

            for (int i = 0; i < _taskCount; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(() => ProcessLongRunningTask(taskId, _cancellationTokenSource.Token)));
            }

            // 모든 태스크가 완료될 때까지 기다림
            await Task.WhenAll(tasks);
            Console.WriteLine("All tasks completed.");
        }

        // 개별 장기 실행 태스크
        private async Task ProcessLongRunningTask(int taskId, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Task {taskId} started.");
            Random random = new Random(taskId);

            for (int i = 0; i < _operationsPerTask && !cancellationToken.IsCancellationRequested; i++)
            {
                // 비즈니스 로직 시뮬레이션 (CPU 집약적인 작업)
                long value = PerformComplexCalculation(taskId, i, random);

                // 공유 자원에 대한 업데이트 (임계 영역을 최소화함)
                UpdateSharedResourceWithResult(taskId, value);

                // 임의의 지연을 추가하여 다양한 타이밍 시나리오 시뮬레이션
                await Task.Delay(random.Next(50, 150), cancellationToken);
            }

            Console.WriteLine($"Task {taskId} completed.");
        }

        // CPU 집약적인 계산 시뮬레이션
        private long PerformComplexCalculation(int taskId, int iteration, Random random)
        {
            // 시간이 오래 걸리는 계산을 시뮬레이션
            // 이 부분은 임계 영역 밖에 있어 다른 스레드의 실행에 영향을 주지 않음
            long result = taskId * 1000 + iteration;

            // 일부 복잡한 계산 시뮬레이션
            for (int i = 0; i < random.Next(1000, 5000); i++)
            {
                result = (result * 31 + i) % 1000000007;
            }

            return result;
        }

        // 공유 자원 업데이트 (임계 영역)
        private void UpdateSharedResourceWithResult(int taskId, long value)
        {
            // 원자적 카운터 증가 - 락 없이 수행됨
            long currentCount = _sharedResource.IncrementCounter();

            // 결과 생성 및 대기열에 추가 - ConcurrentQueue는 lock-free 자료구조임
            var result = new ProcessingResult(taskId, value);
            _sharedResource.AddResult(result);

            // 낙관적 병행성 제어 패턴을 사용한 조건부 업데이트 예시
            // 특정 조건에서만 카운터 값을 수정하려는 경우
            if (value % 100 == 0)
            {
                // 이 작업은 원자적으로 수행되며, 실패하면 false를 반환
                bool updated = false;
                int retries = 0;
                const int MAX_RETRIES = 3;

                while (!updated && retries < MAX_RETRIES)
                {
                    long expectedValue = _sharedResource.GetCounter();
                    long newValue = expectedValue + 1000;
                    updated = _sharedResource.TryUpdateCounter(expectedValue, newValue);
                    retries++;
                }
            }
        }

        // 태스크 취소
        public void CancelTasks()
        {
            _cancellationTokenSource.Cancel();
            Console.WriteLine("Cancellation requested for all tasks.");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // 공유 자원 생성
            var sharedResource = new SharedResource();

            // 태스크 관리자 생성
            var taskManager = new LongTaskManager(sharedResource, taskCount: 5, operationsPerTask: 100);

            // 모니터링 태스크 시작
            var monitoringTask = Task.Run(() => MonitorSharedResource(sharedResource));

            // 장기 실행 태스크 시작
            var processingTask = taskManager.StartLongRunningTasksAsync();

            // 모든 작업이 완료될 때까지 대기
            await processingTask;
            taskManager.CancelTasks();

       

            // 최종 통계 출력
            Console.WriteLine("\nFinal statistics:");
            Console.WriteLine($"Total operations: {sharedResource.GetCounter()}");
            Console.WriteLine($"Last update time: {sharedResource.GetLastUpdateTime()}");

            var results = sharedResource.GetResults();
            int resultCount = 0;
            foreach (var result in results)
            {
                resultCount++;
            }
            Console.WriteLine($"Total results: {resultCount}");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        // 공유 자원 모니터링
        static void MonitorSharedResource(SharedResource resource)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < 6000)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Current count: {resource.GetCounter()}, " +
                                 $"Last update: {resource.GetLastUpdateTime():HH:mm:ss.fff}");
                Thread.Sleep(500);
            }
        }
    }
}