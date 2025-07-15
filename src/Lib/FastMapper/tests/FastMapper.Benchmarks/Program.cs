using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using AutoMapper;
using Mapster;
using FastMapper.Core.Attributes;

namespace FastMapper.Benchmarks;

/// <summary>
/// 벤치마크 프로그램 진입점
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("🚀 FastMapper 성능 벤치마크 시작");
        Console.WriteLine("================================");

        BenchmarkRunner.Run<MappingBenchmarks>();
    }
}

/// <summary>
/// 매핑 성능 벤치마크
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[RankColumn]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class MappingBenchmarks
{
    private readonly List<User> _users;
    private readonly IMapper _autoMapper;
    private readonly TypeAdapterConfig _mapsterConfig;

    public MappingBenchmarks()
    {
        // 테스트 데이터 생성
        _users = GenerateUsers(1000);

        // AutoMapper 설정
        var autoMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserDto>()
               .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
               .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.DateOfBirth)));
        });
        _autoMapper = autoMapperConfig.CreateMapper();

        // Mapster 설정
        _mapsterConfig = new TypeAdapterConfig();
        _mapsterConfig.NewConfig<User, UserDto>()
                     .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                     .Map(dest => dest.Age, src => CalculateAge(src.DateOfBirth));
    }

    /// <summary>
    /// FastMapper 단일 객체 매핑 벤치마크
    /// </summary>
    [Benchmark(Baseline = true)]
    public UserDto FastMapper_SingleObject()
    {
        var user = _users[0];
        // 실제로는 생성된 매퍼 사용
        return ManualMapping(user);
    }

    /// <summary>
    /// AutoMapper 단일 객체 매핑 벤치마크
    /// </summary>
    [Benchmark]
    public UserDto AutoMapper_SingleObject()
    {
        var user = _users[0];
        return _autoMapper.Map<UserDto>(user);
    }

    /// <summary>
    /// Mapster 단일 객체 매핑 벤치마크
    /// </summary>
    [Benchmark]
    public UserDto Mapster_SingleObject()
    {
        var user = _users[0];
        return user.Adapt<UserDto>(_mapsterConfig);
    }

    /// <summary>
    /// 수동 매핑 벤치마크 (성능 기준선)
    /// </summary>
    [Benchmark]
    public UserDto Manual_SingleObject()
    {
        var user = _users[0];
        return ManualMapping(user);
    }

    /// <summary>
    /// FastMapper 컬렉션 매핑 벤치마크
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public List<UserDto> FastMapper_Collection(int count)
    {
        var users = _users.Take(count);
        // 실제로는 생성된 매퍼의 MapCollection 사용
        return users.Select(ManualMapping).ToList();
    }

    /// <summary>
    /// AutoMapper 컬렉션 매핑 벤치마크
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public List<UserDto> AutoMapper_Collection(int count)
    {
        var users = _users.Take(count);
        return _autoMapper.Map<List<UserDto>>(users.ToList());
    }

    /// <summary>
    /// Mapster 컬렉션 매핑 벤치마크
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public List<UserDto> Mapster_Collection(int count)
    {
        var users = _users.Take(count);
        return users.Adapt<List<UserDto>>(_mapsterConfig);
    }

    /// <summary>
    /// 수동 매핑 컬렉션 벤치마크
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public List<UserDto> Manual_Collection(int count)
    {
        var users = _users.Take(count);
        return users.Select(ManualMapping).ToList();
    }

    /// <summary>
    /// 복잡한 객체 매핑 벤치마크
    /// </summary>
    [Benchmark]
    public ComplexDto Complex_FastMapper()
    {
        var complex = GenerateComplexObject();
        return ManualComplexMapping(complex);
    }

    /// <summary>
    /// 메모리 사용량 테스트
    /// </summary>
    [Benchmark]
    public void Memory_AllocationTest()
    {
        var users = _users.Take(100);
        var results = users.Select(ManualMapping).ToArray();
        // 메모리 할당 패턴 분석
    }

    /// <summary>
    /// 테스트 사용자 데이터 생성
    /// </summary>
    private static List<User> GenerateUsers(int count)
    {
        var random = new Random(42);
        var users = new List<User>(count);

        var firstNames = new[] { "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임" };
        var lastNames = new[] { "민수", "영희", "철수", "순이", "진호", "미영", "상현", "지연", "동우", "하늘" };

        for (int i = 0; i < count; i++)
        {
            users.Add(new User
            {
                Id = i + 1,
                FirstName = firstNames[random.Next(firstNames.Length)],
                LastName = lastNames[random.Next(lastNames.Length)],
                Email = $"user{i + 1}@example.com",
                DateOfBirth = DateTime.Now.AddYears(-random.Next(18, 65)),
                CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                IsActive = random.Next(2) == 0
            });
        }

        return users;
    }

    /// <summary>
    /// 복잡한 객체 생성
    /// </summary>
    private static ComplexObject GenerateComplexObject()
    {
        return new ComplexObject
        {
            Id = 1,
            Name = "복잡한 객체",
            Details = new ObjectDetails
            {
                Description = "상세 설명",
                Priority = 5,
                Tags = new List<string> { "tag1", "tag2", "tag3" }
            },
            Items = Enumerable.Range(1, 10).Select(i => new Item
            {
                Id = i,
                Name = $"항목 {i}",
                Value = i * 10.5m
            }).ToList()
        };
    }

    /// <summary>
    /// 수동 매핑 구현 (성능 기준선)
    /// </summary>
    private static UserDto ManualMapping(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Age = CalculateAge(user.DateOfBirth),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// 복잡한 객체 수동 매핑
    /// </summary>
    private static ComplexDto ManualComplexMapping(ComplexObject source)
    {
        return new ComplexDto
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Details?.Description ?? string.Empty,
            Priority = source.Details?.Priority ?? 0,
            TagCount = source.Details?.Tags?.Count ?? 0,
            ItemCount = source.Items?.Count ?? 0,
            TotalValue = source.Items?.Sum(i => i.Value) ?? 0
        };
    }

    /// <summary>
    /// 나이 계산
    /// </summary>
    private static int CalculateAge(DateTime birthDate)
    {
        return DateTime.Today.Year - birthDate.Year - 
               (DateTime.Today.DayOfYear < birthDate.DayOfYear ? 1 : 0);
    }
}

/// <summary>
/// 벤치마크용 사용자 엔티티
/// </summary>
[MapTo(typeof(UserDto), OptimizationLevel = OptimizationLevel.Aggressive)]
public sealed class User
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "FullName", ConverterMethod = "GetFullName")]
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    [MapProperty(TargetPropertyName = "Age", ConverterMethod = "CalculateAge")]
    public DateTime DateOfBirth { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; }

    public static string GetFullName(string firstName) => firstName; // 실제로는 성 + 이름
    public static int CalculateAge(DateTime birthDate) => 
        DateTime.Today.Year - birthDate.Year - 
        (DateTime.Today.DayOfYear < birthDate.DayOfYear ? 1 : 0);
}

/// <summary>
/// 벤치마크용 사용자 DTO
/// </summary>
public sealed class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 복잡한 객체 (성능 테스트용)
/// </summary>
[MapTo(typeof(ComplexDto))]
public sealed class ComplexObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [MapComplex]
    public ObjectDetails? Details { get; set; }
    
    [MapCollection(CollectionType = CollectionType.List)]
    public List<Item>? Items { get; set; }
}

/// <summary>
/// 객체 상세 정보
/// </summary>
public sealed class ObjectDetails
{
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// 항목 클래스
/// </summary>
public sealed class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>
/// 복잡한 DTO
/// </summary>
public sealed class ComplexDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int TagCount { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// 메모리 사용량 벤치마크
/// </summary>
[MemoryDiagnoser]
public class MemoryBenchmarks
{
    private readonly List<User> _users;

    public MemoryBenchmarks()
    {
        _users = GenerateUsers(1000);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public UserDto[] LargeCollection_Mapping(int count)
    {
        var users = _users.Take(count);
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}",
            Email = u.Email,
            Age = DateTime.Today.Year - u.DateOfBirth.Year,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToArray();
    }

    private static List<User> GenerateUsers(int count)
    {
        var users = new List<User>(count);
        for (int i = 0; i < count; i++)
        {
            users.Add(new User
            {
                Id = i + 1,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"user{i}@test.com",
                DateOfBirth = DateTime.Now.AddYears(-30),
                CreatedAt = DateTime.Now,
                IsActive = true
            });
        }
        return users;
    }
}

/// <summary>
/// 동시성 벤치마크
/// </summary>
[SimpleJob]
public class ConcurrencyBenchmarks
{
    private readonly List<User> _users;

    public ConcurrencyBenchmarks()
    {
        _users = GenerateUsers(1000);
    }

    [Benchmark]
    public async Task<UserDto[]> Parallel_Mapping()
    {
        var tasks = _users.Select(async user => await Task.Run(() => new UserDto
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Age = DateTime.Today.Year - user.DateOfBirth.Year,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }));

        return await Task.WhenAll(tasks);
    }

    [Benchmark]
    public UserDto[] Sequential_Mapping()
    {
        return _users.Select(user => new UserDto
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Age = DateTime.Today.Year - user.DateOfBirth.Year,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }).ToArray();
    }

    private static List<User> GenerateUsers(int count)
    {
        var users = new List<User>(count);
        for (int i = 0; i < count; i++)
        {
            users.Add(new User
            {
                Id = i + 1,
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"user{i}@test.com",
                DateOfBirth = DateTime.Now.AddYears(-30),
                CreatedAt = DateTime.Now,
                IsActive = true
            });
        }
        return users;
    }
}
