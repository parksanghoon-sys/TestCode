using System.Diagnostics;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


public class TaskVsValueTaskBenchmark
{
    private static readonly Dictionary<int, string> Cache = new();
    private static int cacheHitCount = 0;
    private static int cacheMissCount = 0;

    static async Task Main(string[] args)
    {
        // 캐시 미리 채우기
        for (int i = 0; i < 1000; i++)
        {
            Cache[i] = $"Cached Value {i}";
        }

        Console.WriteLine("=== Task vs ValueTask 성능 비교 ===\n");

        // 1. 메모리 할당 비교
        await CompareMemoryAllocation();

        Console.WriteLine();

        // 2. 성능 벤치마크
        await PerformanceBenchmark();

        Console.WriteLine();

        // 3. 캐시 시나리오 비교
        await CacheScenarioBenchmark();
    }

    // 메모리 할당 비교
    static async Task CompareMemoryAllocation()
    {
        Console.WriteLine("=== 메모리 할당 비교 ===");

        // GC 초기화
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long beforeTask = GC.GetTotalMemory(false);

        // Task 버전 - 1000번 호출
        for (int i = 0; i < 1000; i++)
        {
            await GetCachedDataWithTask(i % 100); // 캐시 히트가 많이 발생
        }

        long afterTask = GC.GetTotalMemory(false);
        long taskMemory = afterTask - beforeTask;

        // GC 다시 초기화
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long beforeValueTask = GC.GetTotalMemory(false);

        // ValueTask 버전 - 1000번 호출
        for (int i = 0; i < 1000; i++)
        {
            await GetCachedDataWithValueTask(i % 100); // 캐시 히트가 많이 발생
        }

        long afterValueTask = GC.GetTotalMemory(false);
        long valueTaskMemory = afterValueTask - beforeValueTask;

        Console.WriteLine($"Task 메모리 사용량: {taskMemory:N0} bytes");
        Console.WriteLine($"ValueTask 메모리 사용량: {valueTaskMemory:N0} bytes");
        Console.WriteLine($"메모리 차이: {taskMemory - valueTaskMemory:N0} bytes ({((double)(taskMemory - valueTaskMemory) / taskMemory * 100):F1}% 절약)");
    }

    // 성능 벤치마크
    static async Task PerformanceBenchmark()
    {
        Console.WriteLine("=== 성능 벤치마크 (100,000회 호출) ===");

        const int iterations = 100_000;

        // Task 성능 측정
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await GetCachedDataWithTask(i % 100);
        }
        sw.Stop();
        var taskTime = sw.ElapsedMilliseconds;

        // ValueTask 성능 측정
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            await GetCachedDataWithValueTask(i % 100);
        }
        sw.Stop();
        var valueTaskTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Task 실행 시간: {taskTime:N0} ms");
        Console.WriteLine($"ValueTask 실행 시간: {valueTaskTime:N0} ms");
        Console.WriteLine($"성능 향상: {((double)(taskTime - valueTaskTime) / taskTime * 100):F1}%");
        Console.WriteLine($"ValueTask가 {((double)taskTime / valueTaskTime):F1}배 빠름");
    }

    // 캐시 시나리오별 비교
    static async Task CacheScenarioBenchmark()
    {
        Console.WriteLine("=== 캐시 히트율별 성능 비교 ===");

        // 90% 캐시 히트율
        await BenchmarkWithCacheHitRate(0.9, "90% 캐시 히트");

        // 50% 캐시 히트율  
        await BenchmarkWithCacheHitRate(0.5, "50% 캐시 히트");

        // 10% 캐시 히트율
        await BenchmarkWithCacheHitRate(0.1, "10% 캐시 히트");
    }

    static async Task BenchmarkWithCacheHitRate(double hitRate, string scenario)
    {
        Console.WriteLine($"\n--- {scenario} ---");

        const int iterations = 10_000;
        var random = new Random(42); // 일관된 결과를 위해 시드 고정

        // Task 벤치마크
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            int key = random.NextDouble() < hitRate ? i % 100 : i + 10000; // 캐시 히트/미스 제어
            await GetCachedDataWithTask(key);
        }
        sw.Stop();
        var taskTime = sw.ElapsedMilliseconds;

        // Random 재설정
        random = new Random(42);

        // ValueTask 벤치마크
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            int key = random.NextDouble() < hitRate ? i % 100 : i + 10000; // 동일한 패턴
            await GetCachedDataWithValueTask(key);
        }
        sw.Stop();
        var valueTaskTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Task: {taskTime} ms, ValueTask: {valueTaskTime} ms");
        Console.WriteLine($"성능 차이: {((double)(taskTime - valueTaskTime) / taskTime * 100):F1}%");
    }

    // Task 버전 - 캐시가 있어도 항상 Task 객체 생성
    static async Task<string> GetCachedDataWithTask(int key)
    {
        if (Cache.TryGetValue(key, out string cached))
        {
            cacheHitCount++;
            return cached; // Task<string> 객체가 힙에 할당됨
        }

        cacheMissCount++;
        // 실제 비동기 작업 시뮬레이션
        await Task.Delay(1);
        var result = $"Fresh Data {key}";
        Cache[key] = result;
        return result;
    }

    // ValueTask 버전 - 캐시가 있으면 할당 없음
    static async ValueTask<string> GetCachedDataWithValueTask(int key)
    {
        if (Cache.TryGetValue(key, out string cached))
        {
            cacheHitCount++;
            return cached; // 힙 할당 없음!
        }

        cacheMissCount++;
        // 실제 비동기 작업 시뮬레이션
        await Task.Delay(1);
        var result = $"Fresh Data {key}";
        Cache[key] = result;
        return result;
    }
}

// 추가 예제: ConfigureAwait와 함께 사용
public class ConfigureAwaitExample
{
    public static async ValueTask<string> OptimizedApiCall(string url)
    {
        // 로컬 캐시 확인
        if (LocalCache.TryGet(url, out string cachedResult))
        {
            return cachedResult; // 동기 완료, 할당 없음
        }

        // 네트워크 호출
        using var client = new System.Net.Http.HttpClient();
        var response = await client.GetStringAsync(url).ConfigureAwait(false);

        LocalCache.Set(url, response);
        return response;
    }

    // Task로 구현했다면 캐시 히트 시에도 Task 객체 생성
    public static async Task<string> RegularApiCall(string url)
    {
        if (LocalCache.TryGet(url, out string cachedResult))
        {
            return cachedResult; // Task<string> 객체 생성됨
        }

        using var client = new System.Net.Http.HttpClient();
        var response = await client.GetStringAsync(url).ConfigureAwait(false);

        LocalCache.Set(url, response);
        return response;
    }
}

public static class LocalCache
{
    private static readonly Dictionary<string, string> _cache = new();

    public static bool TryGet(string key, out string value)
        => _cache.TryGetValue(key, out value);

    public static void Set(string key, string value)
        => _cache[key] = value;
}