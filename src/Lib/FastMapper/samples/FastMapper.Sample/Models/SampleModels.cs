using FastMapper.Core.Attributes;

namespace FastMapper.Sample.Models;

/// <summary>
/// 사용자 엔티티 (데이터베이스 모델)
/// </summary>
[MapTo(typeof(UserDto), IsBidirectional = true, OptimizationLevel = OptimizationLevel.Aggressive)]
[MapTo(typeof(UserSummaryDto), ProfileName = "Summary")]
public sealed class User
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "FullName", ConverterMethod = "GetFullName")]
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    [MapProperty(TargetPropertyName = "EmailAddress")]
    public string Email { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    [MapProperty(ConverterMethod = "CalculateAge")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MapProperty(Ignore = true)] // DTO에서 제외
    public string PasswordHash { get; set; } = string.Empty;
    
    [MapCollection(CollectionType = CollectionType.List, RemoveDuplicates = true)]
    public List<Order> Orders { get; set; } = new();
    
    [MapComplex(Strategy = NestedObjectStrategy.CreateNew)]
    public UserProfile? Profile { get; set; }

    // 커스텀 변환 메서드들
    public static string GetFullName(string firstName) => 
        $"{firstName} {/* LastName은 별도로 추가 */}";
    
    public static int CalculateAge(DateTime birthDate) => 
        DateTime.Today.Year - birthDate.Year - 
        (DateTime.Today.DayOfYear < birthDate.DayOfYear ? 1 : 0);
}

/// <summary>
/// 사용자 DTO (API 응답 모델)
/// </summary>
public sealed record UserDto(
    int Id,
    string FullName,
    string EmailAddress,
    DateTime DateOfBirth,
    int Age,
    List<OrderDto> Orders,
    UserProfileDto? Profile
);

/// <summary>
/// 사용자 요약 DTO
/// </summary>
public sealed record UserSummaryDto(
    int Id,
    string FullName,
    string EmailAddress,
    int OrderCount
);

/// <summary>
/// 주문 엔티티
/// </summary>
[MapTo(typeof(OrderDto), OptimizationLevel = OptimizationLevel.Balanced)]
public sealed class Order
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "OrderNumber")]
    public string Code { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    [MapProperty(ConverterMethod = "FormatCurrency")]
    public string Currency { get; set; } = "KRW";
    
    public DateTime OrderDate { get; set; }
    
    [MapProperty(ConditionMethod = "ShouldIncludeStatus")]
    public OrderStatus Status { get; set; }
    
    [MapCollection(CollectionType = CollectionType.List)]
    public List<OrderItem> Items { get; set; } = new();

    // 커스텀 변환 메서드
    public static string FormatCurrency(string currency) => 
        currency switch
        {
            "KRW" => "원",
            "USD" => "달러",
            "EUR" => "유로",
            _ => currency
        };
    
    public static bool ShouldIncludeStatus(Order order) => 
        order.Status != OrderStatus.Draft;
}

/// <summary>
/// 주문 DTO
/// </summary>
public sealed record OrderDto(
    int Id,
    string OrderNumber,
    decimal Amount,
    string FormattedCurrency,
    DateTime OrderDate,
    OrderStatus? Status,
    List<OrderItemDto> Items
);

/// <summary>
/// 주문 항목 엔티티
/// </summary>
[MapTo(typeof(OrderItemDto))]
public sealed class OrderItem
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "ProductName")]
    public string Name { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    [MapProperty(ConverterMethod = "CalculateTotal")]
    public decimal Price { get; set; }
    
    // 계산된 총액
    public static decimal CalculateTotal(decimal price) => price; // 실제로는 Quantity와 곱셈
}

/// <summary>
/// 주문 항목 DTO
/// </summary>
public sealed record OrderItemDto(
    int Id,
    string ProductName,
    int Quantity,
    decimal TotalPrice
);

/// <summary>
/// 사용자 프로필 엔티티
/// </summary>
[MapTo(typeof(UserProfileDto), IsBidirectional = true)]
public sealed class UserProfile
{
    public int Id { get; set; }
    
    [MapProperty(ValidatorMethod = "ValidatePhoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    [MapProperty(DefaultValue = "Unknown")]
    public string? City { get; set; }
    
    [MapProperty(TargetPropertyName = "Country")]
    public string CountryCode { get; set; } = "KR";

    // 검증 메서드
    public static bool ValidatePhoneNumber(string phoneNumber) => 
        !string.IsNullOrEmpty(phoneNumber) && phoneNumber.Length >= 10;
}

/// <summary>
/// 사용자 프로필 DTO
/// </summary>
public sealed record UserProfileDto(
    int Id,
    string PhoneNumber,
    string Address,
    string City,
    string Country
);

/// <summary>
/// 주문 상태 열거형
/// </summary>
public enum OrderStatus
{
    Draft,
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
