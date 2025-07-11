using FastMapper.Core.Abstractions;
using FastMapper.Extensions.DependencyInjection;
using FastMapper.Sample.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FastMapper.Sample.Services;

/// <summary>
/// 사용자 서비스 인터페이스
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 사용자 목록 조회
    /// </summary>
    Task<List<UserDto>> GetUsersAsync();
    
    /// <summary>
    /// 사용자 요약 정보 조회
    /// </summary>
    Task<List<UserSummaryDto>> GetUserSummariesAsync();
    
    /// <summary>
    /// 사용자 생성
    /// </summary>
    Task<UserDto> CreateUserAsync(UserDto userDto);
}

/// <summary>
/// 사용자 서비스 구현
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IMapper<User, UserDto> _userMapper;
    private readonly IMapper<User, UserSummaryDto> _userSummaryMapper;
    private readonly IBidirectionalMapper<User, UserDto> _bidirectionalMapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IMapper<User, UserDto> userMapper,
        IMapper<User, UserSummaryDto> userSummaryMapper,
        IBidirectionalMapper<User, UserDto> bidirectionalMapper,
        ILogger<UserService> logger)
    {
        _userMapper = userMapper;
        _userSummaryMapper = userSummaryMapper;
        _bidirectionalMapper = bidirectionalMapper;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        _logger.LogInformation("사용자 목록을 조회합니다...");
        
        var stopwatch = Stopwatch.StartNew();
        
        // 가상의 데이터베이스에서 사용자 조회
        var users = await GetSampleUsersAsync();
        
        // 고성능 배치 매핑
        var userDtos = _userMapper.MapCollection(users).ToList();
        
        stopwatch.Stop();
        _logger.LogInformation("사용자 {Count}명을 {ElapsedMs}ms에 매핑했습니다.", 
            userDtos.Count, stopwatch.ElapsedMilliseconds);

        return userDtos;
    }

    public async Task<List<UserSummaryDto>> GetUserSummariesAsync()
    {
        _logger.LogInformation("사용자 요약 정보를 조회합니다...");
        
        var users = await GetSampleUsersAsync();
        
        // 다른 DTO로 매핑 (같은 소스, 다른 대상)
        var summaries = _userSummaryMapper.MapCollection(users).ToList();
        
        _logger.LogInformation("사용자 요약 {Count}개를 생성했습니다.", summaries.Count);
        
        return summaries;
    }

    public async Task<UserDto> CreateUserAsync(UserDto userDto)
    {
        _logger.LogInformation("새 사용자를 생성합니다: {UserName}", userDto.FullName);
        
        // DTO를 엔티티로 역방향 매핑
        var user = _bidirectionalMapper.MapFrom(userDto);
        
        // 가상의 데이터베이스 저장
        user.Id = Random.Shared.Next(1000, 9999);
        user.CreatedAt = DateTime.UtcNow;
        
        await Task.Delay(100); // 데이터베이스 저장 시뮬레이션
        
        // 다시 DTO로 매핑하여 반환
        var result = _bidirectionalMapper.MapTo(user);
        
        _logger.LogInformation("사용자가 생성되었습니다. ID: {UserId}", user.Id);
        
        return result;
    }

    /// <summary>
    /// 샘플 사용자 데이터 생성
    /// </summary>
    private static async Task<List<User>> GetSampleUsersAsync()
    {
        // 실제 환경에서는 데이터베이스 쿼리
        await Task.Delay(50); // 데이터베이스 조회 시뮬레이션
        
        return new List<User>
        {
            new()
            {
                Id = 1,
                FirstName = "홍",
                LastName = "길동",
                Email = "hong@example.com",
                DateOfBirth = new DateTime(1990, 5, 15),
                Profile = new UserProfile
                {
                    Id = 1,
                    PhoneNumber = "010-1234-5678",
                    Address = "서울시 강남구",
                    City = "서울",
                    CountryCode = "KR"
                },
                Orders = new List<Order>
                {
                    new()
                    {
                        Id = 1,
                        Code = "ORD-001",
                        Amount = 50000,
                        Currency = "KRW",
                        OrderDate = DateTime.Now.AddDays(-5),
                        Status = OrderStatus.Delivered,
                        Items = new List<OrderItem>
                        {
                            new() { Id = 1, Name = "노트북", Quantity = 1, Price = 50000 }
                        }
                    }
                }
            },
            new()
            {
                Id = 2,
                FirstName = "김",
                LastName = "철수",
                Email = "kim@example.com",
                DateOfBirth = new DateTime(1985, 8, 20),
                Profile = new UserProfile
                {
                    Id = 2,
                    PhoneNumber = "010-9876-5432",
                    Address = "부산시 해운대구",
                    City = "부산",
                    CountryCode = "KR"
                },
                Orders = new List<Order>
                {
                    new()
                    {
                        Id = 2,
                        Code = "ORD-002",
                        Amount = 25000,
                        Currency = "KRW",
                        OrderDate = DateTime.Now.AddDays(-2),
                        Status = OrderStatus.Processing,
                        Items = new List<OrderItem>
                        {
                            new() { Id = 2, Name = "마우스", Quantity = 2, Price = 12500 }
                        }
                    }
                }
            },
            new()
            {
                Id = 3,
                FirstName = "이",
                LastName = "영희",
                Email = "lee@example.com",
                DateOfBirth = new DateTime(1992, 12, 3),
                Profile = new UserProfile
                {
                    Id = 3,
                    PhoneNumber = "010-5555-7777",
                    Address = "대구시 중구",
                    City = "대구",
                    CountryCode = "KR"
                },
                Orders = new List<Order>()
            }
        };
    }
}

/// <summary>
/// 주문 서비스 인터페이스
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 주문 목록 조회
    /// </summary>
    Task<List<OrderDto>> GetOrdersAsync();
    
    /// <summary>
    /// 대량 주문 매핑 성능 테스트
    /// </summary>
    Task<(List<OrderDto> Orders, TimeSpan Duration)> PerformanceBenchmarkAsync(int orderCount);
}

/// <summary>
/// 주문 서비스 구현
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IMapper<Order, OrderDto> _orderMapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMapper<Order, OrderDto> orderMapper, ILogger<OrderService> logger)
    {
        _orderMapper = orderMapper;
        _logger = logger;
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        _logger.LogInformation("주문 목록을 조회합니다...");
        
        var orders = await GetSampleOrdersAsync();
        var orderDtos = _orderMapper.MapCollection(orders).ToList();
        
        _logger.LogInformation("주문 {Count}개를 매핑했습니다.", orderDtos.Count);
        
        return orderDtos;
    }

    public async Task<(List<OrderDto> Orders, TimeSpan Duration)> PerformanceBenchmarkAsync(int orderCount)
    {
        _logger.LogInformation("성능 테스트를 시작합니다. 주문 개수: {OrderCount}", orderCount);
        
        // 대량 데이터 생성
        var orders = await GenerateLargeOrderDataAsync(orderCount);
        
        var stopwatch = Stopwatch.StartNew();
        
        // 비동기 배치 매핑
        var orderDtos = await _orderMapper.MapCollectionAsync(orders);
        
        stopwatch.Stop();
        
        _logger.LogInformation("성능 테스트 완료. {Count}개 주문을 {ElapsedMs}ms에 매핑. " +
                             "평균: {AvgMs}ms/주문", 
            orderDtos.Count, stopwatch.ElapsedMilliseconds, 
            (double)stopwatch.ElapsedMilliseconds / orderDtos.Count);
        
        return (orderDtos.ToList(), stopwatch.Elapsed);
    }

    /// <summary>
    /// 샘플 주문 데이터 생성
    /// </summary>
    private static async Task<List<Order>> GetSampleOrdersAsync()
    {
        await Task.Delay(30);
        
        return new List<Order>
        {
            new()
            {
                Id = 1,
                Code = "ORD-001",
                Amount = 100000,
                Currency = "KRW",
                OrderDate = DateTime.Now.AddDays(-3),
                Status = OrderStatus.Delivered,
                Items = new List<OrderItem>
                {
                    new() { Id = 1, Name = "키보드", Quantity = 1, Price = 80000 },
                    new() { Id = 2, Name = "마우스패드", Quantity = 1, Price = 20000 }
                }
            },
            new()
            {
                Id = 2,
                Code = "ORD-002",
                Amount = 50000,
                Currency = "USD",
                OrderDate = DateTime.Now.AddDays(-1),
                Status = OrderStatus.Processing,
                Items = new List<OrderItem>
                {
                    new() { Id = 3, Name = "헤드셋", Quantity = 1, Price = 50000 }
                }
            }
        };
    }

    /// <summary>
    /// 대량 주문 데이터 생성
    /// </summary>
    private static async Task<List<Order>> GenerateLargeOrderDataAsync(int count)
    {
        await Task.Delay(100); // 데이터 생성 시뮬레이션
        
        var orders = new List<Order>(count);
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            orders.Add(new Order
            {
                Id = i + 1,
                Code = $"ORD-{i + 1:D6}",
                Amount = random.Next(10000, 100000),
                Currency = random.Next(2) == 0 ? "KRW" : "USD",
                OrderDate = DateTime.Now.AddDays(-random.Next(30)),
                Status = (OrderStatus)random.Next(Enum.GetValues<OrderStatus>().Length),
                Items = GenerateOrderItems(random.Next(1, 5))
            });
        }
        
        return orders;
    }

    /// <summary>
    /// 주문 항목 생성
    /// </summary>
    private static List<OrderItem> GenerateOrderItems(int count)
    {
        var items = new List<OrderItem>(count);
        var productNames = new[] { "노트북", "마우스", "키보드", "모니터", "스피커", "헤드셋" };
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            items.Add(new OrderItem
            {
                Id = i + 1,
                Name = productNames[random.Next(productNames.Length)],
                Quantity = random.Next(1, 5),
                Price = random.Next(5000, 50000)
            });
        }
        
        return items;
    }
}

/// <summary>
/// 매핑 데모 서비스 인터페이스
/// </summary>
public interface IMappingDemoService
{
    /// <summary>
    /// 전체 데모 실행
    /// </summary>
    Task RunDemonstrationAsync();
}

/// <summary>
/// 매핑 데모 서비스 구현
/// </summary>
public sealed class MappingDemoService : IMappingDemoService
{
    private readonly IUserService _userService;
    private readonly IOrderService _orderService;
    private readonly IDynamicMapper _dynamicMapper;
    private readonly IMapperFactory _mapperFactory;
    private readonly ILogger<MappingDemoService> _logger;

    public MappingDemoService(
        IUserService userService,
        IOrderService orderService,
        IDynamicMapper dynamicMapper,
        IMapperFactory mapperFactory,
        ILogger<MappingDemoService> logger)
    {
        _userService = userService;
        _orderService = orderService;
        _dynamicMapper = dynamicMapper;
        _mapperFactory = mapperFactory;
        _logger = logger;
    }

    public async Task RunDemonstrationAsync()
    {
        Console.WriteLine("\n📋 1. 기본 매핑 테스트");
        await DemonstrateBasicMappingAsync(Get_logger());

        Console.WriteLine("\n📋 2. 컬렉션 매핑 테스트");
        await DemonstrateCollectionMappingAsync();

        Console.WriteLine("\n📋 3. 양방향 매핑 테스트");
        await DemonstrateBidirectionalMappingAsync();

        Console.WriteLine("\n📋 4. 동적 매핑 테스트");
        await DemonstrateDynamicMappingAsync();

        Console.WriteLine("\n📋 5. 성능 벤치마크 테스트");
        await DemonstratePerformanceBenchmarkAsync();

        Console.WriteLine("\n📋 6. 팩토리 패턴 테스트");
        await DemonstrateFactoryPatternAsync();
    }

    private ILogger Get_logger()
    {
        return _logger;
    }

    /// <summary>
    /// 기본 매핑 데모
    /// </summary>
    private async Task DemonstrateBasicMappingAsync(ILogger _logger)
    {
        try
        {
            var users = await _userService.GetUsersAsync();
            
            Console.WriteLine($"   ✅ {users.Count}명의 사용자를 성공적으로 매핑했습니다.");
            
            foreach (var user in users.Take(2))
            {
                Console.WriteLine($"   🔹 사용자: {user.FullName} ({user.EmailAddress})");
                Console.WriteLine($"      나이: {user.Age}세, 주문: {user.Orders.Count}개");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "기본 매핑 테스트 중 오류 발생");
            Console.WriteLine($"   ❌ 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 컬렉션 매핑 데모
    /// </summary>
    private async Task DemonstrateCollectionMappingAsync()
    {
        try
        {
            var orders = await _orderService.GetOrdersAsync();
            
            Console.WriteLine($"   ✅ {orders.Count}개의 주문을 성공적으로 매핑했습니다.");
            
            foreach (var order in orders)
            {
                Console.WriteLine($"   🔹 주문: {order.OrderNumber}");
                Console.WriteLine($"      금액: {order.Amount:N0}{order.FormattedCurrency}");
                Console.WriteLine($"      상태: {order.Status}, 항목: {order.Items.Count}개");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "컬렉션 매핑 테스트 중 오류 발생");
            Console.WriteLine($"   ❌ 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 양방향 매핑 데모
    /// </summary>
    private async Task DemonstrateBidirectionalMappingAsync()
    {
        try
        {
            var newUser = new UserDto(
                Id: 0,
                FullName: "박 민수",
                EmailAddress: "park@example.com",
                DateOfBirth: new DateTime(1988, 3, 10),
                Age: 35,
                Orders: new List<OrderDto>(),
                Profile: new UserProfileDto(
                    Id: 0,
                    PhoneNumber: "010-1111-2222",
                    Address: "인천시 남동구",
                    City: "인천",
                    Country: "KR"
                )
            );

            var createdUser = await _userService.CreateUserAsync(newUser);
            
            Console.WriteLine($"   ✅ 새 사용자가 생성되었습니다.");
            Console.WriteLine($"   🔹 ID: {createdUser.Id}");
            Console.WriteLine($"   🔹 이름: {createdUser.FullName}");
            Console.WriteLine($"   🔹 이메일: {createdUser.EmailAddress}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "양방향 매핑 테스트 중 오류 발생");
            Console.WriteLine($"   ❌ 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 동적 매핑 데모
    /// </summary>
    private Task DemonstrateDynamicMappingAsync()
    {
        try
        {
            // 임의의 객체 생성
            var user = new User
            {
                Id = 999,
                FirstName = "테스트",
                LastName = "사용자",
                Email = "test@example.com",
                DateOfBirth = new DateTime(1995, 1, 1)
            };

            // 동적 매핑 실행
            var userDto = _dynamicMapper.Map<UserDto>(user);
            
            if (userDto is not null)
            {
                Console.WriteLine($"   ✅ 동적 매핑 성공");
                Console.WriteLine($"   🔹 {userDto.FullName} ({userDto.EmailAddress})");
            }
            else
            {
                Console.WriteLine($"   ❌ 동적 매핑 실패");
            }

            // 매핑 가능성 확인
            var canMapToDto = _dynamicMapper.CanMap(typeof(User), typeof(UserDto));
            var canMapToSummary = _dynamicMapper.CanMap(typeof(User), typeof(UserSummaryDto));
            
            Console.WriteLine($"   🔍 User -> UserDto 매핑 가능: {canMapToDto}");
            Console.WriteLine($"   🔍 User -> UserSummaryDto 매핑 가능: {canMapToSummary}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "동적 매핑 테스트 중 오류 발생");
            Console.WriteLine($"   ❌ 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 성능 벤치마크 데모
    /// </summary>
    private async Task DemonstratePerformanceBenchmarkAsync()
    {
        try
        {
            var testSizes = new[] { 100, 1000, 5000 };
            
            foreach (var size in testSizes)
            {
                var (orders, duration) = await _orderService.PerformanceBenchmarkAsync(size);
                
                Console.WriteLine($"   🚀 {size:N0}개 항목 매핑: {duration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"      처리량: {size / duration.TotalSeconds:F0} items/sec");
                
                // 메모리 사용량 확인
                GC.Collect();
                var memoryBefore = GC.GetTotalMemory(false);
                Console.WriteLine($"      메모리 사용량: {memoryBefore / 1024 / 1024:F2} MB");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "성능 벤치마크 테스트 중 오류 발생");
            Console.WriteLine($"   ❌ 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 팩토리 패턴 데모
    /// </summary>
    private async Task DemonstrateFactoryPatternAsync()
    {
        try
        {
            // 팩토리를 통해 매퍼 생성
            var userMapper = _mapperFactory.CreateMapper<User, UserDto>();
            var bidirectionalMapper = _mapperFactory.CreateBidirectionalMapper<User, UserDto>();
            
            var testUser = new User
            {
                Id = 777,
                FirstName = "팩토리",
                LastName = "테스트",
                Email = "factory@example.com",
                DateOfBirth = new DateTime(1990, 6, 15)
            };

            var mapped = userMapper.Map(testUser);
            Console.WriteLine($"   ✅ 팩토리 패턴으로 매핑 성공");
            Console.WriteLine($"   🔹 {mapped.FullName} ({mapped.EmailAddress})");
            
            if (bidirectionalMapper is not null)
            {
                Console.WriteLine($"   ✅ 양방향 매퍼도 사용 가능");
            }
            else
            {
                Console.WriteLine($"   ⚠️ 양방향 매퍼는 사용 불가");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "팩토리 패턴 테스트 중 오류 발생");
            Console.WriteLine($"   ❌ 오류: {ex.Message}");
        }
    }
}
