# 🚀 빠른 시작 가이드

FastMapper를 5분 안에 시작해보세요!

## 📦 1단계: 설치

### NuGet 패키지 매니저 사용
```bash
Install-Package FastMapper.Core
Install-Package FastMapper.SourceGenerator
Install-Package FastMapper.Extensions
```

### .NET CLI 사용
```bash
dotnet add package FastMapper.Core
dotnet add package FastMapper.SourceGenerator
dotnet add package FastMapper.Extensions
```

### PackageReference 직접 추가
```xml
<PackageReference Include="FastMapper.Core" Version="1.0.0" />
<PackageReference Include="FastMapper.SourceGenerator" Version="1.0.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<PackageReference Include="FastMapper.Extensions" Version="1.0.0" />
```

## 🎯 2단계: 모델 정의

### 소스 엔티티 (데이터베이스 모델)
```csharp
using FastMapper.Core.Attributes;

[MapTo(typeof(UserDto))]
public class User
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "FullName", ConverterMethod = "GetFullName")]
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    [MapProperty(ConverterMethod = "CalculateAge")]
    public DateTime DateOfBirth { get; set; }
    
    [MapProperty(Ignore = true)] // DTO에서 제외
    public string PasswordHash { get; set; } = string.Empty;

    // 커스텀 변환 메서드
    public static string GetFullName(string firstName) => $"{firstName}"; // 실제로는 성도 포함
    public static int CalculateAge(DateTime birthDate) => 
        DateTime.Today.Year - birthDate.Year;
}
```

### 대상 DTO (API 응답 모델)
```csharp
public record UserDto(
    int Id,
    string FullName,
    string Email,
    int Age
);
```

## ⚙️ 3단계: DI 등록

### ASP.NET Core (Program.cs)
```csharp
using FastMapper.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// FastMapper 등록
builder.Services.AddFastMapper();

// 옵션 설정 (선택사항)
builder.Services.ConfigureFastMapper(options =>
{
    options.EnablePerformanceMonitoring = true;
    options.EnableValidation = true;
});

var app = builder.Build();
```

### 콘솔 애플리케이션
```csharp
using FastMapper.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddFastMapper();
    })
    .Build();
```

## 🎯 4단계: 매퍼 사용

### 서비스에서 의존성 주입
```csharp
using FastMapper.Core.Abstractions;

public class UserService
{
    private readonly IMapper<User, UserDto> _mapper;
    
    public UserService(IMapper<User, UserDto> mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(int id)
    {
        var user = GetUserFromDatabase(id);
        return _mapper.Map(user);
    }
    
    public List<UserDto> GetUsers()
    {
        var users = GetUsersFromDatabase();
        
        // 고성능 컬렉션 매핑
        return _mapper.MapCollection(users).ToList();
    }
    
    private User GetUserFromDatabase(int id)
    {
        return new User
        {
            Id = id,
            FirstName = "홍",
            LastName = "길동",
            Email = "hong@example.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            PasswordHash = "hashed_password"
        };
    }
    
    private List<User> GetUsersFromDatabase()
    {
        return new List<User>
        {
            new() { Id = 1, FirstName = "홍", LastName = "길동", Email = "hong@example.com", DateOfBirth = new DateTime(1990, 1, 1) },
            new() { Id = 2, FirstName = "김", LastName = "철수", Email = "kim@example.com", DateOfBirth = new DateTime(1985, 5, 15) }
        };
    }
}
```

### 컨트롤러에서 사용 (ASP.NET Core)
```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    
    public UsersController(UserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public ActionResult<List<UserDto>> GetUsers()
    {
        var users = _userService.GetUsers();
        return Ok(users);
    }
    
    [HttpGet("{id}")]
    public ActionResult<UserDto> GetUser(int id)
    {
        var user = _userService.GetUser(id);
        return Ok(user);
    }
}
```

## ✅ 5단계: 빌드 및 실행

```bash
# 프로젝트 빌드
dotnet build

# 애플리케이션 실행
dotnet run
```

빌드 시 Source Generator가 자동으로 매핑 코드를 생성합니다!

## 🎉 결과 확인

생성된 매핑 코드는 `Generated` 네임스페이스에서 확인할 수 있습니다:

```csharp
// Generated/UserToUserDtoMapper.g.cs (자동 생성됨)
public sealed class UserToUserDtoMapper : IMapper<User, UserDto>
{
    public UserDto Map(User source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return new UserDto(
            Id: source.Id,
            FullName: User.GetFullName(source.FirstName),
            Email: source.Email,
            Age: User.CalculateAge(source.DateOfBirth)
        );
    }
    
    // 추가 메서드들...
}
```

## 📊 성능 확인

```csharp
public class PerformanceTest
{
    private readonly IMapper<User, UserDto> _mapper;
    
    public PerformanceTest(IMapper<User, UserDto> mapper)
    {
        _mapper = mapper;
    }
    
    public void BenchmarkMapping()
    {
        var users = GenerateTestUsers(10000);
        
        var stopwatch = Stopwatch.StartNew();
        var result = _mapper.MapCollection(users).ToList();
        stopwatch.Stop();
        
        Console.WriteLine($"10,000개 매핑 완료: {stopwatch.ElapsedMilliseconds}ms");
        // 예상 결과: ~20-50ms (AutoMapper: ~1000-2000ms)
    }
}
```

## 🔧 다음 단계

이제 기본 사용법을 익혔습니다! 다음 문서들을 확인해보세요:

### 📖 심화 학습
- [어트리뷰트 가이드](attributes-guide.md) - 모든 매핑 옵션 살펴보기
- [복잡한 매핑](complex-mapping.md) - 중첩 객체와 컬렉션 처리
- [성능 최적화](performance-optimization.md) - 최고 성능 달성하기

### 🏗️ 실전 활용
- [MSA 아키텍처](msa-architecture.md) - 마이크로서비스에서 활용하기
- [단위 테스트](unit-testing.md) - 매핑 테스트 작성하기
- [모니터링](performance-monitoring.md) - 성능 모니터링 설정

### ❓ 문제 해결
- [FAQ](faq.md) - 자주 묻는 질문
- [문제 해결](troubleshooting.md) - 일반적인 문제 해결
- [마이그레이션](migration-guide.md) - AutoMapper에서 마이그레이션

## 💡 팁

**🚀 성능 팁**
- `OptimizationLevel.Aggressive` 사용으로 최고 성능 달성
- 컬렉션 매핑시 `MapCollection()` 메서드 활용
- 비동기 처리시 `MapCollectionAsync()` 사용

**🎯 디버깅 팁**
- Generated 코드 확인으로 매핑 로직 검증
- 개발 환경에서 `EnableValidation = true` 설정
- 커스텀 변환 메서드에 로깅 추가

**🔧 유지보수 팁**
- 어트리뷰트를 통한 명시적 매핑 설정
- 단위 테스트로 매핑 검증
- 성능 모니터링으로 병목 지점 파악

---

**🎉 축하합니다! FastMapper를 성공적으로 시작했습니다. 이제 고성능 매핑의 세계를 경험해보세요!**
