# FastMapper 사용법 가이드

## 기본 사용법

### 1. 프로젝트 설정

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="FastMapper.Core" />
    <PackageReference Include="FastMapper.SourceGenerator" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
    <PackageReference Include="FastMapper.Extensions" />
  </ItemGroup>
</Project>
```

### 2. 매핑 설정

```csharp
using FastMapper.Core.Attributes;

// 소스 클래스
[MapTo(typeof(UserDto))]
public class User
{
    public int Id { get; set; }
    
    [MapProperty(TargetPropertyName = "FullName")]
    public string Name { get; set; } = string.Empty;
    
    [MapProperty(Ignore = true)]
    public string Password { get; set; } = string.Empty;
}

// 대상 클래스
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
```

### 3. DI 설정

```csharp
// Program.cs
using FastMapper.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// FastMapper 등록
builder.Services.AddFastMapper();

var app = builder.Build();
```

### 4. 매퍼 사용

```csharp
public class UserService
{
    private readonly IMapper<User, UserDto> _mapper;
    
    public UserService(IMapper<User, UserDto> mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(User user)
    {
        return _mapper.Map(user);
    }
}
```

## 고급 기능

### 커스텀 변환 함수

```csharp
[MapTo(typeof(UserDto))]
public class User
{
    [MapProperty(ConverterMethod = "FormatName")]
    public string Name { get; set; } = string.Empty;
    
    // 정적 변환 메서드
    public static string FormatName(string name) => name.ToUpper();
}
```

### 조건부 매핑

```csharp
[MapTo(typeof(UserDto))]
public class User
{
    [MapProperty(ConditionMethod = "ShouldMapEmail")]
    public string Email { get; set; } = string.Empty;
    
    public static bool ShouldMapEmail(User user) => !string.IsNullOrEmpty(user.Email);
}
```

### 컬렉션 매핑

```csharp
[MapTo(typeof(UserDto))]
public class User
{
    [MapCollection(CollectionType = CollectionType.List)]
    public List<Order> Orders { get; set; } = new();
}
```

## 빌드

프로젝트를 빌드하면 Source Generator가 자동으로 매핑 코드를 생성합니다:

```bash
dotnet build
```

생성된 코드는 `obj/Debug/net8.0/generated/` 폴더에서 확인할 수 있습니다.
