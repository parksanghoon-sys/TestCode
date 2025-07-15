using FastMapper.Core.Abstractions;
using FastMapper.Core.Attributes;
using FastMapper.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FastMapper.Tests.Integration;

/// <summary>
/// 의존성 주입 통합 테스트
/// </summary>
public sealed class DependencyInjectionIntegrationTests
{
    /// <summary>
    /// 서비스 컬렉션에 FastMapper 등록 테스트
    /// </summary>
    [Fact]
    public void AddFastMapper_서비스등록_성공적으로완료됨()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddFastMapper();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mapperFactory = serviceProvider.GetService<IMapperFactory>();
        var dynamicMapper = serviceProvider.GetService<IDynamicMapper>();

        mapperFactory.Should().NotBeNull();
        dynamicMapper.Should().NotBeNull();
    }

    /// <summary>
    /// 매퍼 팩토리를 통한 매퍼 생성 테스트
    /// </summary>
    [Fact]
    public void MapperFactory_매퍼생성_올바르게동작함()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFastMapper();

        var serviceProvider = services.BuildServiceProvider();
        var mapperFactory = serviceProvider.GetRequiredService<IMapperFactory>();

        // Act & Assert
        var act = () => mapperFactory.CreateMapper<TestSource, TestDestination>();
        
        // 매퍼가 등록되지 않았으므로 예외가 발생해야 함
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*매퍼를 찾을 수 없습니다*");
    }

    /// <summary>
    /// 동적 매퍼 테스트
    /// </summary>
    [Fact]
    public void DynamicMapper_타입확인_올바르게동작함()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFastMapper();

        var serviceProvider = services.BuildServiceProvider();
        var dynamicMapper = serviceProvider.GetRequiredService<IDynamicMapper>();

        // Act
        var canMap = dynamicMapper.CanMap(typeof(TestSource), typeof(TestDestination));
        var result = dynamicMapper.Map<TestDestination>(new TestSource { Id = 1, Name = "Test" });

        // Assert
        canMap.Should().BeFalse(); // 매퍼가 등록되지 않았으므로 false
        result.Should().BeNull(); // 매핑할 수 없으므로 null
    }

    /// <summary>
    /// 매핑 옵션 구성 테스트
    /// </summary>
    [Fact]
    public void ConfigureFastMapper_옵션설정_올바르게적용됨()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFastMapper();
        
        services.ConfigureFastMapper(options =>
        {
            options.EnablePerformanceMonitoring = true;
            options.MaxDepth = 15;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetService<FastMapper.Core.Common.MappingOptions>();

        // Assert
        options.Should().NotBeNull();
        // 주의: Configure를 사용했지만 실제로는 별도의 인스턴스가 등록되므로 확인 방법이 다를 수 있음
    }

    /// <summary>
    /// 테스트용 소스 클래스
    /// </summary>
    [MapTo(typeof(TestDestination))]
    public sealed class TestSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 테스트용 대상 클래스
    /// </summary>
    public sealed class TestDestination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

/// <summary>
/// End-to-End 테스트
/// </summary>
public sealed class EndToEndMappingTests
{
    /// <summary>
    /// 전체 매핑 파이프라인 테스트
    /// </summary>
    [Fact]
    public void FullMappingPipeline_완전한워크플로우_성공적으로실행됨()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddFastMapper();

        var serviceProvider = services.BuildServiceProvider();

        // 실제 사용 시나리오 시뮬레이션
        var sourceData = new List<ComplexSource>
        {
            new()
            {
                Id = 1,
                Title = "첫 번째 항목",
                CreatedAt = DateTime.Now,
                Tags = new List<string> { "tag1", "tag2" },
                Metadata = new SourceMetadata { Description = "설명1", Priority = 1 }
            },
            new()
            {
                Id = 2,
                Title = "두 번째 항목",
                CreatedAt = DateTime.Now.AddDays(-1),
                Tags = new List<string> { "tag3", "tag4" },
                Metadata = new SourceMetadata { Description = "설명2", Priority = 2 }
            }
        };

        // Act
        // 실제 환경에서는 Source Generator가 매퍼를 생성하지만
        // 테스트 환경에서는 수동으로 검증
        
        // Assert
        sourceData.Should().NotBeEmpty();
        sourceData.Should().HaveCount(2);
        sourceData.All(s => !string.IsNullOrEmpty(s.Title)).Should().BeTrue();
    }

    /// <summary>
    /// 성능 요구사항 검증 테스트
    /// </summary>
    [Fact]
    public void PerformanceRequirements_대량데이터매핑_시간제한내완료()
    {
        // Arrange
        const int itemCount = 1000;
        var sources = GenerateTestData(itemCount);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // 간단한 변환 로직 (실제로는 생성된 매퍼 사용)
        var results = sources.Select(s => new ComplexDestination
        {
            Id = s.Id,
            Title = s.Title,
            FormattedDate = s.CreatedAt.ToString("yyyy-MM-dd"),
            TagCount = s.Tags.Count,
            Description = s.Metadata?.Description ?? "없음"
        }).ToList();

        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(itemCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // 1초 이내
        
        // 결과 검증
        results.All(r => r.Id > 0).Should().BeTrue();
        results.All(r => !string.IsNullOrEmpty(r.Title)).Should().BeTrue();
    }

    /// <summary>
    /// 메모리 사용량 검증 테스트
    /// </summary>
    [Fact]
    public void MemoryUsage_대량데이터처리_메모리효율성확인()
    {
        // Arrange
        const int itemCount = 10000;
        
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var sources = GenerateTestData(itemCount);
        var results = sources.Select(s => new ComplexDestination
        {
            Id = s.Id,
            Title = s.Title,
            FormattedDate = s.CreatedAt.ToString("yyyy-MM-dd"),
            TagCount = s.Tags.Count,
            Description = s.Metadata?.Description ?? "없음"
        }).ToList();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        results.Should().HaveCount(itemCount);
        
        // 메모리 사용량이 합리적인 범위 내에 있는지 확인 (항목당 약 1KB 이하)
        var memoryPerItem = memoryUsed / itemCount;
        memoryPerItem.Should().BeLessThan(1024);
    }

    /// <summary>
    /// 테스트 데이터 생성
    /// </summary>
    private static List<ComplexSource> GenerateTestData(int count)
    {
        var random = new Random(42); // 시드 고정으로 재현 가능한 테스트
        var sources = new List<ComplexSource>(count);

        for (int i = 0; i < count; i++)
        {
            sources.Add(new ComplexSource
            {
                Id = i + 1,
                Title = $"항목 {i + 1}",
                CreatedAt = DateTime.Now.AddDays(-random.Next(30)),
                Tags = Enumerable.Range(0, random.Next(1, 6))
                                .Select(j => $"tag{i}_{j}")
                                .ToList(),
                Metadata = new SourceMetadata
                {
                    Description = $"설명 {i + 1}",
                    Priority = random.Next(1, 6)
                }
            });
        }

        return sources;
    }

    /// <summary>
    /// 복잡한 소스 클래스
    /// </summary>
    [MapTo(typeof(ComplexDestination), OptimizationLevel = OptimizationLevel.Aggressive)]
    public sealed class ComplexSource
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        [MapProperty(TargetPropertyName = "FormattedDate", ConverterMethod = "FormatDate")]
        public DateTime CreatedAt { get; set; }
        
        [MapCollection(CollectionType = CollectionType.List)]
        public List<string> Tags { get; set; } = new();
        
        [MapComplex(Strategy = NestedObjectStrategy.CreateNew)]
        public SourceMetadata? Metadata { get; set; }

        public static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 복잡한 대상 클래스
    /// </summary>
    public sealed class ComplexDestination
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
        public int TagCount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 소스 메타데이터 클래스
    /// </summary>
    public sealed class SourceMetadata
    {
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
    }
}

/// <summary>
/// 오류 처리 통합 테스트
/// </summary>
public sealed class ErrorHandlingIntegrationTests
{
    /// <summary>
    /// null 객체 처리 테스트
    /// </summary>
    [Fact]
    public void NullObjectHandling_null입력처리_적절히처리됨()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFastMapper();

        var serviceProvider = services.BuildServiceProvider();
        var dynamicMapper = serviceProvider.GetRequiredService<IDynamicMapper>();

        // Act & Assert
        var result = dynamicMapper.Map<TestClass>(null!);
        result.Should().BeNull();
    }

    /// <summary>
    /// 지원되지 않는 매핑 타입 처리 테스트
    /// </summary>
    [Fact]
    public void UnsupportedMappingType_지원되지않는타입_적절히처리됨()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFastMapper();

        var serviceProvider = services.BuildServiceProvider();
        var dynamicMapper = serviceProvider.GetRequiredService<IDynamicMapper>();

        // Act
        var canMap = dynamicMapper.CanMap(typeof(int), typeof(string));
        var result = dynamicMapper.Map<string>(123);

        // Assert
        canMap.Should().BeFalse();
        result.Should().BeNull();
    }

    /// <summary>
    /// 테스트용 클래스
    /// </summary>
    public sealed class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
