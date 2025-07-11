# 🚀 FastMapper

**고성능 코드 제네레이션 기반 객체 매핑 라이브러리**

Source Generator를 활용하여 컴파일 타임에 매핑 코드를 생성하는 C# 라이브러리입니다. Reflection을 사용하지 않아 런타임 성능을 극대화합니다.

## ✨ 주요 특징

- **Source Generator 기반**: 컴파일 타임 코드 생성으로 높은 성능
- **Attribute 기반 설정**: 간단하고 직관적인 매핑 구성
- **제네릭 인터페이스**: 타입 안전성 보장
- **DI 컨테이너 지원**: 의존성 주입 완벽 지원
- **양방향 매핑**: 필요시 양방향 매핑 가능

## 📦 설치

```bash
# NuGet Package Manager
Install-Package FastMapper.Core
Install-Package FastMapper.SourceGenerator
Install-Package FastMapper.Extensions

# .NET CLI
dotnet add package FastMapper.Core
dotnet add package FastMapper.SourceGenerator
dotnet add package FastMapper.Extensions
```

## 🚀 기본 사용법

### 1. 매핑 설정

```csharp
using FastMapper.Core.Attributes;

// 소스 엔티티
[MapTo(typeof(UserDto), IsBidirectional = true)]
public sealed class User
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "FullName", ConverterMethod = "GetFullName")]
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    [MapProperty(Ignore = true)]
    public string PasswordHash { get; set; } = string.Empty;

    // 커스텀 변환 메서드
    public static string GetFullName(string firstName) => $"{firstName} {/* LastName 추가 */}";
}

// 대상 DTO
public sealed record UserDto(
    int Id,
    string FullName
);
```

### 2. DI 설정

```csharp
using FastMapper.Extensions.DependencyInjection;

// Program.cs
services.AddFastMapper();
```

### 3. 매퍼 사용

```csharp
public class UserService
{
    private readonly IMapper<User, UserDto> _mapper;
    
    public UserService(IMapper<User, UserDto> mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(User user) => _mapper.Map(user);
    
    public List<UserDto> GetUsers(List<User> users) => 
        _mapper.MapCollection(users).ToList();
}
```

## 🎯 고급 기능

### 커스텀 변환

```csharp
[MapProperty(ConverterMethod = "FormatDate")]
public DateTime CreatedAt { get; set; }

public static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");
```

### 조건부 매핑

```csharp
[MapProperty(ConditionMethod = "ShouldMapEmail")]
public string Email { get; set; } = string.Empty;

public static bool ShouldMapEmail(User user) => !string.IsNullOrEmpty(user.Email);
```

### 컬렉션 매핑

```csharp
[MapCollection(CollectionType = CollectionType.List, RemoveDuplicates = true)]
public List<Order> Orders { get; set; } = new();
```

## 🧪 빌드 및 테스트

```bash
# 전체 빌드 및 테스트
./build.sh    # Linux/macOS
build.bat     # Windows

# 또는 직접
dotnet build
dotnet test

# 성능 벤치마크
dotnet run --project tests/FastMapper.Benchmarks -c Release
```

## 📁 프로젝트 구조

```
FastMapper/
├── src/
│   ├── FastMapper.Core/              # 핵심 라이브러리
│   ├── FastMapper.SourceGenerator/   # Source Generator
│   └── FastMapper.Extensions/        # DI 확장
├── tests/
│   ├── FastMapper.Tests/             # 테스트
│   └── FastMapper.Benchmarks/        # 벤치마크
└── samples/
    └── FastMapper.Sample/            # 사용 예시
```

## 📄 라이선스

MIT License - 자세한 내용은 [LICENSE](LICENSE) 파일 참조

## 🔗 추가 문서

- [사용법 가이드](USAGE.md)
- [변경 이력](CHANGELOG.md)
