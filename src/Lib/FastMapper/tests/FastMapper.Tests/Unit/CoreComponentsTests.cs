using FastMapper.Core.Attributes;
using FastMapper.Core.Common;
using FluentAssertions;
using Xunit;

namespace FastMapper.Tests.Unit;

/// <summary>
/// 매핑 어트리뷰트 테스트
/// </summary>
public sealed class MappingAttributesTests
{
    /// <summary>
    /// MapTo 어트리뷰트 기본 설정 테스트
    /// </summary>
    [Fact]
    public void MapToAttribute_기본설정_올바르게적용됨()
    {
        // Arrange & Act
        var attribute = new MapToAttribute(typeof(string));

        // Assert
        attribute.TargetType.Should().Be(typeof(string));
        attribute.ProfileName.Should().BeNull();
        attribute.IsBidirectional.Should().BeFalse();
        attribute.OptimizationLevel.Should().Be(OptimizationLevel.Balanced);
    }

    /// <summary>
    /// MapTo 어트리뷰트 커스텀 설정 테스트
    /// </summary>
    [Fact]
    public void MapToAttribute_커스텀설정_올바르게적용됨()
    {
        // Arrange & Act
        var attribute = new MapToAttribute(typeof(int))
        {
            ProfileName = "TestProfile",
            IsBidirectional = true,
            OptimizationLevel = OptimizationLevel.Aggressive
        };

        // Assert
        attribute.TargetType.Should().Be(typeof(int));
        attribute.ProfileName.Should().Be("TestProfile");
        attribute.IsBidirectional.Should().BeTrue();
        attribute.OptimizationLevel.Should().Be(OptimizationLevel.Aggressive);
    }

    /// <summary>
    /// MapProperty 어트리뷰트 기본 설정 테스트
    /// </summary>
    [Fact]
    public void MapPropertyAttribute_기본설정_올바르게적용됨()
    {
        // Arrange & Act
        var attribute = new MapPropertyAttribute();

        // Assert
        attribute.TargetPropertyName.Should().BeNull();
        attribute.Ignore.Should().BeFalse();
        attribute.ConverterMethod.Should().BeNull();
        attribute.ConditionMethod.Should().BeNull();
        attribute.DefaultValue.Should().BeNull();
        attribute.ValidatorMethod.Should().BeNull();
    }

    /// <summary>
    /// MapProperty 어트리뷰트 대상 속성명 설정 테스트
    /// </summary>
    [Fact]
    public void MapPropertyAttribute_대상속성명설정_올바르게적용됨()
    {
        // Arrange & Act
        var attribute = new MapPropertyAttribute("TargetProperty");

        // Assert
        attribute.TargetPropertyName.Should().Be("TargetProperty");
    }

    /// <summary>
    /// MapCollection 어트리뷰트 기본 설정 테스트
    /// </summary>
    [Fact]
    public void MapCollectionAttribute_기본설정_올바르게적용됨()
    {
        // Arrange & Act
        var attribute = new MapCollectionAttribute();

        // Assert
        attribute.ElementType.Should().BeNull();
        attribute.CollectionType.Should().Be(CollectionType.List);
        attribute.EmptyHandling.Should().Be(EmptyCollectionHandling.CreateEmpty);
        attribute.RemoveDuplicates.Should().BeFalse();
    }

    /// <summary>
    /// MapComplex 어트리뷰트 기본 설정 테스트
    /// </summary>
    [Fact]
    public void MapComplexAttribute_기본설정_올바르게적용됨()
    {
        // Arrange & Act
        var attribute = new MapComplexAttribute();

        // Assert
        attribute.Strategy.Should().Be(NestedObjectStrategy.CreateNew);
        attribute.CircularHandling.Should().Be(CircularReferenceHandling.Ignore);
        attribute.MaxDepth.Should().Be(10);
    }

    /// <summary>
    /// null 타입으로 MapTo 어트리뷰트 생성시 예외 발생 테스트
    /// </summary>
    [Fact]
    public void MapToAttribute_null타입전달시_예외발생()
    {
        // Arrange, Act & Assert
        var act = () => new MapToAttribute(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

/// <summary>
/// 매핑 결과 테스트
/// </summary>
public sealed class MappingResultTests
{
    /// <summary>
    /// 성공 결과 생성 테스트
    /// </summary>
    [Fact]
    public void MappingResult_성공결과생성_올바른값반환()
    {
        // Arrange
        var testValue = "test";
        var elapsedMs = 100L;
        var propertyCount = 5;

        // Act
        var result = MappingResult<string>.Success(testValue, elapsedMs, propertyCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(testValue);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
        result.ElapsedMilliseconds.Should().Be(elapsedMs);
        result.MappedPropertyCount.Should().Be(propertyCount);
    }

    /// <summary>
    /// 실패 결과 생성 테스트
    /// </summary>
    [Fact]
    public void MappingResult_실패결과생성_올바른값반환()
    {
        // Arrange
        var errorMessage = "매핑 실패";
        var exception = new InvalidOperationException("테스트 예외");

        // Act
        var result = MappingResult<string>.Failure(errorMessage, exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().Be(exception);
        result.ElapsedMilliseconds.Should().Be(0);
        result.MappedPropertyCount.Should().Be(0);
    }
}

/// <summary>
/// 매핑 컨텍스트 테스트
/// </summary>
public sealed class MappingContextTests
{
    /// <summary>
    /// 매핑 컨텍스트 초기 상태 테스트
    /// </summary>
    [Fact]
    public void MappingContext_초기상태_올바르게설정됨()
    {
        // Arrange & Act
        var context = new MappingContext();

        // Assert
        context.ObjectCache.Should().BeEmpty();
        context.Options.Should().NotBeNull();
        context.UserData.Should().BeEmpty();
        context.Statistics.Should().NotBeNull();
        context.CancellationToken.Should().Be(default);
    }

    /// <summary>
    /// 객체 캐시 추가 및 조회 테스트
    /// </summary>
    [Fact]
    public void MappingContext_객체캐시_추가및조회가능()
    {
        // Arrange
        var context = new MappingContext();
        var source = new { Id = 1, Name = "Test" };
        var destination = new { Id = 1, Name = "Test" };

        // Act
        context.AddToCache(source, destination);

        // Assert
        context.HasCircularReference(source).Should().BeTrue();
        context.GetFromCache<object>(source).Should().Be(destination);
    }

    /// <summary>
    /// 순환 참조 확인 테스트
    /// </summary>
    [Fact]
    public void MappingContext_순환참조확인_올바르게동작()
    {
        // Arrange
        var context = new MappingContext();
        var source = new { Id = 1 };

        // Act & Assert - 캐시에 없을 때
        context.HasCircularReference(source).Should().BeFalse();

        // Act & Assert - 캐시에 있을 때
        context.AddToCache(source, new object());
        context.HasCircularReference(source).Should().BeTrue();
    }

    /// <summary>
    /// 캐시에서 타입 안전 조회 테스트
    /// </summary>
    [Fact]
    public void MappingContext_타입안전조회_올바르게동작()
    {
        // Arrange
        var context = new MappingContext();
        var source = "test";
        var destination = 123;

        context.AddToCache(source, destination);

        // Act & Assert - 올바른 타입으로 조회
        context.GetFromCache<string>(source).Should().Be(123.ToString());

        // Act & Assert - 잘못된 타입으로 조회
        context.GetFromCache<string>(source).Should().BeNull();
    }
}

/// <summary>
/// 매핑 옵션 테스트
/// </summary>
public sealed class MappingOptionsTests
{
    /// <summary>
    /// 매핑 옵션 기본값 테스트
    /// </summary>
    [Fact]
    public void MappingOptions_기본값_올바르게설정됨()
    {
        // Arrange & Act
        var options = new MappingOptions();

        // Assert
        options.NullHandling.Should().Be(NullValueHandling.SetNull);
        options.StringComparison.Should().Be(StringComparison.OrdinalIgnoreCase);
        options.MaxDepth.Should().Be(10);
        options.EnableValidation.Should().BeTrue();
        options.EnablePerformanceMonitoring.Should().BeFalse();
        options.ThreadSafe.Should().BeTrue();
    }

    /// <summary>
    /// 매핑 옵션 커스텀 설정 테스트
    /// </summary>
    [Fact]
    public void MappingOptions_커스텀설정_올바르게적용됨()
    {
        // Arrange & Act
        var options = new MappingOptions
        {
            NullHandling = NullValueHandling.Ignore,
            StringComparison = StringComparison.Ordinal,
            MaxDepth = 5,
            EnableValidation = false,
            EnablePerformanceMonitoring = true,
            ThreadSafe = false
        };

        // Assert
        options.NullHandling.Should().Be(NullValueHandling.Ignore);
        options.StringComparison.Should().Be(StringComparison.Ordinal);
        options.MaxDepth.Should().Be(5);
        options.EnableValidation.Should().BeFalse();
        options.EnablePerformanceMonitoring.Should().BeTrue();
        options.ThreadSafe.Should().BeFalse();
    }
}

/// <summary>
/// 매핑 통계 테스트
/// </summary>
public sealed class MappingStatisticsTests
{
    /// <summary>
    /// 매핑 통계 초기 상태 테스트
    /// </summary>
    [Fact]
    public void MappingStatistics_초기상태_올바르게설정됨()
    {
        // Arrange & Act
        var statistics = new MappingStatistics();

        // Assert
        statistics.MappedObjectCount.Should().Be(0);
        statistics.MappedPropertyCount.Should().Be(0);
        statistics.CacheHits.Should().Be(0);
        statistics.ValidationFailures.Should().Be(0);
        statistics.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 매핑 통계 업데이트 테스트
    /// </summary>
    [Fact]
    public void MappingStatistics_값업데이트_올바르게반영됨()
    {
        // Arrange
        var statistics = new MappingStatistics();
        var initialTime = statistics.StartTime;

        // Act
        statistics.MappedObjectCount = 10;
        statistics.MappedPropertyCount = 50;
        statistics.CacheHits = 5;
        statistics.ValidationFailures = 2;

        Thread.Sleep(10); // 시간 경과 시뮬레이션

        // Assert
        statistics.MappedObjectCount.Should().Be(10);
        statistics.MappedPropertyCount.Should().Be(50);
        statistics.CacheHits.Should().Be(5);
        statistics.ValidationFailures.Should().Be(2);
        statistics.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
